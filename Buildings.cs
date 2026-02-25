using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Buildings
  {
    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

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

    public static string? ResolveBuildingId(
      RequestData client,
      string resource,
      string propertyId,
      string buildingTitle,
      bool createOnTheFly,
      string inventoryId,
      string inventoryTitle,
      IDictionary<string, string> buildingsByKey,
      IDictionary<string, string> checkedBuildings,
      Helper helper)
    {
      if (string.IsNullOrWhiteSpace(propertyId) || string.IsNullOrWhiteSpace(buildingTitle))
        return null;

      var normalizedTitle = buildingTitle.Trim();
      var key = propertyId + "|" + normalizedTitle;
      var checkedKey = "title:" + key;

      if (buildingsByKey.TryGetValue(key, out var cachedBuildingId))
      {
        if (!string.IsNullOrWhiteSpace(cachedBuildingId))
          return cachedBuildingId;

        // Negative cache hit: building was already checked and not resolvable.
        return null;
      }

      if (checkedBuildings.TryGetValue(checkedKey, out var checkedByTitle))
      {
        if (!string.IsNullOrWhiteSpace(checkedByTitle))
          return checkedByTitle;

        // Negative cache hit: building was already checked and not resolvable.
        return null;
      }

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("title", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, Uri.EscapeDataString(normalizedTitle));
      filterBuilder.Add("property_id", FilterBuilder.FilterType.Equals, FilterBuilder.Type.ObjectId, propertyId);

      var listResponse = client.Get(resource + $"?page[number]=1&page[limit]=1&gridfilter={filterBuilder.Get()}");
      if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(listResponse))
      {
        var listRoot = JsonConvert.DeserializeObject<Root>(listResponse);
        var foundBuilding = listRoot?.Data?.FirstOrDefault();
        var resolvedId = foundBuilding?.Attributes?.Id ?? foundBuilding?.Id;
        if (!string.IsNullOrWhiteSpace(resolvedId))
        {
          buildingsByKey[key] = resolvedId;
          checkedBuildings[checkedKey] = resolvedId;
          return resolvedId;
        }
      }

      checkedBuildings[checkedKey] = string.Empty;
      buildingsByKey[key] = string.Empty;

      if (!createOnTheFly)
        return null;

      var payload = JsonConvert.SerializeObject(new
      {
        data = new Dictionary<string, object?>
        {
          ["title"] = normalizedTitle,
          ["property_id"] = propertyId
        }
      });

      var response = client.Post(resource, payload);
      if (client.StatusCode < 200 || client.StatusCode >= 300)
      {
        helper.Message(
          $"Failed to create building (title='{normalizedTitle}', property_id='{propertyId}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}', status={client.StatusCode}). Response: {response}",
          1,
          "ERROR"
        );
        return null;
      }

      var newBuildingId = Helper.ExtractDataId(response);
      if (string.IsNullOrWhiteSpace(newBuildingId))
      {
        helper.Message(
          $"Failed to create building (title='{normalizedTitle}', property_id='{propertyId}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}'): API returned no building id.",
          1,
          "ERROR"
        );
        return null;
      }

      buildingsByKey[key] = newBuildingId;
      checkedBuildings[checkedKey] = newBuildingId;
      helper.Message($"Building created on the fly: '{normalizedTitle}' (property_id='{propertyId}') -> {newBuildingId}", 2);
      return newBuildingId;
    }
  }
}
