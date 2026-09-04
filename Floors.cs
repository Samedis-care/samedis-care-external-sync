using System.Data;
using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Floors
  {
    public class SourceFloor
    {
      public string SourceId { get; set; } = string.Empty;
      public string SourceBuildingId { get; set; } = string.Empty;
      public string Number { get; set; } = string.Empty;
      public string Title { get; set; } = string.Empty;
    }

    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("external_id")]
      public string? ExternalId { get; set; }

      [JsonProperty("tenant_id")]
      public string? TenantId { get; set; }

      [JsonProperty("building_id")]
      public string? BuildingId { get; set; }

      [JsonProperty("title")]
      public string? Title { get; set; }

      [JsonProperty("path")]
      public string? Path { get; set; }

      [JsonProperty("notes")]
      public string? Notes { get; set; }
    }

    public class Data
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("type")]
      public string? Type { get; set; }

      [JsonProperty("attributes")]
      public Attributes? Attributes { get; set; }
    }

    public class Root
    {
      [JsonProperty("data")]
      [JsonConverter(typeof(JsonApi.SingleOrArrayConverter<Data>))]
      public List<Data>? Data { get; set; }
    }

    public static Dictionary<string, SourceFloor> LoadSourceFloors(string csvPath, ISyncLog log)
    {
      var result = new Dictionary<string, SourceFloor>(StringComparer.OrdinalIgnoreCase);
      if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
        return result;

      DataTable sourceTable;
      try
      {
        sourceTable = Csv.Read(csvPath, tableName: "SourceFloors", trimFields: true);
      }
      catch (Exception ex)
      {
        log.Warn($"Failed to read source floors CSV '{csvPath}': {ex.Message}");
        return result;
      }

      foreach (DataRow row in sourceTable.Rows)
      {
        var sourceId = Rows.Value(row, "lid");
        if (string.IsNullOrWhiteSpace(sourceId))
          sourceId = Rows.Value(row, "id");

        if (string.IsNullOrWhiteSpace(sourceId))
          continue;

        var title = Rows.Value(row, "Bezeichnung");
        if (string.IsNullOrWhiteSpace(title))
          title = Rows.Value(row, "description");
        if (string.IsNullOrWhiteSpace(title))
          title = Rows.Value(row, "descriptions");
        if (string.IsNullOrWhiteSpace(title))
          title = Rows.Value(row, "title");
        var sourceBuildingId = Rows.Value(row, "parent_id");
        if (string.IsNullOrWhiteSpace(sourceBuildingId))
          sourceBuildingId = Rows.Value(row, "Übergeordnet");

        var number = Rows.Value(row, "Number");
        if (string.IsNullOrWhiteSpace(number))
          number = Rows.Value(row, "number");

        result[sourceId] = new SourceFloor
        {
          SourceId = sourceId,
          SourceBuildingId = sourceBuildingId,
          Number = number,
          Title = title
        };
      }

      log.Debug($"Loaded source floor map entries: {result.Count}");
      return result;
    }

    /// <summary>
    /// Resolves the floor the source names, creating it below the building when asked to.
    /// </summary>
    public static string? ResolveFloorId(
      IApiClient client,
      string resource,
      string buildingId,
      string floorTitle,
      bool createOnTheFly,
      string inventoryId,
      string inventoryTitle,
      ResourceLookup lookup,
      ISyncLog log,
      string externalId = "",
      bool updateOnExisting = false)
    {
      var title = floorTitle?.Trim() ?? string.Empty;
      var external = externalId?.Trim() ?? string.Empty;

      Dictionary<string, object?> Attributes(bool clearing)
      {
        var payload = new Dictionary<string, object?>
        {
          ["title"] = title,
          ["building_id"] = buildingId,
        };
        if (!string.IsNullOrWhiteSpace(external)) payload["external_id"] = external;
        return payload;
      }

      return Hierarchy.Resolve(client, resource,
        new Hierarchy.Node("Floor", title, external,
                           new (string, string?)[] { ("building_id", buildingId) },
                           Attributes)
        {
          Context = $"inventory_id='{inventoryId}', inventory_title='{inventoryTitle}'",
        },
        lookup, log, createOnTheFly, updateOnExisting);
    }
  }
}
