using System.Data;
using FluentAssertions;
using SamedisCare.Helper.Text;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// The stand-in export under tests/fixtures/to_samedis, read through the same path the import
/// uses. These tests are about the fixtures themselves: that they stay readable, stay
/// self-consistent, and keep the awkward rows that make the edge paths reachable. A fixture
/// that quietly drifts is worse than none, because every test built on it still passes.
/// </summary>
public class FixtureTests
{
    private static DataTable Read(string name)
        => Csv.Read(Path.Combine(AppContext.BaseDirectory, "fixtures", "to_samedis", name),
                    tableName: name, trimFields: true);

    [Theory]
    [InlineData("buildings.csv", 3)]
    [InlineData("floors.csv", 15)]
    [InlineData("rooms.csv", 45)]
    [InlineData("departments.csv", 8)]
    [InlineData("inventories.csv", 50)]
    [InlineData("tasks.csv", 20)]
    [InlineData("requests.csv", 5)]
    [InlineData("request-messages.csv", 5)]
    public void Each_file_has_the_expected_number_of_rows(string name, int expected)
        => Read(name).Rows.Count.Should().Be(expected);

    // The import reads these by name, so a renamed or reordered column breaks it.
    [Fact]
    public void The_location_files_carry_the_columns_the_import_reads()
    {
        Csv.HasColumns(Read("buildings.csv"),
                       new[] { "id", "parent_id", "number", "description", "location_type",
                               "street", "postal_code", "city" }).Should().BeTrue();
        Csv.HasColumns(Read("floors.csv"),
                       new[] { "id", "parent_id", "number", "description", "location_type" })
           .Should().BeTrue();
        Csv.HasColumns(Read("rooms.csv"),
                       new[] { "id", "parent_id", "number", "description", "location_type" })
           .Should().BeTrue();
    }

    [Fact]
    public void The_inventory_file_carries_the_columns_the_import_reads()
        => Csv.HasColumns(Read("inventories.csv"),
                          new[] { "external_id", "inventory_number", "serial_number", "catalog_id",
                                  "device_model_title", "device_type_title", "manufacturer",
                                  "purchase_price", "ownership", "operation_status",
                                  "cost_center_number", "source_location_type",
                                  "source_location_id", "commissioning_at" })
              .Should().BeTrue();

    [Fact]
    public void The_task_file_carries_the_columns_the_import_reads()
        => Csv.HasColumns(Read("tasks.csv"),
                          new[] { "issue_number", "inventory_device_number", "issue_type", "title",
                                  "date", "status", "done_at", "responsible_name",
                                  "maintenance_passed" })
              .Should().BeTrue();

    // The hierarchy has to close: every floor under a building, every room under a floor.
    [Fact]
    public void The_location_hierarchy_is_closed()
    {
        var buildings = Read("buildings.csv").AsEnumerable().Select(r => r["id"].ToString()).ToHashSet();
        var floors = Read("floors.csv");
        var rooms = Read("rooms.csv");

        floors.AsEnumerable().Select(r => r["parent_id"].ToString())
              .Should().OnlyContain(p => buildings.Contains(p));

        var floorIds = floors.AsEnumerable().Select(r => r["id"].ToString()).ToHashSet();
        rooms.AsEnumerable().Select(r => r["parent_id"].ToString())
             .Should().OnlyContain(p => floorIds.Contains(p));
    }

    // Every task must belong to an inventory in the same export, which the live files do not
    // manage -- only 24 of 440 live tasks match. Fixtures that share that flaw would test the
    // failure path and nothing else.
    [Fact]
    public void Every_task_belongs_to_an_inventory_in_the_same_export()
    {
        var numbers = Read("inventories.csv").AsEnumerable()
                          .Select(r => r["inventory_number"].ToString()).ToHashSet();

        Read("tasks.csv").AsEnumerable().Select(r => r["inventory_device_number"].ToString())
            .Should().OnlyContain(n => numbers.Contains(n));
    }

    [Fact]
    public void Inventory_numbers_and_external_ids_are_unique()
    {
        var rows = Read("inventories.csv").AsEnumerable().ToList();

        rows.Select(r => r["inventory_number"].ToString()).Should().OnlyHaveUniqueItems();
        rows.Select(r => r["external_id"].ToString()).Should().OnlyHaveUniqueItems();
    }

    // Both are always empty in the live export, which is what forces the resolution to run
    // through external_id / inventory number and through the title lookup.
    [Fact]
    public void The_id_and_catalog_id_columns_are_empty_as_they_are_live()
    {
        var rows = Read("inventories.csv").AsEnumerable().ToList();

        rows.Should().OnlyContain(r => string.IsNullOrEmpty(r["id"].ToString()));
        rows.Should().OnlyContain(r => string.IsNullOrEmpty(r["catalog_id"].ToString()));
    }

    // German conventions, as the source system writes them.
    [Fact]
    public void Dates_and_prices_keep_the_source_conventions()
    {
        var row = Read("inventories.csv").Rows[0];

        row["commissioning_at"].ToString().Should().MatchRegex(@"^\d{2}\.\d{2}\.\d{4} \d{2}:\d{2}:\d{2}$");
        row["purchase_price"].ToString().Should().MatchRegex(@"^\d+,\d{6}$");
        Read("tasks.csv").Rows[0]["date"].ToString().Should().MatchRegex(@"^\d{2}\.\d{2}\.\d{4}$");
    }

    // The awkward rows exist on purpose. If they ever vanish the edge paths stop being tested
    // without a single test turning red, so they are asserted here.
    [Fact]
    public void The_deliberately_awkward_rows_are_still_there()
    {
        var raw = Csv.Read(Path.Combine(AppContext.BaseDirectory, "fixtures", "to_samedis",
                                        "inventories.csv"), trimFields: false);
        var rows = raw.AsEnumerable().ToList();

        rows.Should().Contain(r => r["serial_number"].ToString()!.StartsWith(" "),
                              "a padded value must survive into the fixtures");
        rows.Should().Contain(r => r["description"].ToString() == "NULL",
                              "a SQL export writes an absent value as the text NULL");
        rows.Should().Contain(r => r["purchase_price"].ToString()!.Contains('.'),
                              "one price is written in the other convention");
        rows.Should().Contain(r => string.IsNullOrEmpty(r["commissioning_at"].ToString()),
                              "one row has no commissioning date");
    }

    // Same model title under two manufacturers: the catalog lookup must not conflate them.
    [Fact]
    public void One_model_title_appears_under_two_manufacturers()
    {
        var byTitle = Read("inventories.csv").AsEnumerable()
            .GroupBy(r => r["device_model_title"].ToString())
            .Where(g => g.Select(r => r["manufacturer"].ToString()).Distinct().Count() > 1)
            .ToList();

        byTitle.Should().NotBeEmpty();
    }

    [Fact]
    public void The_department_file_carries_the_columns_the_import_reads()
        => Csv.HasColumns(Read("departments.csv"),
                          new[] { "id", "cost_center_number", "department",
                                  "cost_center_description", "abteilung", "notes",
                                  "profit_center", "wirtschaftende_einheit" })
              .Should().BeTrue();

    // The import takes the title from `department`, then `cost_center_description`, then
    // `abteilung`, and the profit centre from `profit_center` then `wirtschaftende_einheit`.
    // All of those fallbacks are exercised, or a renamed column would go unnoticed.
    [Fact]
    public void Every_department_title_and_profit_centre_column_is_exercised()
    {
        var rows = Read("departments.csv").AsEnumerable().ToList();

        rows.Should().Contain(r => !string.IsNullOrEmpty(r["department"].ToString()));
        rows.Should().Contain(r => !string.IsNullOrEmpty(r["cost_center_description"].ToString()));
        rows.Should().Contain(r => !string.IsNullOrEmpty(r["abteilung"].ToString()));
        rows.Should().Contain(r => !string.IsNullOrEmpty(r["profit_center"].ToString()));
        rows.Should().Contain(r => !string.IsNullOrEmpty(r["wirtschaftende_einheit"].ToString()));
    }

    [Fact]
    public void Every_department_resolves_to_exactly_one_title_and_one_profit_centre()
    {
        foreach (var row in Read("departments.csv").AsEnumerable())
        {
            var titles = new[] { "department", "cost_center_description", "abteilung" }
                .Count(c => !string.IsNullOrEmpty(row[c].ToString()));
            var centres = new[] { "profit_center", "wirtschaftende_einheit" }
                .Count(c => !string.IsNullOrEmpty(row[c].ToString()));

            titles.Should().Be(1, "a row must not name its title twice");
            centres.Should().Be(1, "a row must not name its profit centre twice");
        }
    }

    [Fact]
    public void Cost_centre_numbers_are_unique()
        => Read("departments.csv").AsEnumerable().Select(r => r["cost_center_number"].ToString())
               .Should().OnlyHaveUniqueItems();

    // An inventory names a department that has to exist, or the row is imported without one.
    [Fact]
    public void Every_inventory_belongs_to_a_department_in_the_same_export()
    {
        var known = Read("departments.csv").AsEnumerable()
            .Select(r => new[] { "department", "cost_center_description", "abteilung" }
                         .Select(c => r[c].ToString() ?? string.Empty)
                         .First(v => !string.IsNullOrEmpty(v)))
            .ToHashSet();

        Read("inventories.csv").AsEnumerable().Select(r => r["department"].ToString())
            .Should().OnlyContain(d => known.Contains(d));
    }

    [Fact]
    public void Every_inventory_cost_centre_exists_in_the_department_file()
    {
        var known = Read("departments.csv").AsEnumerable()
            .Select(r => r["cost_center_number"].ToString()).ToHashSet();

        Read("inventories.csv").AsEnumerable().Select(r => r["cost_center_number"].ToString())
            .Should().OnlyContain(c => known.Contains(c));
    }

    [Fact]
    public void Every_inventory_profit_centre_matches_its_departments()
    {
        static string FirstNonEmpty(DataRow row, params string[] columns)
            => columns.Select(c => row[c].ToString() ?? string.Empty)
                      .First(v => !string.IsNullOrEmpty(v));

        var byDepartment = Read("departments.csv").AsEnumerable().ToDictionary(
            r => FirstNonEmpty(r, "department", "cost_center_description", "abteilung"),
            r => FirstNonEmpty(r, "profit_center", "wirtschaftende_einheit"));

        Read("inventories.csv").AsEnumerable().Should().OnlyContain(
            r => byDepartment[r["department"].ToString()!] == r["profit_center"].ToString());
    }

    // The import branches on source_location_type and resolves a floor or a building to a
    // placeholder room. Those branches are only covered if all three types appear.
    [Fact]
    public void All_three_location_reference_types_appear()
    {
        var types = Read("inventories.csv").AsEnumerable()
            .Select(r => r["source_location_type"].ToString()).Distinct().ToList();

        types.Should().BeEquivalentTo(new[] { "Raum", "Ebene", "Gebäude" });
    }

    [Fact]
    public void Every_location_reference_points_at_something_that_exists()
    {
        var rooms = Read("rooms.csv").AsEnumerable().Select(r => r["id"].ToString()).ToHashSet();
        var floors = Read("floors.csv").AsEnumerable().Select(r => r["id"].ToString()).ToHashSet();
        var buildings = Read("buildings.csv").AsEnumerable().Select(r => r["id"].ToString()).ToHashSet();

        foreach (var row in Read("inventories.csv").AsEnumerable())
        {
            var id = row["source_location_id"].ToString()!;
            var known = row["source_location_type"].ToString() switch
            {
                "Raum" => rooms,
                "Ebene" => floors,
                _ => buildings,
            };
            known.Should().Contain(id, $"{row["inventory_number"]} references a {row["source_location_type"]}");
        }
    }

    // A room that is never referenced is a location assignment nobody tests, so the rows are
    // spread rather than cycled through a stride.
    [Fact]
    public void The_inventories_are_spread_across_the_locations()
    {
        var rows = Read("inventories.csv").AsEnumerable().ToList();

        Referenced(rows, "Raum").Should().BeGreaterThanOrEqualTo(40, "of 45 rooms");
        Referenced(rows, "Ebene").Should().Be(5);
        Referenced(rows, "Gebäude").Should().Be(3);

        rows.Select(r => r["department"].ToString()).Distinct().Should().HaveCount(8);
    }

    private static int Referenced(IReadOnlyCollection<DataRow> rows, string type)
        => rows.Where(r => r["source_location_type"].ToString() == type)
               .Select(r => r["source_location_id"].ToString()).Distinct().Count();

    // No two inventories share a room, so a wrong room assignment is visible at a glance
    // instead of hiding behind a plausible-looking cluster.
    [Fact]
    public void No_room_holds_more_than_one_inventory()
        => Read("inventories.csv").AsEnumerable()
               .Where(r => r["source_location_type"].ToString() == "Raum")
               .Select(r => r["source_location_id"].ToString())
               .Should().OnlyHaveUniqueItems();

    // The task upload exists to attach a protocol and skips any row without one, so a task
    // with no file is not a test case but a no-op. The whole first run was skipped this way.
    [Fact]
    public void Every_task_names_a_protocol_that_exists()
    {
        var documents = Path.Combine(AppContext.BaseDirectory, "fixtures", "to_samedis",
                                     "task_documents");

        foreach (var row in Read("tasks.csv").AsEnumerable())
        {
            var name = row["filename"].ToString();
            name.Should().NotBeNullOrEmpty($"task {row["issue_number"]} would be skipped");
            File.Exists(Path.Combine(documents, name!)).Should().BeTrue($"{name} must exist");
        }
    }

    // The server rejects a device_retired issue whose finished date lies ahead: "The finished
    // date is invalid. The date must not be in the future."
    [Fact]
    public void No_retirement_date_lies_in_the_future()
        => Read("inventories.csv").AsEnumerable()
               .Select(r => r["retirement_date"].ToString())
               .Where(d => !string.IsNullOrEmpty(d))
               .Should().OnlyContain(d => string.CompareOrdinal(d!.Substring(6, 4), "2026") < 0);

    [Fact]
    public void Only_retired_inventories_carry_a_retirement_date()
        => Read("inventories.csv").AsEnumerable()
               .Where(r => !string.IsNullOrEmpty(r["retirement_date"].ToString()))
               .Should().OnlyContain(r => r["operation_status"].ToString() == "Ausgemustert");

    [Fact]
    public void The_request_files_carry_the_columns_the_upload_requires()
    {
        // Requests.UploadRequiredColumns / MessageUploadRequiredColumns -- the upload refuses
        // the whole file when one is missing.
        Csv.HasColumns(Read("requests.csv"), new[] { "id", "incident_number" }).Should().BeTrue();
        Csv.HasColumns(Read("request-messages.csv"),
                       new[] { "id", "incident_id", "incident_number", "content" }).Should().BeTrue();
    }

    // Neither file creates a request -- one PUTs onto an existing one, the other POSTs a
    // message onto it. An id left empty is what makes the row resolve by incident_number.
    [Fact]
    public void Request_rows_are_matched_by_number_not_by_id()
    {
        Read("requests.csv").AsEnumerable()
            .Should().OnlyContain(r => string.IsNullOrEmpty(r["id"].ToString()));

        Read("request-messages.csv").AsEnumerable()
            .Should().OnlyContain(r => string.IsNullOrEmpty(r["id"].ToString())
                                    && string.IsNullOrEmpty(r["incident_id"].ToString()));
    }

    [Fact]
    public void Request_rows_only_use_statuses_the_server_accepts()
        => Read("requests.csv").AsEnumerable().Select(r => r["status"].ToString())
               .Should().BeSubsetOf(new[] { "new", "pending", "in_progress", "done" });

    [Fact]
    public void Requests_point_at_inventories_from_the_same_export()
    {
        var numbers = Read("inventories.csv").AsEnumerable()
                          .Select(r => r["inventory_number"].ToString()).ToHashSet();

        Read("requests.csv").AsEnumerable().Select(r => r["inventory_number"].ToString())
            .Should().OnlyContain(n => numbers.Contains(n));
    }

    [Fact]
    public void Message_attachments_exist_where_they_are_named()
    {
        var documents = Path.Combine(AppContext.BaseDirectory, "fixtures", "to_samedis",
                                     "task_documents");

        Read("request-messages.csv").AsEnumerable()
            .Select(r => r["filename"].ToString())
            .Where(n => !string.IsNullOrEmpty(n))
            .Should().OnlyContain(n => File.Exists(Path.Combine(documents, n!)));
    }

    // The server refuses an upload below 1 KB: "The file size is smaller than the minimum
    // size of 1 KB. Please check if the file is broken." Nineteen of twenty uploads failed
    // that way on the second run.
    [Fact]
    public void Every_protocol_is_large_enough_for_the_server_to_accept()
    {
        var documents = Path.Combine(AppContext.BaseDirectory, "fixtures", "to_samedis",
                                     "task_documents");

        Directory.GetFiles(documents, "*.pdf").Should().NotBeEmpty()
            .And.OnlyContain(f => new FileInfo(f).Length > 1024);
    }

    // "The inventory is retired. Its operation status may only be changed via a recommission
    // task." A maintenance task on a retired device is refused, so the tasks are drawn from
    // the inventories still in service.
    [Fact]
    public void No_task_targets_a_retired_inventory()
    {
        var status = Read("inventories.csv").AsEnumerable()
            .ToDictionary(r => r["inventory_number"].ToString()!,
                          r => r["operation_status"].ToString());

        Read("tasks.csv").AsEnumerable()
            .Should().OnlyContain(r => status[r["inventory_device_number"].ToString()!] != "Ausgemustert");
    }

    // Live names must never end up in the repository.
    [Fact]
    public void The_fixtures_carry_no_live_names()
    {
        var text = string.Join("\n", Directory.GetFiles(
            Path.Combine(AppContext.BaseDirectory, "fixtures", "to_samedis"), "*.csv")
            .Select(File.ReadAllText));

        text.Should().Contain("Musterstadt").And.Contain("Haus Ahorn");
        text.Should().NotContain("Psychiatrie", "that is a description from the live export");
    }
}

/// <summary>
/// The inventory fixture carries one deliberate collision: the same model name sold by a
/// second manufacturer, which must resolve to its own tenant catalog entry rather than being
/// folded into the first. That case only tests what it claims to as long as the manufacturer
/// is the ONLY thing that differs -- a device type that drifts along with it turns one
/// question into two and quietly files a patient monitor under ventilators.
/// </summary>
public class ModelDefinitionTests
{
    private static List<Dictionary<string, string>> Inventories()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "to_samedis", "inventories.csv");
        var lines = File.ReadAllLines(path);
        var headers = Split(lines[0]);

        return lines.Skip(1).Where(l => l.Trim().Length > 0)
                    .Select(l => headers.Zip(Split(l)).ToDictionary(p => p.First, p => p.Second))
                    .ToList();
    }

    private static string[] Split(string line)
        => line.TrimStart('﻿').Split(';').Select(f => f.Trim().Trim('"')).ToArray();

    [Fact]
    public void A_model_name_never_carries_two_device_types()
        => Inventories()
               .GroupBy(r => r["device_model_title"])
               .Where(g => g.Select(r => r["device_type_title"]).Distinct().Count() > 1)
               .Should().BeEmpty("a title that means two different kinds of device would make "
                               + "the catalog entry the import creates depend on row order");

    [Fact]
    public void One_model_name_is_shared_by_two_manufacturers()
    {
        var shared = Inventories()
            .GroupBy(r => r["device_model_title"])
            .Where(g => g.Select(r => r["manufacturer"]).Distinct().Count() > 1)
            .ToList();

        shared.Should().ContainSingle("the collision is deliberate and one case is enough");
        shared[0].Select(r => r["manufacturer"]).Distinct().Should().HaveCount(2);
    }

    // What the two rules above add up to, and what the import has to produce.
    [Fact]
    public void The_fixture_describes_nine_distinct_catalog_entries()
        => Inventories()
               .Select(r => (r["device_model_title"], r["manufacturer"]))
               .Distinct()
               .Should().HaveCount(9, "eight models, one of them under a second manufacturer");
}
