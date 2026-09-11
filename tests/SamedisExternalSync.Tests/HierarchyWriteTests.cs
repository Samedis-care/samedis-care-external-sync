using FluentAssertions;
using SamedisCare.Api.Http;
using SamedisCare.Api.Lookup;
using SamedisCare.Helper.Logging;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>Records every request, so a test can assert what was written and what was not.</summary>
internal sealed class RecordingApi : IApiClient
{
    private readonly Func<string, (int, string)> _get;
    private readonly (int, string) _write;

    public RecordingApi(Func<string, (int, string)> get, (int, string)? write = null)
    {
        _get = get;
        _write = write ?? (201, "{\"data\":{\"id\":\"new-1\"}}");
    }

    public List<string> Gets { get; } = new();
    public List<(string Resource, string Content)> Posts { get; } = new();
    public List<(string Resource, string Id, string Content)> Puts { get; } = new();

    public int StatusCode { get; private set; }
    public string LastContent { get; private set; } = string.Empty;
    public string LastError => string.Empty;
    public bool TestMode => false;

    public string Get(string resource)
    {
        Gets.Add(resource);
        (StatusCode, LastContent) = _get(resource);
        return LastContent;
    }

    public string Post(string resource, string content)
    {
        Posts.Add((resource, content));
        (StatusCode, LastContent) = _write;
        return LastContent;
    }

    public string Put(string resource, string id, string content)
    {
        Puts.Add((resource, id, content));
        (StatusCode, LastContent) = _write;
        return LastContent;
    }

    public string PostDocument(string r, string f, string n) => throw new NotSupportedException();
}

/// <summary>
/// What the hierarchy resolvers write. The lookup half was already covered; this is the half
/// that reaches the server, and it is where the three nodes genuinely differ -- which payload
/// keys they carry, and which of them are sent empty on an update so a value the source
/// dropped is cleared rather than left standing.
/// </summary>
public class HierarchyWriteTests
{
    private static readonly ISyncLog Silent = new NullSyncLog();

    private const string PropertyId = "507f1f77bcf86cd799439001";
    private const string BuildingId = "507f1f77bcf86cd799439002";

    private static (int, string) NotFound()
        => (404, "{\"meta\":{\"msg\":{\"success\":false,\"error\":\"record_not_found_error\"}}}");

    private static (int, string) Found(string id)
        => (200, $"{{\"data\":[{{\"id\":\"{id}\"}}],\"meta\":{{\"total\":1}}}}");

    // ---- what a create carries ------------------------------------------------------

    [Fact]
    public void A_new_building_leaves_out_the_address_fields_the_source_did_not_fill()
    {
        var api = new RecordingApi(_ => NotFound());

        Buildings.ResolveBuildingId(api, "buildings", PropertyId, "Haus A", true, "", "",
                                    new ResourceLookup(api, "buildings"), Silent,
                                    externalId: "B-1")
                 .Should().Be("new-1");

        var posted = api.Posts.Single().Content;
        posted.Should().Contain("\"title\":\"Haus A\"").And.Contain("\"property_id\"");
        posted.Should().Contain("\"external_id\":\"B-1\"");
        posted.Should().NotContain("street", "a new building should not be written full of empty strings");
        posted.Should().NotContain("zip").And.NotContain("town");
    }

    [Fact]
    public void A_new_building_carries_the_address_the_source_did_fill()
    {
        var api = new RecordingApi(_ => NotFound());

        Buildings.ResolveBuildingId(api, "buildings", PropertyId, "Haus A", true, "", "",
                                    new ResourceLookup(api, "buildings"), Silent,
                                    street: "Musterweg 1", zip: "12345", town: "Musterstadt");

        api.Posts.Single().Content.Should().Contain("Musterweg 1").And.Contain("12345")
                                           .And.Contain("Musterstadt");
    }

    [Fact]
    public void A_new_room_leaves_out_an_empty_note()
    {
        var api = new RecordingApi(_ => NotFound());

        Locations.ResolveLocationId(api, "device_locations", "", "Raum 1", true, "", "",
                                    new ResourceLookup(api, "device_locations"), Silent,
                                    propertyId: PropertyId);

        api.Posts.Single().Content.Should().NotContain("notes");
    }

    // ---- what an update carries -----------------------------------------------------

    // The point of the distinction: a street the source removed has to disappear here too,
    // and it only does if the update names the field. A create has nothing to clear.
    [Fact]
    public void An_updated_building_sends_the_address_fields_even_when_they_are_empty()
    {
        var api = new RecordingApi(url => url.Contains("via/external_id") ? Found("b-1") : NotFound());

        Buildings.ResolveBuildingId(api, "buildings", PropertyId, "Haus A", false, "", "",
                                    new ResourceLookup(api, "buildings"), Silent,
                                    externalId: "B-1", updateOnExisting: true)
                 .Should().Be("b-1");

        var (_, id, content) = api.Puts.Single();
        id.Should().Be("b-1");
        content.Should().Contain("\"street\":\"\"").And.Contain("\"zip\":\"\"")
                        .And.Contain("\"town\":\"\"");
    }

    [Fact]
    public void An_updated_room_sends_an_emptied_note()
    {
        var api = new RecordingApi(url => url.Contains("via/external_id") ? Found("l-1") : NotFound());

        Locations.ResolveLocationId(api, "device_locations", "", "Raum 1", false, "", "",
                                    new ResourceLookup(api, "device_locations"), Silent,
                                    propertyId: PropertyId, externalId: "L-1",
                                    updateOnExisting: true);

        api.Puts.Single().Content.Should().Contain("\"notes\":\"\"");
    }

    [Fact]
    public void Without_update_on_existing_a_match_is_never_written_to()
    {
        var api = new RecordingApi(url => url.Contains("via/external_id") ? Found("b-1") : NotFound());

        Buildings.ResolveBuildingId(api, "buildings", PropertyId, "Haus A", false, "", "",
                                    new ResourceLookup(api, "buildings"), Silent,
                                    externalId: "B-1")
                 .Should().Be("b-1");

        api.Puts.Should().BeEmpty();
        api.Posts.Should().BeEmpty();
    }

    // A row that carries an external_id but no title can still identify the record. Writing
    // to it would send an empty title and clear the name of a building that has one.
    [Fact]
    public void A_row_without_a_title_resolves_the_record_but_never_writes_to_it()
    {
        var api = new RecordingApi(url => url.Contains("via/external_id") ? Found("b-1") : NotFound());

        Buildings.ResolveBuildingId(api, "buildings", PropertyId, "", false, "", "",
                                    new ResourceLookup(api, "buildings"), Silent,
                                    externalId: "B-1", updateOnExisting: true)
                 .Should().Be("b-1");

        api.Puts.Should().BeEmpty("an empty title in a PUT would clear the name it matched on");
    }

    // ---- what a failed create does ---------------------------------------------------

    [Fact]
    public void A_rejected_create_resolves_nothing()
        => Buildings.ResolveBuildingId(
               new RecordingApi(_ => NotFound(), write: (422, "{\"meta\":{\"msg\":{\"error\":\"validation\"}}}")),
               "buildings", PropertyId, "Haus A", true, "", "",
               new ResourceLookup(new RecordingApi(_ => NotFound()), "buildings"), Silent)
           .Should().BeNull();

    [Fact]
    public void A_create_that_answers_without_an_id_resolves_nothing()
    {
        var api = new RecordingApi(_ => NotFound(), write: (201, "{\"data\":{}}"));

        Buildings.ResolveBuildingId(api, "buildings", PropertyId, "Haus A", true, "", "",
                                    new ResourceLookup(api, "buildings"), Silent)
                 .Should().BeNull("without an id the caller has nothing to attach anything to");
    }

    // ---- what a create seeds ---------------------------------------------------------

    [Fact]
    public void A_created_floor_hangs_below_its_building()
    {
        var api = new RecordingApi(_ => NotFound());

        Floors.ResolveFloorId(api, "floors", BuildingId, "1. OG", true, "", "",
                              new ResourceLookup(api, "floors"), Silent);

        api.Posts.Single().Content.Should().Contain($"\"building_id\":\"{BuildingId}\"");
    }

    // The row after this one names the same room. Without seeding it would ask the server for
    // what this run just wrote.
    [Fact]
    public void A_created_room_is_answered_from_memory_afterwards()
    {
        var api = new RecordingApi(_ => NotFound());
        var lookup = new ResourceLookup(api, "device_locations");

        var first = Locations.ResolveLocationId(api, "device_locations", "", "Raum 1", true, "", "",
                                                lookup, Silent, propertyId: PropertyId,
                                                externalId: "L-1");
        var requestsAfterCreate = api.Gets.Count;

        var second = Locations.ResolveLocationId(api, "device_locations", "", "Raum 1", true, "", "",
                                                 lookup, Silent, propertyId: PropertyId,
                                                 externalId: "L-1");

        second.Should().Be(first);
        api.Gets.Count.Should().Be(requestsAfterCreate, "the second row must not ask again");
        api.Posts.Should().ContainSingle("and must not create the room twice");
    }
}
