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
    /// A building is identified by its title within a property, so both conditions travel
    /// together. Declared once so the lookup and the cache seeding cannot drift apart -- a
    /// seed built from different conditions is a cache entry that never gets hit.
    /// </summary>
    private static (string Field, string? Value)[] TitleConditions(string title, string propertyId)
      => new (string, string?)[] { ("title", title), ("property_id", propertyId) };

    public static string? ResolveBuildingId(
      RequestData client,
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
      var normalizedTitle = buildingTitle.Trim();
      var normalizedExternalId = externalId?.Trim() ?? string.Empty;
      var normalizedStreet = street?.Trim() ?? string.Empty;
      var normalizedZip = zip?.Trim() ?? string.Empty;
      var normalizedTown = town?.Trim() ?? string.Empty;
      var key = propertyId + "|" + normalizedTitle;
      var checkedKey = "title:" + key;
      var useScopedExternalLookup = !updateOnExisting;
      var externalScopeKey = useScopedExternalLookup
        ? (string.IsNullOrWhiteSpace(propertyId) ? string.Empty : propertyId + "|") + normalizedExternalId
        : normalizedExternalId;
      var checkedExternalKey = "external_id:" + externalScopeKey;

      Dictionary<string, object?> BuildPayload(bool includeEmptyAddress)
      {
        var payload = new Dictionary<string, object?>
        {
          ["title"] = normalizedTitle,
          ["property_id"] = propertyId
        };

        if (!string.IsNullOrWhiteSpace(normalizedExternalId))
          payload["external_id"] = normalizedExternalId;

        if (includeEmptyAddress || !string.IsNullOrWhiteSpace(normalizedStreet))
          payload["street"] = normalizedStreet;
        if (includeEmptyAddress || !string.IsNullOrWhiteSpace(normalizedZip))
          payload["zip"] = normalizedZip;
        if (includeEmptyAddress || !string.IsNullOrWhiteSpace(normalizedTown))
          payload["town"] = normalizedTown;

        return payload;
      }

      void SyncExistingBuilding(string resolvedId, string matchedBy)
      {
        if (!updateOnExisting || string.IsNullOrWhiteSpace(resolvedId))
          return;

        var updatePayload = JsonConvert.SerializeObject(new
        {
          data = BuildPayload(includeEmptyAddress: true)
        });
        var updateResponse = client.Put(resource, resolvedId, updatePayload);
        if (client.StatusCode >= 200 && client.StatusCode < 300)
        {
          log.Debug($"Building synced via PUT (match_by='{matchedBy}', id='{resolvedId}', title='{normalizedTitle}', external_id='{normalizedExternalId}').");
        }
        else
        {
          log.Warn($"Failed to sync building via PUT (match_by='{matchedBy}', id='{resolvedId}', title='{normalizedTitle}', property_id='{propertyId}', external_id='{normalizedExternalId}', status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}'). Response: {updateResponse}");
        }
      }

        // external_id is the stable cross-system anchor, so it is tried first. The lookup
        // remembers hits and misses, which is what the checkedBuildings bookkeeping did here.
        var resolvedByExternalId = lookup.ByUniqueField("external_id", normalizedExternalId);
        if (!string.IsNullOrWhiteSpace(resolvedByExternalId))
        {
          // Seed the title lookup too, so a later row naming the same building by title and
          // property is answered from memory.
          lookup.RememberFields(TitleConditions(normalizedTitle, propertyId), resolvedByExternalId);
          SyncExistingBuilding(resolvedByExternalId, "external_id");
          return resolvedByExternalId;
        }

      if (string.IsNullOrWhiteSpace(propertyId) || string.IsNullOrWhiteSpace(normalizedTitle))
        return null;

      var resolvedByTitle = lookup.ByFields(TitleConditions(normalizedTitle, propertyId));
      if (!string.IsNullOrWhiteSpace(resolvedByTitle))
      {
        SyncExistingBuilding(resolvedByTitle, "title");
        return resolvedByTitle;
      }

      if (!createOnTheFly)
        return null;

      var payload = JsonConvert.SerializeObject(new
      {
        data = BuildPayload(includeEmptyAddress: false)
      });

      var response = client.Post(resource, payload);
      if (client.StatusCode < 200 || client.StatusCode >= 300)
      {
        log.Error($"Failed to create building (title='{normalizedTitle}', property_id='{propertyId}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}', status={client.StatusCode}). Response: {response}");
        return null;
      }

      var newBuildingId = JsonApi.ExtractDataId(response);
      if (string.IsNullOrWhiteSpace(newBuildingId))
      {
        log.Error($"Failed to create building (title='{normalizedTitle}', property_id='{propertyId}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}'): API returned no building id.");
        return null;
      }

      lookup.RememberFields(TitleConditions(normalizedTitle, propertyId), newBuildingId);
      lookup.RememberUniqueField("external_id", normalizedExternalId, newBuildingId);
      log.Debug($"Building created on the fly: '{normalizedTitle}' (property_id='{propertyId}') -> {newBuildingId}");
      return newBuildingId;
    }
  }
}
