using System.Data;
using FluentAssertions;
using SamedisCare.Helper.Text;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// The API rejects an ownership outside its enum with HTTP 400 for the whole row, so what this
/// translation sends decides whether an inventory update arrives at all
/// (samedis-care-issues#3601: every NOVA update carrying "Eigen" failed).
/// </summary>
public class OwnershipMappingTests
{
    private static OwnershipMapping Ukt() => OwnershipMapping.Validate(new List<OwnershipMappingEntry>
    {
        new() { Source = "Eigen", Target = "owned" },
        new() { Source = "Leihe", Target = "loaner-device" },
        new() { Source = "Miete", Target = "rental-leasing" },
        new() { Source = "NN", Target = "" },
    });

    private static DataRow Row(string ownership)
    {
        var table = new DataTable();
        table.Columns.Add("ownership");
        table.Columns.Add("inventory_number");
        var row = table.NewRow();
        row["ownership"] = ownership;
        row["inventory_number"] = "INV-1";
        table.Rows.Add(row);
        return row;
    }

    [Theory]
    [InlineData("Eigen", "owned")]
    [InlineData("eigen", "owned")]
    [InlineData("  EIGEN ", "owned")]
    [InlineData("Leihe", "loaner-device")]
    [InlineData("Miete", "rental-leasing")]
    public void A_configured_label_is_sent_as_its_target(string source, string expected)
        => Ukt().Resolve(source).Should().Be(expected);

    [Fact]
    public void A_label_mapped_to_empty_is_left_out()
        => Ukt().Resolve("NN").Should().BeEmpty();

    // The old normalizer turned '-' into '_', which broke the API's own values.
    [Theory]
    [InlineData("loaner-device")]
    [InlineData("rental-leasing")]
    [InlineData("pay-per-use")]
    [InlineData("Owned")]
    public void An_API_value_passes_through_unchanged(string value)
        => OwnershipMapping.Validate(null).Resolve(value).Should().Be(value.ToLowerInvariant());

    [Fact]
    public void An_unknown_label_is_left_out_and_counted_once_per_value()
    {
        var mapping = Ukt();
        mapping.Resolve("Privat").Should().BeEmpty();
        mapping.Resolve("privat").Should().BeEmpty();
        mapping.Resolve("Kauf").Should().BeEmpty();

        mapping.Unmapped.Should().BeEquivalentTo(new Dictionary<string, int> { ["Privat"] = 2, ["Kauf"] = 1 });
    }

    [Fact]
    public void A_mapped_label_is_not_reported_as_unmapped()
    {
        var mapping = Ukt();
        mapping.Resolve("Eigen");
        mapping.Resolve("NN");
        mapping.Resolve("");
        mapping.Unmapped.Should().BeEmpty();
    }

    [Fact]
    public void The_payload_carries_the_mapped_value()
        => Inventories.BuildInventoryAttributes(Row("Eigen"), null, null, NumberFormat.Comma, ownership: Ukt())
            ["ownership"].Should().Be("owned");

    // The rest of the row must still be written; sending the label would fail all of it.
    [Fact]
    public void An_unmapped_label_drops_only_the_ownership_from_the_payload()
    {
        var attributes = Inventories.BuildInventoryAttributes(Row("Privat"), null, null, NumberFormat.Comma, ownership: Ukt());
        attributes.Should().NotContainKey("ownership");
        attributes["device_number"].Should().Be("INV-1");
    }

    [Fact]
    public void A_target_the_API_does_not_accept_stops_the_run()
    {
        var act = () => OwnershipMapping.Validate(new List<OwnershipMappingEntry> { new() { Source = "Eigen", Target = "own" } });
        act.Should().Throw<ArgumentException>().WithMessage("*'own' is not an ownership value*");
    }

    [Fact]
    public void An_entry_without_source_stops_the_run()
    {
        var act = () => OwnershipMapping.Validate(new List<OwnershipMappingEntry> { new() { Source = " ", Target = "owned" } });
        act.Should().Throw<ArgumentException>().WithMessage("*has no source*");
    }

    [Fact]
    public void A_source_mapped_twice_stops_the_run()
    {
        var act = () => OwnershipMapping.Validate(new List<OwnershipMappingEntry>
        {
            new() { Source = "Eigen", Target = "owned" },
            new() { Source = "EIGEN", Target = "patient" },
        });
        act.Should().Throw<ArgumentException>().WithMessage("*mapped more than once*");
    }
}
