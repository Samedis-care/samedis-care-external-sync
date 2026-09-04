using System.Data;
using FluentAssertions;
using SamedisCare.Helper.Text;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// BuildInventoryAttributes turns one source row into the payload sent to the API, so what it
/// does with an absent, padded or oddly formatted cell decides what ends up in the record.
/// </summary>
public class InventoryAttributeTests
{
    private static DataRow Row(params (string Column, string Value)[] cells)
    {
        var table = new DataTable();
        foreach (var (column, _) in cells) table.Columns.Add(column);
        var row = table.NewRow();
        foreach (var (column, value) in cells) row[column] = value;
        table.Rows.Add(row);
        return row;
    }

    private static Dictionary<string, object> Build(DataRow row, NumberFormat? numbers = null)
        => Inventories.BuildInventoryAttributes(row, null, null, numbers ?? NumberFormat.Comma);

    [Fact]
    public void The_inventory_number_becomes_the_device_number()
        => Build(Row(("inventory_number", "INV-1")))["device_number"].Should().Be("INV-1");

    [Fact]
    public void The_external_id_is_carried_over()
        => Build(Row(("external_id", "EXT-1")))["external_id"].Should().Be("EXT-1");

    // A cell the source left empty must not become an empty string in the payload: that would
    // overwrite whatever the record already holds.
    [Fact]
    public void An_empty_cell_is_left_out_of_the_payload()
        => Build(Row(("external_id", ""), ("inventory_number", "INV-1")))
            .Should().NotContainKey("external_id");

    // A SQL export to CSV writes an absent value as the four characters NULL.
    [Theory]
    [InlineData("NULL")]
    [InlineData("null")]
    public void The_literal_text_NULL_is_treated_as_absent(string stored)
        => Build(Row(("serial_number", stored), ("inventory_number", "INV-1")))
            .Should().NotContainKey("serial_number");

    [Fact]
    public void Padded_values_are_trimmed()
        => Build(Row(("serial_number", "  SN-1  ")))["serial_number"].Should().Be("SN-1");

    // The decimal separator is a per-installation setting, so the same cell means different
    // numbers under different configurations. Getting this wrong writes a price off by a
    // factor of a hundred.
    [Fact]
    public void A_price_is_read_with_the_configured_separator()
    {
        var german = Build(Row(("purchase_price", "1234,56")), NumberFormat.Comma);
        var invariant = Build(Row(("purchase_price", "1234.56")), NumberFormat.Dot);

        german["purchase_price"].Should().Be(invariant["purchase_price"]);
    }

    // Recorded, not endorsed. Under the German convention a dot is the GROUP separator, so a
    // file written in the invariant convention turns 1234.56 into 123456 -- a price off by a
    // factor of a hundred, with nothing in the log to show for it. The parse is lax about
    // where the group separator sits, so "1234.56" is accepted even though a real grouped
    // number would read "1.234,56". Tightening this changes number handling for every tool,
    // so it is surfaced here rather than changed in passing.
    [Fact]
    public void A_price_written_in_the_other_convention_is_silently_misread()
    {
        var attributes = Build(Row(("purchase_price", "1234.56")), NumberFormat.Comma);

        attributes["purchase_price"].Should().Be(123456m,
            "the dot is read as a group separator - this is the hazard, not the intent");
    }

    [Fact]
    public void A_properly_grouped_german_price_reads_correctly()
        => Build(Row(("purchase_price", "1.234,56")), NumberFormat.Comma)["purchase_price"]
            .Should().Be(1234.56m);

    [Fact]
    public void A_non_numeric_price_is_left_out()
        => Build(Row(("purchase_price", "auf Anfrage"), ("inventory_number", "INV-1")))
            .Should().NotContainKey("purchase_price");

    [Fact]
    public void A_catalog_id_override_wins_over_the_source_column()
    {
        var row = Row(("catalog_id", "from-source"), ("inventory_number", "INV-1"));

        Inventories.BuildInventoryAttributes(row, null, null, NumberFormat.Comma,
                                             catalogIdOverride: "resolved")["catalog_id"]
            .Should().Be("resolved");
    }

    [Fact]
    public void The_source_catalog_id_is_used_when_there_is_no_override()
        => Build(Row(("catalog_id", "from-source")))["catalog_id"].Should().Be("from-source");

    [Fact]
    public void A_missing_column_is_simply_absent()
        => ((Action)(() => Build(Row(("inventory_number", "INV-1")))))
            .Should().NotThrow("a source export need not carry every column");
}

/// <summary>
/// The value maps moved to SamedisCare.Api (CatalogValues), together with the tests that
/// hold what MdrRiskClassMap does with a risk_level it was never meant to see. Keeping a
/// second copy here would be the duplication that prompted the move.
/// </summary>

/// <summary>
/// Dates the source writes in whatever format its system uses.
/// </summary>
public class DateNormalisationTests
{
    [Theory]
    [InlineData("2026-08-28", "2026-08-28")]
    [InlineData("2026-08-28T14:30:00", "2026-08-28")]
    public void An_understood_date_becomes_an_iso_day(string input, string expected)
        => Helper.NormalizeDate(input).Should().Be(expected);

    // Anything it cannot read is passed through rather than dropped, so a value the API might
    // still understand is not lost on the way.
    [Theory]
    [InlineData("  irgendwann  ", "irgendwann")]
    [InlineData("00.00.0000", "00.00.0000")]
    public void An_unreadable_value_is_passed_through_trimmed(string input, string expected)
        => Helper.NormalizeDate(input).Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_value_stays_empty(string input)
        => Helper.NormalizeDate(input).Should().BeEmpty();
}
