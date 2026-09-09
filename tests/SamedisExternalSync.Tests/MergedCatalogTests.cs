using System.Data;
using FluentAssertions;
using SamedisCare.Api.Http;
using SamedisCare.Api.Lookup;
using SamedisCare.Helper.Logging;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// Device models can be merged in samedis: the merge hard-destroys the source record, moves
/// its inventories to the survivor and carries only the source's <b>id</b> across, in
/// <c>merged_catalog_ids</c> (samedis-care-issues#2347). Title, manufacturer and external_id
/// die with it.
/// <para>
/// Both directions of the sync have to survive that, and neither failure is loud: an import
/// that recreates the merged-away model undoes the merge without an error, and an export that
/// omits the historic ids leaves the source system sending one forever.
/// </para>
/// </summary>
public class MergedCatalogTests
{
    private const string TenantId = "507f1f77bcf86cd799439001";
    private const string Historic = "507f1f77bcf86cd799439011";
    private const string Survivor = "507f1f77bcf86cd799439099";

    private static (int, string) Found(string id) => (200, $"{{\"data\":[{{\"id\":\"{id}\"}}],\"meta\":{{\"total\":1}}}}");
    private static (int, string) NotFound() => (404, "{\"meta\":{\"msg\":{\"success\":false,\"error\":\"record_not_found_error\"}}}");

    private static DeviceModels.InventoryCatalogContext Context(IApiClient api, ISyncLog log,
                                                                bool mayCreate = true)
        => new(api, TenantId,
               new ResourceLookup(api, "device_models"),
               new ResourceLookup(api, "device_types"),
               new ResourceLookup(api, "contacts"),
               new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
               mayCreate, log);

    private static string Resolve(DeviceModels.InventoryCatalogContext ctx,
                                  string sourceCatalogId = "",
                                  bool isCreateOperation = true,
                                  string title = "Perfusor Space",
                                  string maker = "B. Braun",
                                  string type = "Spritzenpumpe")
        => DeviceModels.ResolveCatalogIdForInventoryRow(
             ctx, sourceCatalogId, title, maker, type, isCreateOperation, isPlaceholder: false);

    // ---------------------------------------------------------------- the import direction

    // The one that mattered. An operator merges two device models; the source system keeps
    // sending the title of the one that is gone. Looking it up finds nothing -- so the old
    // code created it again and wrote the fresh id onto the inventory the merge job had just
    // moved to the survivor. The merge was undone on the next run, and every run after it,
    // with Errors: 0 in the log.
    [Fact]
    public void An_update_never_creates_a_device_model()
    {
        var api = new FakeApiWithWrites(
            get: url => url.StartsWith("device_models") ? NotFound()
               : url.StartsWith("device_types") ? Found("type-1")
               : Found("maker-1"),
            post: _ => (201, "{\"data\":{\"id\":\"recreated\"}}"));

        Resolve(Context(api, new NullSyncLog()), isCreateOperation: false).Should().BeEmpty();
        api.Posts.Should().BeEmpty();
    }

    // An empty answer is not a failure on an update: BuildInventoryAttributes leaves a blank
    // catalog_id out of the payload, so the inventory keeps the device model it has. That is
    // the whole point -- the merge survives.
    [Fact]
    public void An_update_that_resolves_nothing_says_so_without_an_error()
    {
        var log = new RecordingSyncLog();
        var api = new FakeApiWithWrites(
            get: url => url.StartsWith("device_models") ? NotFound()
               : url.StartsWith("device_types") ? Found("type-1")
               : Found("maker-1"),
            post: _ => (201, "{\"data\":{\"id\":\"recreated\"}}"));

        Resolve(Context(api, log), isCreateOperation: false);

        log.Entries.Should().NotContain(e => e.Severity == "ERROR");
        log.ToText().Should().Contain("EXISTING inventory").And.Contain("creating nothing");
    }

    // The guard is about the operation, not about switching creation off: a device that is
    // new to samedis still brings its model with it.
    [Fact]
    public void A_create_still_creates_the_missing_device_model()
    {
        var api = new FakeApiWithWrites(
            get: url => url.StartsWith("device_models") ? NotFound()
               : url.StartsWith("device_types") ? Found("type-1")
               : Found("maker-1"),
            post: _ => (201, "{\"data\":{\"id\":\"new-model\"}}"));

        Resolve(Context(api, new NullSyncLog()), isCreateOperation: true).Should().Be("new-model");
        api.Posts.Should().ContainSingle().Which.Resource.Should().Be("device_models");
    }

    // An update may still ADOPT a model that already exists -- that is how a genuine model
    // change in the source still reaches samedis. Only inventing one is forbidden.
    [Fact]
    public void An_update_still_adopts_a_model_that_exists()
    {
        var api = new FakeApiWithWrites(
            get: url => url.StartsWith("device_models") ? Found("model-1") : NotFound(),
            post: _ => (201, "{\"data\":{\"id\":\"should-not-happen\"}}"));

        Resolve(Context(api, new NullSyncLog()), isCreateOperation: false).Should().Be("model-1");
        api.Posts.Should().BeEmpty();
    }

    // ------------------------------------------------- the id the source carries by itself

    // The server answers a merged-away id with the record that absorbed it. Writing what came
    // back instead of what was sent is what stops the historic id from circulating: the
    // backend would rewrite it on its way in either way, but silently, and the source would
    // never learn.
    [Fact]
    public void A_merged_catalog_id_is_replaced_by_the_survivor()
    {
        var log = new RecordingSyncLog();
        var api = new FakeApiWithWrites(
            get: url => url.Contains(Historic) ? Found(Survivor) : NotFound(),
            post: _ => throw new NotSupportedException());

        Resolve(Context(api, log), sourceCatalogId: Historic).Should().Be(Survivor);
        log.ToText().Should().Contain("was merged into");
    }

    // The scenario in the issue, spelled out: "Perfusor-Space" (12345) is merged into
    // "Perfusor Space" (45678); the external system still has 12345 on its local device type
    // and now sends a device that does not exist in samedis yet. The historic id is resolved
    // BEFORE the create, so the POST carries 45678 and there is no failed write to retry.
    [Fact]
    public void A_new_inventory_carrying_a_historic_id_is_created_with_the_current_one()
    {
        var log = new RecordingSyncLog();
        var api = new FakeApiWithWrites(
            get: url => url.Contains(Historic) ? Found(Survivor) : NotFound(),
            post: _ => throw new NotSupportedException());

        Resolve(Context(api, log), sourceCatalogId: Historic, isCreateOperation: true)
            .Should().Be(Survivor);

        log.ToText().Should().Contain(Historic).And.Contain(Survivor);
    }

    // Same for a device that is already in samedis: the update also writes the current id,
    // which is what eventually stops the historic one from coming back every run.
    [Fact]
    public void An_update_carrying_a_historic_id_writes_the_current_one()
    {
        var api = new FakeApiWithWrites(
            get: url => url.Contains(Historic) ? Found(Survivor) : NotFound(),
            post: _ => throw new NotSupportedException());

        Resolve(Context(api, new NullSyncLog()), sourceCatalogId: Historic, isCreateOperation: false)
            .Should().Be(Survivor);
    }

    [Fact]
    public void A_live_catalog_id_is_passed_through_untouched()
    {
        var log = new RecordingSyncLog();
        var api = new FakeApiWithWrites(
            get: url => url.Contains(Survivor) ? Found(Survivor) : NotFound(),
            post: _ => throw new NotSupportedException());

        Resolve(Context(api, log), sourceCatalogId: Survivor).Should().Be(Survivor);
        log.ToText().Should().NotContain("was merged into");
    }

    // The check costs one request per DISTINCT id, not per row -- otherwise it would be too
    // expensive to do at all on a facility with tens of thousands of devices.
    [Fact]
    public void The_same_catalog_id_is_only_asked_about_once()
    {
        var api = new FakeApiWithWrites(
            get: url => url.Contains(Historic) ? Found(Survivor) : NotFound(),
            post: _ => throw new NotSupportedException());
        var ctx = Context(api, new NullSyncLog());

        Resolve(ctx, sourceCatalogId: Historic).Should().Be(Survivor);
        Resolve(ctx, sourceCatalogId: Historic).Should().Be(Survivor);

        api.Gets.Count(u => u.Contains(Historic)).Should().Be(1);
    }

    // An id that resolves to nothing is NOT swapped for a title guess: it is sent as it
    // stands, so the backend's rejection stays the diagnosis. Guessing here would attach the
    // device to a different model than the source asked for.
    [Fact]
    public void An_unknown_catalog_id_is_sent_unchanged_with_a_warning()
    {
        var log = new RecordingSyncLog();
        var api = new FakeApiWithWrites(
            get: _ => NotFound(),
            post: _ => throw new NotSupportedException());

        Resolve(Context(api, log), sourceCatalogId: Historic).Should().Be(Historic);
        log.Entries.Should().Contain(e => e.Severity == "WARN");
    }

    // A malformed value in the id column is rejected without a request -- source systems put
    // placeholders and free text there.
    [Fact]
    public void A_malformed_catalog_id_costs_no_request()
    {
        var api = new FakeApiWithWrites(
            get: _ => NotFound(),
            post: _ => throw new NotSupportedException());

        Resolve(Context(api, new NullSyncLog()), sourceCatalogId: "kein-modell").Should().Be("kein-modell");
        api.Gets.Should().BeEmpty();
    }

    // ---------------------------------------------------------------- the export direction

    // Without this column the source system has no way to recognise its own stale id. It
    // costs nothing to carry: the export already fetches each device model's detail for the
    // service intervals, and merged_catalog_ids is only on the detail serializer anyway.
    [Fact]
    public void The_export_carries_the_historic_ids()
    {
        var ds = DeviceModels.CreateDeviceDataSet();
        DeviceModels.FillDeviceDataSet(ds, $@"{{""data"":[{{""attributes"":{{
            ""id"":""{Survivor}"",
            ""title"":""Perfusor Space PCA"",
            ""merged_catalog_ids"":[""{Historic}"",""507f1f77bcf86cd799439012""]
        }}}}]}}");

        var row = ds.Tables["Devices"]!.Rows[0];
        row["merged_catalog_ids"].Should().Be($"{Historic},507f1f77bcf86cd799439012");
    }

    // Comma, not the CSV's own semicolon: a key list the source system parses should not
    // need quote handling to be read back.
    [Fact]
    public void The_historic_ids_do_not_use_the_csv_delimiter()
    {
        var ds = DeviceModels.CreateDeviceDataSet();
        DeviceModels.FillDeviceDataSet(ds, $@"{{""data"":[{{""attributes"":{{
            ""id"":""{Survivor}"", ""merged_catalog_ids"":[""{Historic}"",""507f1f77bcf86cd799439012""]
        }}}}]}}");

        ds.Tables["Devices"]!.Rows[0]["merged_catalog_ids"].ToString().Should().NotContain(";");
    }

    // Every model that was never merged into -- almost all of them -- must read as an empty
    // cell rather than as a missing column.
    [Fact]
    public void A_model_that_absorbed_nothing_exports_an_empty_cell()
    {
        var ds = DeviceModels.CreateDeviceDataSet();
        DeviceModels.FillDeviceDataSet(ds, $@"{{""data"":[{{""attributes"":{{
            ""id"":""{Survivor}"", ""title"":""Perfusor Space PCA""
        }}}}]}}");

        ds.Tables["Devices"]!.Rows[0]["merged_catalog_ids"].Should().Be("");
    }
}
