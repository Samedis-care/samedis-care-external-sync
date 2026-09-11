using FluentAssertions;
using SamedisCare.Api.Lookup;
using SamedisCare.Helper.Logging;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>A throwaway directory tree, removed with the test.</summary>
internal sealed class TempDir : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "sc-arch-" + Guid.NewGuid().ToString("N"));

    public TempDir() => Directory.CreateDirectory(Path);

    public string Upload => System.IO.Path.Combine(Path, "upload");

    public string Archive => System.IO.Path.Combine(Path, "archive", "upload");

    public TempDir WithUpload(params string[] fileNames)
    {
        Directory.CreateDirectory(Upload);
        foreach (var name in fileNames)
            File.WriteAllText(System.IO.Path.Combine(Upload, name), "a;b\r\n1;2\r\n");
        return this;
    }

    public void Dispose()
    {
        if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
    }
}

/// <summary>
/// Archiving moves the processed CSVs out of the upload folder. Getting it wrong either loses
/// a file or leaves one behind to be imported a second time.
/// </summary>
public class ArchiveUploadTests
{
    private static readonly ISyncLog Silent = new NullSyncLog();

    [Fact]
    public void Processed_files_are_moved_into_a_sibling_archive_folder()
    {
        using var dir = new TempDir().WithUpload("inventories.csv");

        Helper.ArchiveUploadCsvFiles(Silent, dir.Upload, inventoriesUploadEnabled: true);

        Directory.GetFiles(dir.Upload, "*.csv").Should().BeEmpty();
        Directory.GetFiles(dir.Archive, "*.csv").Should().ContainSingle()
                 .Which.Should().Contain("inventories_");
    }

    [Fact]
    public void The_archived_name_keeps_the_original_stem_and_extension()
    {
        using var dir = new TempDir().WithUpload("inventories.csv");

        Helper.ArchiveUploadCsvFiles(Silent, dir.Upload, true);

        var archived = Path.GetFileName(Directory.GetFiles(dir.Archive).Single());
        archived.Should().StartWith("inventories_").And.EndWith(".csv");
    }

    [Fact]
    public void Every_csv_in_the_folder_is_taken()
    {
        using var dir = new TempDir().WithUpload("a.csv", "b.csv", "c.csv");

        Helper.ArchiveUploadCsvFiles(Silent, dir.Upload, true);

        Directory.GetFiles(dir.Archive).Should().HaveCount(3);
    }

    [Fact]
    public void Files_that_are_not_csv_are_left_alone()
    {
        using var dir = new TempDir().WithUpload("a.csv");
        File.WriteAllText(Path.Combine(dir.Upload, "notes.txt"), "x");

        Helper.ArchiveUploadCsvFiles(Silent, dir.Upload, true);

        File.Exists(Path.Combine(dir.Upload, "notes.txt")).Should().BeTrue();
    }

    // Off means off: a run with the upload disabled must not move the operator's files.
    [Fact]
    public void Nothing_moves_when_the_inventory_upload_is_disabled()
    {
        using var dir = new TempDir().WithUpload("a.csv");

        Helper.ArchiveUploadCsvFiles(Silent, dir.Upload, inventoriesUploadEnabled: false);

        Directory.GetFiles(dir.Upload, "*.csv").Should().ContainSingle();
    }

    [Fact]
    public void A_missing_upload_folder_is_not_an_error()
    {
        using var dir = new TempDir();

        var act = () => Helper.ArchiveUploadCsvFiles(Silent, Path.Combine(dir.Path, "nope"), true);

        act.Should().NotThrow();
    }

    [Fact]
    public void An_empty_upload_folder_is_not_an_error()
    {
        using var dir = new TempDir();
        Directory.CreateDirectory(dir.Upload);

        ((Action)(() => Helper.ArchiveUploadCsvFiles(Silent, dir.Upload, true))).Should().NotThrow();
    }

    // Two runs in the same second would otherwise collide on the timestamped name and the
    // second file would be lost.
    [Fact]
    public void A_name_collision_does_not_overwrite_the_earlier_archive()
    {
        using var dir = new TempDir().WithUpload("a.csv");
        Helper.ArchiveUploadCsvFiles(Silent, dir.Upload, true);

        dir.WithUpload("a.csv");
        Helper.ArchiveUploadCsvFiles(Silent, dir.Upload, true);

        Directory.GetFiles(dir.Archive).Should().HaveCount(2);
    }
}

/// <summary>
/// Device models are resolved by title and manufacturer, against the tenant's own models and
/// the public catalog.
/// </summary>
public class CatalogResolutionTests
{
    private static ResourceLookup Lookup(FakeApi api) => new(api, "device_models");

    [Fact]
    public void The_type_plate_manufacturer_is_tried_first()
    {
        var api = FakeApi.Answering(("manufacturer_according_to_type_plate", "by-type-plate"));

        DeviceModels.ResolveCatalogId(Lookup(api), "Seca 954", "seca").Should().Be("by-type-plate");
        api.Requests.Should().ContainSingle();
    }

    // Source systems use the two manufacturer fields interchangeably.
    [Fact]
    public void The_responsible_manufacturer_is_tried_second()
    {
        var api = FakeApi.Answering(("current_responsible_manufacturer", "by-responsible"));

        DeviceModels.ResolveCatalogId(Lookup(api), "Seca 954", "seca").Should().Be("by-responsible");

        var urls = api.Requests.Select(Uri.UnescapeDataString).ToList();
        urls.Should().HaveCount(2);
        urls[0].Should().Contain("manufacturer_according_to_type_plate");
        urls[1].Should().Contain("current_responsible_manufacturer");
    }

    [Fact]
    public void Without_a_manufacturer_the_title_alone_is_used()
    {
        var api = FakeApi.NotFound();

        DeviceModels.ResolveCatalogId(Lookup(api), "Seca 954", "");

        api.Requests.Should().ContainSingle();
        Uri.UnescapeDataString(api.Requests.Single()).Should().NotContain("manufacturer");
    }

    // Catalogs are largely public master data, so a lookup limited to the tenant's own models
    // would miss almost everything.
    [Fact]
    public void The_public_catalog_is_searched_as_well_as_the_tenants_models()
    {
        var api = FakeApi.NotFound();

        DeviceModels.ResolveCatalogId(Lookup(api), "Seca 954", "seca");

        api.Requests.Should().OnlyContain(r => r.Contains("filter[scope]=public_and_tenant"));
    }

    // Preserved from before the migration. Flipping the cascade's flag would also match
    // "seca 954" against a catalog entry "Seca 954"; that is a decision about the data.
    [Fact]
    public void The_title_is_compared_case_sensitively()
    {
        var api = FakeApi.NotFound();

        DeviceModels.ResolveCatalogId(Lookup(api), "seca 954", "");

        Uri.UnescapeDataString(api.Requests.Single()).Should().Contain("\"type\":\"equals\"");
    }

    [Fact]
    public void A_blank_title_resolves_nothing_without_asking()
    {
        var api = FakeApi.NotFound();

        DeviceModels.ResolveCatalogId(Lookup(api), "   ", "seca").Should().BeNull();
        api.Requests.Should().BeEmpty();
    }

    [Fact]
    public void A_repeated_lookup_is_answered_from_memory()
    {
        var api = FakeApi.Answering(("manufacturer_according_to_type_plate", "id-1"));
        var lookup = Lookup(api);

        for (var i = 0; i < 4; i++)
            DeviceModels.ResolveCatalogId(lookup, "Seca 954", "seca").Should().Be("id-1");

        api.Requests.Should().ContainSingle();
    }
}
