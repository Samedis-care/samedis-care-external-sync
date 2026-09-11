using FluentAssertions;
using SamedisCare.Api.Lookup;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// The inventory cascade is where a wrong answer is expensive: resolving to the wrong record
/// makes the sync update someone else's device, and resolving to nothing makes it create a
/// duplicate of a device that is already there.
/// </summary>
public class InventoryResolutionTests
{
    private const string Oid = "507f1f77bcf86cd799439011";
    private const string OtherOid = "507f1f77bcf86cd799439022";

    private static ResourceLookup Lookup(FakeApi api) => new(api, "inventories");

    [Fact]
    public void The_samedis_id_wins_over_everything_else()
    {
        var api = FakeApi.Answering(($"/{Oid}", Oid));
        var lookup = Lookup(api);

        Inventories.ResolveExistingInventoryId(lookup, Oid, "EXT-1", "DEV-1", true)
                   .Should().Be(Oid);

        api.Requests.Should().ContainSingle("no weaker key may be consulted after a hit");
    }

    [Fact]
    public void The_external_id_is_used_when_there_is_no_samedis_id()
    {
        var api = FakeApi.Answering(("via/external_id/EXT-1", "by-external"));

        Inventories.ResolveExistingInventoryId(Lookup(api), "", "EXT-1", "DEV-1", true)
                   .Should().Be("by-external");
    }

    // The invariant the original implementation documented at length. The source may deliver
    // a changed inventory number for a device whose external_id still matches; falling
    // through would pick a DIFFERENT record, and the update would then try to move this row's
    // external_id onto it -- which the collation-insensitive unique index on
    // (tenant_id, external_id) rejects as a duplicate key.
    [Fact]
    public void An_external_id_hit_is_final_even_when_the_inventory_number_changed()
    {
        var api = FakeApi.Answering(
            ("via/external_id/EXT-1", "the-right-record"),
            ("gridfilter", "a-different-record"));

        Inventories.ResolveExistingInventoryId(Lookup(api), "", "EXT-1", "DEV-CHANGED", true)
                   .Should().Be("the-right-record");

        api.Requests.Should().NotContain(r => r.Contains("gridfilter"));
    }

    [Fact]
    public void The_inventory_number_is_the_last_resort()
    {
        var api = FakeApi.Answering(("gridfilter", "by-number"));

        Inventories.ResolveExistingInventoryId(Lookup(api), "", "EXT-1", "DEV-1", true)
                   .Should().Be("by-number");

        api.Requests[0].Should().Contain("via/external_id/EXT-1");
        api.Requests[1].Should().Contain("gridfilter");
    }

    [Fact]
    public void The_inventory_number_can_be_switched_off()
    {
        var api = FakeApi.Answering(("gridfilter", "by-number"));

        Inventories.ResolveExistingInventoryId(Lookup(api), "", "EXT-1", "DEV-1", false)
                   .Should().BeNull();

        api.Requests.Should().NotContain(r => r.Contains("gridfilter"));
    }

    [Fact]
    public void Nothing_resolves_when_the_row_carries_no_keys()
    {
        var api = FakeApi.NotFound();

        Inventories.ResolveExistingInventoryId(Lookup(api), "", "", "", true).Should().BeNull();
        api.Requests.Should().BeEmpty("there is nothing to ask about");
    }

    // Source data routinely carries free text or a placeholder in an id column. Asking the
    // API about it only costs a round trip.
    [Theory]
    [InlineData("n/a")]
    [InlineData("-")]
    [InlineData("507f1f77bcf86cd79943901")]
    public void A_malformed_samedis_id_is_not_sent_to_the_server(string id)
    {
        var api = FakeApi.NotFound();

        Inventories.ResolveExistingInventoryId(Lookup(api), id, "", "", true);

        api.Requests.Should().BeEmpty();
    }

    [Fact]
    public void The_device_number_lookup_asks_for_the_small_serializer_variant()
    {
        var api = FakeApi.NotFound();

        Inventories.ResolveInventoryIdByDeviceNumber(Lookup(api), "DEV-1");

        api.Requests.Single().Should().Contain("variant=regular");
    }

    [Fact]
    public void A_device_number_that_matches_nothing_yields_an_empty_string()
        => Inventories.ResolveInventoryIdByDeviceNumber(Lookup(FakeApi.NotFound()), "DEV-1")
                      .Should().BeEmpty();

    [Fact]
    public void A_blank_device_number_is_not_sent_to_the_server()
    {
        var api = FakeApi.NotFound();

        Inventories.ResolveInventoryIdByDeviceNumber(Lookup(api), "   ").Should().BeEmpty();
        api.Requests.Should().BeEmpty();
    }

    // Repeated rows referring to the same device must not each cost a request.
    [Fact]
    public void A_repeated_lookup_is_answered_from_memory()
    {
        var api = FakeApi.Answering(("gridfilter", "id-7"));
        var lookup = Lookup(api);

        for (var i = 0; i < 5; i++)
            Inventories.ResolveInventoryIdByDeviceNumber(lookup, "DEV-1").Should().Be("id-7");

        api.Requests.Should().ContainSingle();
    }

    [Fact]
    public void A_repeated_miss_is_also_answered_from_memory()
    {
        var api = FakeApi.NotFound();
        var lookup = Lookup(api);

        for (var i = 0; i < 5; i++)
            Inventories.ResolveInventoryIdByDeviceNumber(lookup, "GONE").Should().BeEmpty();

        api.Requests.Should().ContainSingle("a miss is remembered too");
    }

    // The requests upload used to return the source's inventory_id unchecked, so a value that
    // was not an id -- or named a record this tenant cannot read -- was passed on as if it had
    // been resolved and only failed later, on the write.
    [Fact]
    public void An_unverifiable_source_id_falls_through_to_the_device_number()
    {
        var api = FakeApi.Answering(("gridfilter", "by-number"));

        Inventories.ResolveInventoryIdByIdOrDeviceNumber(Lookup(api), "not-an-id", "DEV-1")
                   .Should().Be("by-number");
    }

    [Fact]
    public void A_source_id_the_tenant_cannot_read_falls_through_too()
    {
        var api = FakeApi.Answering(("gridfilter", "by-number"));

        Inventories.ResolveInventoryIdByIdOrDeviceNumber(Lookup(api), OtherOid, "DEV-1")
                   .Should().Be("by-number");
    }

    [Fact]
    public void A_verifiable_source_id_is_used_directly()
    {
        var api = FakeApi.Answering(($"/{Oid}", Oid));

        Inventories.ResolveInventoryIdByIdOrDeviceNumber(Lookup(api), Oid, "DEV-1")
                   .Should().Be(Oid);

        api.Requests.Should().NotContain(r => r.Contains("gridfilter"));
    }

    // A 5xx says the lookup could not be answered. Reading it as "no such record" is what
    // makes a sync create a duplicate of something that is already there.
    [Fact]
    public void A_server_error_is_not_mistaken_for_a_missing_record()
    {
        var lookup = Lookup(FakeApi.AlwaysStatus(500));

        var act = () => Inventories.ResolveExistingInventoryId(lookup, "", "EXT-1", "DEV-1", true);

        act.Should().Throw<LookupUnavailableException>();
    }

    [Fact]
    public void A_record_that_is_genuinely_absent_stays_absent()
        => Inventories.ResolveExistingInventoryId(Lookup(FakeApi.AlwaysStatus(404)),
                                                  "", "EXT-1", "DEV-1", true)
                      .Should().BeNull();
}
