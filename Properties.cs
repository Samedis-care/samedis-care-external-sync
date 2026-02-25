using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Properties
  {
    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("tenant_id")]
      public string? TenantId { get; set; }

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

    public static string? ResolvePropertyId(
      RequestData client,
      string resource,
      string propertyTitle,
      bool createOnTheFly,
      IDictionary<string, string> propertiesByTitle,
      IDictionary<string, string> checkedProperties,
      Helper helper)
    {
      if (string.IsNullOrWhiteSpace(propertyTitle))
        return null;

      var normalizedTitle = propertyTitle.Trim();
      var checkedByTitleKey = "title:" + normalizedTitle;

      if (propertiesByTitle.TryGetValue(normalizedTitle, out var cachedPropertyId))
      {
        if (!string.IsNullOrWhiteSpace(cachedPropertyId))
          return cachedPropertyId;

        // Negative cache hit: property was already checked and not resolvable.
        return null;
      }

      if (checkedProperties.TryGetValue(checkedByTitleKey, out var checkedByTitle))
      {
        if (!string.IsNullOrWhiteSpace(checkedByTitle))
          return checkedByTitle;

        // Negative cache hit: property was already checked and not resolvable.
        return null;
      }

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("title", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, Uri.EscapeDataString(normalizedTitle));

      var listResponse = client.Get(resource + $"?page[number]=1&page[limit]=1&gridfilter={filterBuilder.Get()}");
      if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(listResponse))
      {
        var listRoot = JsonConvert.DeserializeObject<Root>(listResponse);
        var foundProperty = listRoot?.Data?.FirstOrDefault();
        var resolvedId = foundProperty?.Attributes?.Id ?? foundProperty?.Id;
        if (!string.IsNullOrWhiteSpace(resolvedId))
        {
          propertiesByTitle[normalizedTitle] = resolvedId;
          checkedProperties[checkedByTitleKey] = resolvedId;
          return resolvedId;
        }
      }

      // If no property with tenant-name title exists, use the first existing property if available.
      var firstResponse = client.Get(resource + "?page[number]=1&page[limit]=1");
      if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(firstResponse))
      {
        var firstRoot = JsonConvert.DeserializeObject<Root>(firstResponse);
        var firstProperty = firstRoot?.Data?.FirstOrDefault();
        var firstPropertyId = firstProperty?.Attributes?.Id ?? firstProperty?.Id;
        if (!string.IsNullOrWhiteSpace(firstPropertyId))
        {
          propertiesByTitle[normalizedTitle] = firstPropertyId;
          checkedProperties[checkedByTitleKey] = firstPropertyId;
          helper.Message($"Using existing property '{firstProperty?.Attributes?.Title ?? normalizedTitle}' -> {firstPropertyId}", 2);
          return firstPropertyId;
        }
      }

      checkedProperties[checkedByTitleKey] = string.Empty;
      propertiesByTitle[normalizedTitle] = string.Empty;

      if (!createOnTheFly)
        return null;

      var payload = JsonConvert.SerializeObject(new
      {
        data = new Dictionary<string, object?>
        {
          ["title"] = normalizedTitle
        }
      });

      var createResponse = client.Post(resource, payload);
      if (client.StatusCode < 200 || client.StatusCode >= 300)
      {
        helper.Message(
          $"Failed to create property '{normalizedTitle}' (status={client.StatusCode}). Response: {createResponse}",
          1,
          "ERROR"
        );
        return null;
      }

      var newPropertyId = Helper.ExtractDataId(createResponse);
      if (string.IsNullOrWhiteSpace(newPropertyId))
      {
        helper.Message(
          $"Failed to create property '{normalizedTitle}': API returned no property id.",
          1,
          "ERROR"
        );
        return null;
      }

      propertiesByTitle[normalizedTitle] = newPropertyId;
      checkedProperties[checkedByTitleKey] = newPropertyId;
      helper.Message($"Property created on the fly: '{normalizedTitle}' -> {newPropertyId}", 2);
      return newPropertyId;
    }
  }
}
