using System.Data;
using FluentAssertions;
using SamedisCare.Api.Http;
using SamedisCare.Api.Lookup;
using SamedisCare.Helper.Config;
using SamedisCare.Helper.Logging;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// The config block that lets a facility say which CSV column carries a device-model key.
/// </summary>
/// <remarks>
/// ConfigStore runs with ignoreUnmatchedProperties: false, so a key the model does not know
/// stops the run. These are the first config tests in this repo; before them nothing here
/// noticed a section that had silently stopped being read.
/// </remarks>
public class CatalogLookupConfigTests
{
    private static AppConfig Parse(string yaml)
        => ConfigStore.Parse<AppConfig>(yaml, ignoreUnmatchedProperties: false);

    [Fact]
    public void Both_kinds_of_mapping_are_read()
    {
        var config = Parse(@"
inventories:
  catalog_lookup:
    enabled: true
    mappings:
      - column: quell_artikelnummer
        field: external_id
      - column: emtec_code
        regulatory: emtec_code
        upcase: true
");

        var lookup = config.Inventories.CatalogLookup;
        lookup.Enabled.Should().BeTrue();
        lookup.Mappings.Should().HaveCount(2);

        lookup.Mappings[0].Column.Should().Be("quell_artikelnummer");
        lookup.Mappings[0].Field.Should().Be("external_id");
        lookup.Mappings[0].Regulatory.Should().BeNull();
        lookup.Mappings[0].Upcase.Should().BeFalse();

        lookup.Mappings[1].Regulatory.Should().Be("emtec_code");
        lookup.Mappings[1].Upcase.Should().BeTrue();
    }

    // Every config.yml in the field predates this block, so its absence has to be ordinary.
    [Fact]
    public void A_config_without_the_block_is_off_and_empty()
    {
        var config = Parse("samedis:\n  tenant_id: \"507f1f77bcf86cd799439001\"\n");

        config.Inventories.CatalogLookup.Enabled.Should().BeFalse();
        config.Inventories.CatalogLookup.Mappings.Should().BeEmpty();
    }

    // fillNullSections: a section written with nothing under it must still arrive as an
    // object, not as null -- otherwise the first property read throws.
    [Fact]
    public void An_empty_block_arrives_as_defaults()
    {
        var config = Parse("inventories:\n");

        config.Inventories.Should().NotBeNull();
        config.Inventories.CatalogLookup.Enabled.Should().BeFalse();
    }

    // The example is what a new installation is copied from, and the loader rejects an
    // unknown key outright -- so a key added to the example but not to AppConfig, or renamed
    // on AppConfig and left in the example, stops the first run instead of the build.
    [Fact]
    public void The_shipped_example_parses()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "config.yml.example");
        File.Exists(path).Should().BeTrue();

        var config = Parse(File.ReadAllText(path));

        config.Samedis.ApiVersion.Should().Be("v4");
        config.Inventories.CatalogLookup.Enabled.Should().BeFalse();
        config.Inventories.CatalogLookup.Mappings.Should().BeEmpty();
    }
}

/// <summary>
/// Validation of the mapping, which runs once at startup.
/// </summary>
/// <remarks>
/// Every way of getting this wrong fails silently against the server: an unknown regulatory
/// label is sliced out of the filter and the endpoint answers with no records, which reads as
/// "this device model does not exist" -- the answer that makes a sync create a duplicate. So
/// the run stops here instead.
/// </remarks>
public class CatalogLookupValidationTests
{
    private static CatalogLookupConfig Config(params CatalogLookupMapping[] mappings)
        => new() { Enabled = true, Mappings = mappings.ToList() };

    private static Action Validating(params CatalogLookupMapping[] mappings)
        => () => CatalogLookup.Validate(Config(mappings));

    [Theory]
    [InlineData("udi_id")]
    [InlineData("eudamed_id")]
    [InlineData("eudamed_di")]
    [InlineData("emtec_id")]
    [InlineData("emtec_code")]
    [InlineData("emdn_code")]
    [InlineData("gmdn_code")]
    public void An_identifier_is_accepted(string label)
        => CatalogLookup.Validate(Config(new CatalogLookupMapping { Column = "c", Regulatory = label }))
                        .Should().ContainSingle();

    [Fact]
    public void External_id_is_accepted_as_a_field()
        => CatalogLookup.Validate(Config(new CatalogLookupMapping { Column = "c", Field = "external_id" }))
                        .Should().ContainSingle();

    // The server would run this filter -- eu_mdr is a label it accepts. It is rejected here
    // because the answer names a risk class, not a device: thousands of models match and the
    // cascade would take an arbitrary one, handing back an id rather than an error.
    [Theory]
    [InlineData("eu_mdr")]
    [InlineData("us_fda")]
    [InlineData("ce")]
    [InlineData("umdns_code")]
    public void A_classification_is_rejected(string label)
        => Validating(new CatalogLookupMapping { Column = "c", Regulatory = label })
               .Should().Throw<ArgumentException>().WithMessage("*" + label + "*");

    [Fact]
    public void A_field_other_than_external_id_is_rejected()
        => Validating(new CatalogLookupMapping { Column = "c", Field = "device_number" })
               .Should().Throw<ArgumentException>();

    [Fact]
    public void A_mapping_naming_both_a_field_and_a_regulatory_key_is_rejected()
        => Validating(new CatalogLookupMapping { Column = "c", Field = "external_id", Regulatory = "emtec_code" })
               .Should().Throw<ArgumentException>().WithMessage("*both*");

    [Fact]
    public void A_mapping_naming_neither_is_rejected()
        => Validating(new CatalogLookupMapping { Column = "c" })
               .Should().Throw<ArgumentException>().WithMessage("*neither*");

    [Fact]
    public void A_mapping_without_a_column_is_rejected()
        => Validating(new CatalogLookupMapping { Regulatory = "emtec_code" })
               .Should().Throw<ArgumentException>();

    // Switched off, nothing is checked and nothing is used -- a half-written mapping left in
    // the file must not stop a run that does not ask for it.
    [Fact]
    public void A_disabled_block_validates_to_nothing()
        => CatalogLookup.Validate(new CatalogLookupConfig
        {
            Enabled = false,
            Mappings = new List<CatalogLookupMapping> { new() { Column = "c", Regulatory = "eu_mdr" } }
        }).Should().BeEmpty();

    [Fact]
    public void A_missing_block_validates_to_nothing()
        => CatalogLookup.Validate(null).Should().BeEmpty();
}

/// <summary>
/// Reading one source row through the mapping.
/// </summary>
public class CatalogLookupRowTests
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

    private static IReadOnlyList<CatalogLookupMapping> Mappings(params CatalogLookupMapping[] m)
        => CatalogLookup.Validate(new CatalogLookupConfig { Enabled = true, Mappings = m.ToList() });

    // The order the file lists them in is the order they are tried in, so it has to survive.
    [Fact]
    public void The_configured_order_is_kept()
    {
        var keys = CatalogLookup.ValuesFrom(
            Row(("emtec_id", "E-1"), ("emtec_code", "EC-9")),
            Mappings(new CatalogLookupMapping { Column = "emtec_code", Regulatory = "emtec_code" },
                     new CatalogLookupMapping { Column = "emtec_id", Regulatory = "emtec_id" }));

        keys.Regulatory.Select(r => r.Label).Should().Equal("emtec_code", "emtec_id");
        keys.Regulatory.Select(r => r.Value).Should().Equal("EC-9", "E-1");
    }

    [Fact]
    public void An_external_id_mapping_lands_on_its_own_key()
    {
        var keys = CatalogLookup.ValuesFrom(
            Row(("artikelnummer", "A-42")),
            Mappings(new CatalogLookupMapping { Column = "artikelnummer", Field = "external_id" }));

        keys.ExternalId.Should().Be("A-42");
        keys.Regulatory.Should().BeEmpty();
    }

    // A column the export does not have reads as an empty cell, so a partially filled file
    // still resolves the rows that do carry a key.
    [Fact]
    public void A_missing_column_and_a_blank_cell_both_drop_out()
    {
        var keys = CatalogLookup.ValuesFrom(
            Row(("emtec_code", "   ")),
            Mappings(new CatalogLookupMapping { Column = "emtec_code", Regulatory = "emtec_code" },
                     new CatalogLookupMapping { Column = "not_in_this_csv", Regulatory = "udi_id" }));

        keys.Any.Should().BeFalse();
        keys.Regulatory.Should().BeEmpty();
    }

    // The server matches exactly and case-sensitively, and emtec codes are stored upper-cased.
    [Fact]
    public void Upcase_applies_only_where_it_is_set()
    {
        var keys = CatalogLookup.ValuesFrom(
            Row(("emtec_code", "ec-9"), ("udi", "abc123")),
            Mappings(new CatalogLookupMapping { Column = "emtec_code", Regulatory = "emtec_code", Upcase = true },
                     new CatalogLookupMapping { Column = "udi", Regulatory = "udi_id" }));

        keys.Regulatory.Select(r => r.Value).Should().Equal("EC-9", "abc123");
    }

    // The cascade takes one external_id, so a second column filled in the same row must not
    // silently replace the first.
    [Fact]
    public void The_first_filled_external_id_column_wins()
    {
        var keys = CatalogLookup.ValuesFrom(
            Row(("primary", "P-1"), ("secondary", "S-2")),
            Mappings(new CatalogLookupMapping { Column = "primary", Field = "external_id" },
                     new CatalogLookupMapping { Column = "secondary", Field = "external_id" }));

        keys.ExternalId.Should().Be("P-1");
    }

    [Fact]
    public void Without_mappings_a_row_offers_nothing()
        => CatalogLookup.ValuesFrom(Row(("emtec_code", "EC-9")), Array.Empty<CatalogLookupMapping>())
                        .Any.Should().BeFalse();
}

/// <summary>
/// What the configured keys change about resolving an inventory row's device model.
/// </summary>
public class CatalogLookupResolutionTests
{
    private const string TenantId = "507f1f77bcf86cd799439001";
    private const string Unknown = "507f1f77bcf86cd799439011";
    private static readonly ISyncLog Silent = new NullSyncLog();

    private static DeviceModels.InventoryCatalogContext Context(IApiClient api)
        => new(api, TenantId,
               new ResourceLookup(api, "device_models"),
               new ResourceLookup(api, "device_types"),
               new ResourceLookup(api, "contacts"),
               new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
               MayCreateLocalDeviceModels: false, Silent);

    private static CatalogLookup.Keys EmtecCode(string value = "EC-9")
        => new(null, new[] { ("emtec_code", (string?)value) });

    private static string Resolve(DeviceModels.InventoryCatalogContext ctx,
                                  CatalogLookup.Keys? keys,
                                  string sourceCatalogId = "",
                                  bool isCreateOperation = true)
        => DeviceModels.ResolveCatalogIdForInventoryRow(
             ctx, sourceCatalogId, "Perfusor Space", "B. Braun", "Spritzenpumpe",
             isCreateOperation, isPlaceholder: false, keys: keys);

    // The point of the feature: a row whose title does not match the catalog's wording is
    // created instead of skipped.
    [Fact]
    public void A_regulatory_key_resolves_the_model_before_the_title_is_tried()
    {
        var api = FakeApi.Answering(("filter[regulatory][emtec_code]", "by-emtec"));

        Resolve(Context(api), EmtecCode()).Should().Be("by-emtec");

        api.Requests.Should().ContainSingle();
        api.Requests.Single().Should().Contain("filter[scope]=public_and_tenant");
    }

    // One eudamed_id covers both "Perfusor Space" and "Perfusor Space PCA" in production, so
    // an identifier is asked together with the title before it is trusted alone.
    [Fact]
    public void A_regulatory_key_is_narrowed_by_the_title_first()
    {
        var api = FakeApi.NotFound();

        Resolve(Context(api), EmtecCode());

        var regulatory = api.Requests.Where(r => r.Contains("filter[regulatory]")).ToList();
        regulatory.Should().HaveCount(2);
        Uri.UnescapeDataString(regulatory[0]).Should().Contain("\"title\"").And.Contain("Perfusor+Space");
        regulatory[1].Should().NotContain("gridfilter");
    }

    [Fact]
    public void Without_configured_keys_nothing_changes()
    {
        var api = FakeApi.Answering(("manufacturer_according_to_type_plate", "by-type-plate"));

        Resolve(Context(api), keys: null).Should().Be("by-type-plate");

        api.Requests.Should().ContainSingle();
        api.Requests.Should().NotContain(r => r.Contains("regulatory"));
    }

    // Without the key this row is a write the backend rejects with "Device model can't be
    // blank" -- the same message as sending no model at all.
    [Fact]
    public void An_unresolvable_catalog_id_is_rescued_by_a_key()
    {
        var api = FakeApi.Answering(("filter[regulatory][emtec_code]", "by-emtec"));

        Resolve(Context(api), EmtecCode(), sourceCatalogId: Unknown).Should().Be("by-emtec");
    }

    // device_model_merges.csv reports merges the backend resolved, which the source system
    // can act on by updating its own reference. A client-side substitution is not that.
    [Fact]
    public void A_rescue_is_not_reported_as_a_merge()
    {
        var api = FakeApi.Answering(("filter[regulatory][emtec_code]", "by-emtec"));
        var ctx = Context(api);

        Resolve(ctx, EmtecCode(), sourceCatalogId: Unknown);

        ctx.Remaps.Should().BeEmpty();
    }

    // An existing device keeps the model it has: the same reasoning that keeps local model
    // creation create-only (samedis-care-issues#2347). A key pointing elsewhere would move it.
    [Fact]
    public void An_update_never_uses_the_keys()
    {
        var api = FakeApi.Answering(("filter[regulatory][emtec_code]", "by-emtec"));

        Resolve(Context(api), EmtecCode(), sourceCatalogId: Unknown, isCreateOperation: false);

        api.Requests.Should().NotContain(r => r.Contains("regulatory"));
    }

    // Found by the integration test against the test stack: a row rescued on create failed
    // on EVERY later run, because the source keeps sending the same dead id and the update
    // sent it on. An empty result leaves the attribute out of the payload, so the device
    // keeps the model it already has and the year, location and status still land.
    [Fact]
    public void An_unresolvable_catalog_id_is_left_out_of_an_update()
    {
        var api = FakeApi.NotFound();

        Resolve(Context(api), EmtecCode(), sourceCatalogId: Unknown, isCreateOperation: false)
            .Should().BeEmpty();
    }

    // On a create the opposite: there is no device yet, so nothing is protected by dropping
    // the id, and the backend's rejection stays the diagnosis. Pinned by
    // MergedCatalogTests.An_unknown_catalog_id_is_sent_unchanged_with_a_warning as well.
    [Fact]
    public void An_unresolvable_catalog_id_is_still_sent_on_a_create()
    {
        var api = FakeApi.NotFound();

        Resolve(Context(api), keys: null, sourceCatalogId: Unknown).Should().Be(Unknown);
    }

    // A key repeated across rows costs one request per run, not one per row.
    [Fact]
    public void A_repeated_key_is_answered_from_memory()
    {
        var api = FakeApi.Answering(("filter[regulatory][emtec_code]", "id-1"));
        var ctx = Context(api);

        for (var i = 0; i < 4; i++)
            Resolve(ctx, EmtecCode()).Should().Be("id-1");

        api.Requests.Should().ContainSingle();
    }
}
