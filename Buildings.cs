using System.Data;
using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Buildings
  {
    public class SourceBuilding
    {
      public string SourceId { get; set; } = string.Empty;
      public string ParentSourceId { get; set; } = string.Empty;
      public string Number { get; set; } = string.Empty;
      public string Title { get; set; } = string.Empty;
      public string Street { get; set; } = string.Empty;
      public string Zip { get; set; } = string.Empty;
      public string Town { get; set; } = string.Empty;
    }

    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("external_id")]
      public string? ExternalId { get; set; }

      [JsonProperty("tenant_id")]
      public string? TenantId { get; set; }

      [JsonProperty("property_id")]
      public string? PropertyId { get; set; }

      [JsonProperty("title")]
      public string? Title { get; set; }

      [JsonProperty("path")]
      public string? Path { get; set; }

      [JsonProperty("notes")]
      public string? Notes { get; set; }

      [JsonProperty("street")]
      public string? Street { get; set; }

      [JsonProperty("zip")]
      public string? Zip { get; set; }

      [JsonProperty("town")]
      public string? Town { get; set; }
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

    public static Dictionary<string, SourceBuilding> LoadSourceBuildings(string csvPath, ISyncLog log)
    {
      var result = new Dictionary<string, SourceBuilding>(StringComparer.OrdinalIgnoreCase);
      if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
        return result;

      DataTable sourceTable;
      try
      {
        sourceTable = Csv.Read(csvPath, tableName: "SourceBuildings", trimFields: true);
      }
      catch (Exception ex)
      {
        log.Warn($"Failed to read source buildings CSV '{csvPath}': {ex.Message}");
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
        var parentSourceId = Rows.Value(row, "parent_id");
        if (string.IsNullOrWhiteSpace(parentSourceId))
          parentSourceId = Rows.Value(row, "Übergeordnet");

        var number = Rows.Value(row, "Number");
        if (string.IsNullOrWhiteSpace(number))
          number = Rows.Value(row, "number");
        var street = Rows.Value(row, "street");
        var zip = Rows.Value(row, "postal_code");
        if (string.IsNullOrWhiteSpace(zip))
          zip = Rows.Value(row, "zip");
        var town = Rows.Value(row, "city");
        if (string.IsNullOrWhiteSpace(town))
          town = Rows.Value(row, "town");

        result[sourceId] = new SourceBuilding
        {
          SourceId = sourceId,
          ParentSourceId = parentSourceId,
          Number = number,
          Title = title,
          Street = street,
          Zip = zip,
          Town = town
        };
      }

      log.Debug($"Loaded source building map entries: {result.Count}");
      return result;
    }

    /// <summary>
    /// Resolves the building the source names, creating it below the property when asked to.
    /// </summary>
    /// <remarks>
    /// Street, zip and town are sent on an update even when the source left them empty, so a
    /// value removed upstream is cleared here rather than left standing. On a create they
    /// stay out -- there is nothing to clear, and a new building should not be written full
    /// of empty strings.
    /// </remarks>
    public static string? ResolveBuildingId(
      IApiClient client,
      string resource,
      string propertyId,
      string buildingTitle,
      bool createOnTheFly,
      string inventoryId,
      string inventoryTitle,
      ResourceLookup lookup,
      ISyncLog log,
      string externalId = "",
      string street = "",
      string zip = "",
      string town = "",
      bool updateOnExisting = false)
    {
      var title = buildingTitle?.Trim() ?? string.Empty;
      var external = externalId?.Trim() ?? string.Empty;

      Dictionary<string, object?> Attributes(bool clearing)
      {
        var payload = new Dictionary<string, object?>
        {
          ["title"] = title,
          ["property_id"] = propertyId,
        };
        if (!string.IsNullOrWhiteSpace(external)) payload["external_id"] = external;

        foreach (var (key, value) in new[] { ("street", street), ("zip", zip), ("town", town) })
        {
          var v = value?.Trim() ?? string.Empty;
          if (clearing || !string.IsNullOrWhiteSpace(v)) payload[key] = v;
        }
        return payload;
      }

      return Hierarchy.Resolve(client, resource,
        new Hierarchy.Node("Building", title, external,
                           new (string, string?)[] { ("property_id", propertyId) },
                           Attributes)
        {
          Context = $"inventory_id='{inventoryId}', inventory_title='{inventoryTitle}'",
        },
        lookup, log, createOnTheFly, updateOnExisting);
    }
  }
}
