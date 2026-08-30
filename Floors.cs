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
    /// A floor is identified by its title within a building, so both conditions travel
    /// together. Declared once so the lookup and the cache seeding cannot drift apart.
    /// </summary>
    private static (string Field, string? Value)[] TitleConditions(string title, string buildingId)
      => new (string, string?)[] { ("title", title), ("building_id", buildingId) };

    public static string? ResolveFloorId(
      RequestData client,
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
      var normalizedTitle = floorTitle.Trim();
      var normalizedExternalId = externalId?.Trim() ?? string.Empty;
      var key = buildingId + "|" + normalizedTitle;
      var checkedKey = "title:" + key;
      var useScopedExternalLookup = !updateOnExisting;
      var externalScopeKey = useScopedExternalLookup
        ? (string.IsNullOrWhiteSpace(buildingId) ? string.Empty : buildingId + "|") + normalizedExternalId
        : normalizedExternalId;
      var checkedExternalKey = "external_id:" + externalScopeKey;

      Dictionary<string, object?> BuildPayload()
      {
        var payload = new Dictionary<string, object?>
        {
          ["title"] = normalizedTitle,
          ["building_id"] = buildingId
        };

        if (!string.IsNullOrWhiteSpace(normalizedExternalId))
          payload["external_id"] = normalizedExternalId;

        return payload;
      }

      void SyncExistingFloor(string resolvedId, string matchedBy)
      {
        if (!updateOnExisting || string.IsNullOrWhiteSpace(resolvedId))
          return;

        var updatePayload = JsonConvert.SerializeObject(new
        {
          data = BuildPayload()
        });
        var updateResponse = client.Put(resource, resolvedId, updatePayload);
        if (client.StatusCode >= 200 && client.StatusCode < 300)
        {
          log.Debug($"Floor synced via PUT (match_by='{matchedBy}', id='{resolvedId}', title='{normalizedTitle}', external_id='{normalizedExternalId}').");
        }
        else
        {
          log.Warn($"Failed to sync floor via PUT (match_by='{matchedBy}', id='{resolvedId}', title='{normalizedTitle}', building_id='{buildingId}', external_id='{normalizedExternalId}', status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}'). Response: {updateResponse}");
        }
      }

      // external_id is the stable cross-system anchor, so it is tried first. The lookup
      // remembers hits and misses, which is what the checkedFloors bookkeeping did here.
      var resolvedByExternalId = lookup.ByUniqueField("external_id", normalizedExternalId);
      if (!string.IsNullOrWhiteSpace(resolvedByExternalId))
      {
        lookup.RememberFields(TitleConditions(normalizedTitle, buildingId), resolvedByExternalId);
        SyncExistingFloor(resolvedByExternalId, "external_id");
        return resolvedByExternalId;
      }

      if (string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(normalizedTitle))
        return null;

      var resolvedByTitle = lookup.ByFields(TitleConditions(normalizedTitle, buildingId));
      if (!string.IsNullOrWhiteSpace(resolvedByTitle))
      {
        SyncExistingFloor(resolvedByTitle, "title");
        return resolvedByTitle;
      }

      if (!createOnTheFly)
        return null;

      var payload = JsonConvert.SerializeObject(new
      {
        data = BuildPayload()
      });

      var response = client.Post(resource, payload);
      if (client.StatusCode < 200 || client.StatusCode >= 300)
      {
        log.Error($"Failed to create floor (title='{normalizedTitle}', building_id='{buildingId}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}', status={client.StatusCode}). Response: {response}");
        return null;
      }

      var newFloorId = JsonApi.ExtractDataId(response);
      if (string.IsNullOrWhiteSpace(newFloorId))
      {
        log.Error($"Failed to create floor (title='{normalizedTitle}', building_id='{buildingId}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}'): API returned no floor id.");
        return null;
      }
      lookup.RememberFields(TitleConditions(normalizedTitle, buildingId), newFloorId);
      lookup.RememberUniqueField("external_id", normalizedExternalId, newFloorId);
      log.Debug($"Floor created on the fly: '{normalizedTitle}' (building_id='{buildingId}') -> {newFloorId}");
      return newFloorId;
    }
  }
}
