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
      [JsonConverter(typeof(Helper.SingleOrArrayConverter<Data>))]
      public List<Data>? Data { get; set; }
    }

    public static Dictionary<string, SourceFloor> LoadSourceFloors(string csvPath, Helper helper)
    {
      var result = new Dictionary<string, SourceFloor>(StringComparer.OrdinalIgnoreCase);
      if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
        return result;

      DataTable sourceTable;
      try
      {
        sourceTable = Helper.ImportCsvToDataTable(csvPath, "SourceFloors");
      }
      catch (Exception ex)
      {
        helper.Message($"Failed to read source floors CSV '{csvPath}': {ex.Message}", 1, "WARN");
        return result;
      }

      foreach (DataRow row in sourceTable.Rows)
      {
        var sourceId = Helper.GetRowValue(row, "lid");
        if (string.IsNullOrWhiteSpace(sourceId))
          sourceId = Helper.GetRowValue(row, "id");

        if (string.IsNullOrWhiteSpace(sourceId))
          continue;

        var title = Helper.GetRowValue(row, "Bezeichnung");
        if (string.IsNullOrWhiteSpace(title))
          title = Helper.GetRowValue(row, "description");
        if (string.IsNullOrWhiteSpace(title))
          title = Helper.GetRowValue(row, "descriptions");
        if (string.IsNullOrWhiteSpace(title))
          title = Helper.GetRowValue(row, "title");
        var sourceBuildingId = Helper.GetRowValue(row, "parent_id");
        if (string.IsNullOrWhiteSpace(sourceBuildingId))
          sourceBuildingId = Helper.GetRowValue(row, "Übergeordnet");

        var number = Helper.GetRowValue(row, "Number");
        if (string.IsNullOrWhiteSpace(number))
          number = Helper.GetRowValue(row, "number");

        result[sourceId] = new SourceFloor
        {
          SourceId = sourceId,
          SourceBuildingId = sourceBuildingId,
          Number = number,
          Title = title
        };
      }

      helper.Message($"Loaded source floor map entries: {result.Count}", 2);
      return result;
    }

    public static string? ResolveFloorId(
      RequestData client,
      string resource,
      string buildingId,
      string floorTitle,
      bool createOnTheFly,
      string inventoryId,
      string inventoryTitle,
      IDictionary<string, string> floorsByKey,
      IDictionary<string, string> checkedFloors,
      Helper helper)
    {
      if (string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(floorTitle))
        return null;

      var normalizedTitle = floorTitle.Trim();
      var key = buildingId + "|" + normalizedTitle;
      var checkedKey = "title:" + key;

      if (floorsByKey.TryGetValue(key, out var cachedFloorId))
      {
        if (!string.IsNullOrWhiteSpace(cachedFloorId))
          return cachedFloorId;

        // Negative cache hit: floor was already checked and not resolvable.
        return null;
      }

      if (checkedFloors.TryGetValue(checkedKey, out var checkedByTitle))
      {
        if (!string.IsNullOrWhiteSpace(checkedByTitle))
          return checkedByTitle;

        // Negative cache hit: floor was already checked and not resolvable.
        return null;
      }

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("title", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, Uri.EscapeDataString(normalizedTitle));
      filterBuilder.Add("building_id", FilterBuilder.FilterType.Equals, FilterBuilder.Type.ObjectId, buildingId);

      var listResponse = client.Get(resource + $"?page[number]=1&page[limit]=1&gridfilter={filterBuilder.Get()}");
      if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(listResponse))
      {
        var listRoot = JsonConvert.DeserializeObject<Root>(listResponse);
        var foundFloor = listRoot?.Data?.FirstOrDefault();
        var resolvedId = foundFloor?.Attributes?.Id ?? foundFloor?.Id;
        if (!string.IsNullOrWhiteSpace(resolvedId))
        {
          floorsByKey[key] = resolvedId;
          checkedFloors[checkedKey] = resolvedId;
          return resolvedId;
        }
      }

      checkedFloors[checkedKey] = string.Empty;
      floorsByKey[key] = string.Empty;

      if (!createOnTheFly)
        return null;

      var payload = JsonConvert.SerializeObject(new
      {
        data = new Dictionary<string, object?>
        {
          ["title"] = normalizedTitle,
          ["building_id"] = buildingId
        }
      });

      var response = client.Post(resource, payload);
      if (client.StatusCode < 200 || client.StatusCode >= 300)
      {
        helper.Message(
          $"Failed to create floor (title='{normalizedTitle}', building_id='{buildingId}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}', status={client.StatusCode}). Response: {response}",
          1,
          "ERROR"
        );
        return null;
      }

      var newFloorId = Helper.ExtractDataId(response);
      if (string.IsNullOrWhiteSpace(newFloorId))
      {
        helper.Message(
          $"Failed to create floor (title='{normalizedTitle}', building_id='{buildingId}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}'): API returned no floor id.",
          1,
          "ERROR"
        );
        return null;
      }

      floorsByKey[key] = newFloorId;
      checkedFloors[checkedKey] = newFloorId;
      helper.Message($"Floor created on the fly: '{normalizedTitle}' (building_id='{buildingId}') -> {newFloorId}", 2);
      return newFloorId;
    }
  }
}
