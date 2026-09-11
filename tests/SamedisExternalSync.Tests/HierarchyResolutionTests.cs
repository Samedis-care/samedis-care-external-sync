using FluentAssertions;
using SamedisCare.Api.Http;
using SamedisCare.Api.Lookup;
using SamedisCare.Helper.Logging;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// Property / building / floor / location resolution. With creation switched off these walk
/// the lookup only, so the HTTP client is never reached -- which is what makes them testable.
/// </summary>
public class HierarchyResolutionTests
{
    private static readonly ISyncLog Silent = new NullSyncLog();

    /// <summary>Never used by these tests: nothing here creates or updates.</summary>
    private static RequestData Unused
        => new("http://localhost", "token", new HttpSettings(), new NullSyncLog());

    private const string PropertyId = "507f1f77bcf86cd799439001";
    private const string BuildingId = "507f1f77bcf86cd799439002";
    private const string FloorId = "507f1f77bcf86cd799439003";
    private const string LocationId = "507f1f77bcf86cd799439004";

    // --- buildings ---------------------------------------------------------------------

    [Fact]
    public void A_building_resolves_by_external_id_first()
    {
        var api = FakeApi.Answering(("via/external_id/B-EXT", "by-external"));
        var lookup = new ResourceLookup(api, "buildings");

        Buildings.ResolveBuildingId(Unused, "buildings", PropertyId, "Haus A", false, "", "",
                                    lookup, Silent, externalId: "B-EXT")
                 .Should().Be("by-external");

        api.Requests.Should().ContainSingle("a title lookup would be a weaker key");
    }

    [Fact]
    public void A_building_falls_back_to_title_within_its_property()
    {
        var api = FakeApi.Answering(("gridfilter", "by-title"));
        var lookup = new ResourceLookup(api, "buildings");

        Buildings.ResolveBuildingId(Unused, "buildings", PropertyId, "Haus A", false, "", "",
                                    lookup, Silent, externalId: "B-EXT")
                 .Should().Be("by-title");

        var titleRequest = Uri.UnescapeDataString(api.Requests.Last());
        titleRequest.Should().Contain("\"title\"").And.Contain("\"property_id\"");
    }

    // The title alone is not enough: two properties may each have a "Haus A".
    [Fact]
    public void A_building_title_lookup_is_scoped_to_the_property()
    {
        var api = FakeApi.NotFound();
        var lookup = new ResourceLookup(api, "buildings");

        Buildings.ResolveBuildingId(Unused, "buildings", PropertyId, "Haus A", false, "", "",
                                    lookup, Silent);

        Uri.UnescapeDataString(api.Requests.Single()).Should().Contain(PropertyId);
    }

    [Fact]
    public void A_building_without_a_property_or_title_resolves_nothing()
    {
        var api = FakeApi.NotFound();
        var lookup = new ResourceLookup(api, "buildings");

        Buildings.ResolveBuildingId(Unused, "buildings", "", "Haus A", false, "", "", lookup, Silent)
                 .Should().BeNull();
        Buildings.ResolveBuildingId(Unused, "buildings", PropertyId, "", false, "", "", lookup, Silent)
                 .Should().BeNull();
    }

    // An external_id hit seeds the title lookup, so the next row naming the same building by
    // title and property costs nothing.
    [Fact]
    public void A_building_found_by_external_id_seeds_the_title_lookup()
    {
        var api = FakeApi.Answering(("via/external_id/B-EXT", "b-1"));
        var lookup = new ResourceLookup(api, "buildings");

        Buildings.ResolveBuildingId(Unused, "buildings", PropertyId, "Haus A", false, "", "",
                                    lookup, Silent, externalId: "B-EXT");
        var afterFirst = api.Requests.Count;

        Buildings.ResolveBuildingId(Unused, "buildings", PropertyId, "Haus A", false, "", "",
                                    lookup, Silent)
                 .Should().Be("b-1");

        api.Requests.Should().HaveCount(afterFirst, "the title was seeded by the first call");
    }

    // --- floors ------------------------------------------------------------------------

    [Fact]
    public void A_floor_resolves_by_external_id_first()
    {
        var api = FakeApi.Answering(("via/external_id/F-EXT", "by-external"));
        var lookup = new ResourceLookup(api, "floors");

        Floors.ResolveFloorId(Unused, "floors", BuildingId, "EG", false, "", "",
                              lookup, Silent, externalId: "F-EXT")
              .Should().Be("by-external");

        api.Requests.Should().ContainSingle();
    }

    // "EG" exists in every building, so the building has to narrow it.
    [Fact]
    public void A_floor_title_lookup_is_scoped_to_its_building()
    {
        var api = FakeApi.NotFound();
        var lookup = new ResourceLookup(api, "floors");

        Floors.ResolveFloorId(Unused, "floors", BuildingId, "EG", false, "", "", lookup, Silent);

        var url = Uri.UnescapeDataString(api.Requests.Single());
        url.Should().Contain("\"building_id\"").And.Contain(BuildingId);
    }

    [Fact]
    public void A_floor_without_a_building_or_title_resolves_nothing()
    {
        var api = FakeApi.NotFound();
        var lookup = new ResourceLookup(api, "floors");

        Floors.ResolveFloorId(Unused, "floors", "", "EG", false, "", "", lookup, Silent)
              .Should().BeNull();
        Floors.ResolveFloorId(Unused, "floors", BuildingId, "", false, "", "", lookup, Silent)
              .Should().BeNull();
    }

    // --- locations ---------------------------------------------------------------------

    [Fact]
    public void A_location_resolves_by_external_id_before_its_id()
    {
        var api = FakeApi.Answering(("via/external_id/L-EXT", "by-external"));
        var lookup = new ResourceLookup(api, "device_locations");

        Locations.ResolveLocationId(Unused, "device_locations", LocationId, "Raum 1", false, "", "",
                                    lookup, Silent, externalId: "L-EXT")
                 .Should().Be("by-external");

        api.Requests.Should().ContainSingle();
    }

    [Fact]
    public void A_location_resolves_by_its_samedis_id_when_there_is_no_external_id()
    {
        var api = FakeApi.Answering(($"/{LocationId}", LocationId));
        var lookup = new ResourceLookup(api, "device_locations");

        Locations.ResolveLocationId(Unused, "device_locations", LocationId, "Raum 1", false, "", "",
                                    lookup, Silent)
                 .Should().Be(LocationId);
    }

    [Fact]
    public void A_location_falls_back_to_its_title()
    {
        var api = FakeApi.Answering(("gridfilter", "by-title"));
        var lookup = new ResourceLookup(api, "device_locations");

        Locations.ResolveLocationId(Unused, "device_locations", "", "Raum 1", false, "", "",
                                    lookup, Silent)
                 .Should().Be("by-title");
    }

    // Room numbers repeat across the building, so whichever hierarchy ids the row carries
    // have to narrow the search -- and the ones it does not carry must be left out entirely.
    [Fact]
    public void A_location_title_lookup_uses_the_hierarchy_the_row_carries()
    {
        var api = FakeApi.NotFound();
        var lookup = new ResourceLookup(api, "device_locations");

        Locations.ResolveLocationId(Unused, "device_locations", "", "Raum 1", false, "", "",
                                    lookup, Silent, propertyId: PropertyId, buildingId: BuildingId);

        var url = Uri.UnescapeDataString(api.Requests.Single());
        url.Should().Contain("\"property_id\"").And.Contain("\"building_id\"");
        url.Should().NotContain("\"floor_id\"", "the row carries no floor");
    }

    [Fact]
    public void A_location_without_any_key_resolves_nothing()
    {
        var api = FakeApi.NotFound();
        var lookup = new ResourceLookup(api, "device_locations");

        Locations.ResolveLocationId(Unused, "device_locations", "", "", false, "", "", lookup, Silent)
                 .Should().BeNull();
        api.Requests.Should().BeEmpty();
    }

    [Fact]
    public void A_repeated_location_lookup_is_answered_from_memory()
    {
        var api = FakeApi.Answering(("gridfilter", "loc-1"));
        var lookup = new ResourceLookup(api, "device_locations");

        for (var i = 0; i < 4; i++)
            Locations.ResolveLocationId(Unused, "device_locations", "", "Raum 1", false, "", "",
                                        lookup, Silent, floorId: FloorId)
                     .Should().Be("loc-1");

        api.Requests.Should().ContainSingle();
    }
}
