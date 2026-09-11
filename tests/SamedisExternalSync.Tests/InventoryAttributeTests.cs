using System.Data;
using System.Globalization;
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

/// <summary>
/// The source exports are German CSVs with day-first dates. Parsing them must not depend on
/// the locale the tool happens to run under: the customer hosts are German-localised, but a
/// container without a locale, or an English Windows server, would otherwise read 03.04.2026
/// as 4 March and write that onto the inventory record without any parse failure to notice.
/// </summary>
public class DateCultureTests
{
    private static readonly string[] HostCultures = { "de-DE", "en-US", "" };

    private static string UnderCulture(string cultureName, Func<string> body)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = cultureName.Length == 0
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(cultureName);
            return body();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void A_day_first_date_keeps_its_day_on_every_host()
    {
        foreach (var culture in HostCultures)
            UnderCulture(culture, () => Helper.NormalizeDate("03.04.2026"))
                .Should().Be("2026-04-03", $"03.04.2026 is 3 April, also under '{culture}'");
    }

    // The shape the fixtures actually carry (tests/fixtures/to_samedis/inventories.csv).
    [Fact]
    public void A_day_first_date_with_a_time_keeps_its_day_on_every_host()
    {
        foreach (var culture in HostCultures)
            UnderCulture(culture, () => Helper.NormalizeDate("01.02.2004 00:00:00"))
                .Should().Be("2004-02-01", $"01.02.2004 is 1 February, also under '{culture}'");
    }

    // A dotted date with a time but no seconds: covered by an explicit format, so the culture
    // order never gets a say. It is listed for exactly that reason -- it is the shape a source
    // export produces most often after the seconds-bearing one.
    [Theory]
    [InlineData("03.04.2026 08:30")]
    [InlineData("3.4.2026 8:30")]
    public void A_dotted_date_with_a_time_but_no_seconds_is_day_first(string input)
    {
        foreach (var culture in HostCultures)
            UnderCulture(culture, () => Helper.NormalizeDate(input))
                .Should().Be("2026-04-03", $"'{input}' is 3 April, also under '{culture}'");
    }

    // These shapes are NOT in Helper.DateFormats, so they reach the free-parse fallback -- the
    // one branch where the de-DE-before-invariant culture order decides the answer. Measured
    // with an invariant-first cascade: all three come back as 2026-03-04. Do not reorder.
    [Theory]
    [InlineData("03.04.2026 08:30:15.500")]
    [InlineData("3.4.26")]
    [InlineData("03-04-2026")]
    public void A_shape_reaching_the_free_parse_is_still_day_first(string input)
    {
        foreach (var culture in HostCultures)
            UnderCulture(culture, () => Helper.NormalizeDate(input))
                .Should().Be("2026-04-03", $"the free-parse fallback must stay day-first for '{input}' under '{culture}'");
    }

    [Fact]
    public void An_iso_date_is_unaffected_by_the_host_culture()
    {
        foreach (var culture in HostCultures)
            UnderCulture(culture, () => Helper.NormalizeDate("2026-04-03"))
                .Should().Be("2026-04-03");
    }

    // Tasks.NormalizeTaskDate was a second copy of this logic; done_at goes through the same
    // parser now, so its behaviour is pinned here rather than assumed.
    [Fact]
    public void A_task_done_at_in_german_form_becomes_an_iso_day()
    {
        foreach (var culture in HostCultures)
            UnderCulture(culture, () => Helper.NormalizeDate("03.04.2026 14:30:00"))
                .Should().Be("2026-04-03");
    }
}
