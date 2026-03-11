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
      [JsonConverter(typeof(Helper.SingleOrArrayConverter<Data>))]
      public List<Data>? Data { get; set; }
    }

    public static Dictionary<string, SourceBuilding> LoadSourceBuildings(string csvPath, Helper helper)
    {
      var result = new Dictionary<string, SourceBuilding>(StringComparer.OrdinalIgnoreCase);
      if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
        return result;

      DataTable sourceTable;
      try
      {
        sourceTable = Helper.ImportCsvToDataTable(csvPath, "SourceBuildings");
      }
      catch (Exception ex)
      {
        helper.Message($"Failed to read source buildings CSV '{csvPath}': {ex.Message}", 1, "WARN");
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
        var parentSourceId = Helper.GetRowValue(row, "parent_id");
        if (string.IsNullOrWhiteSpace(parentSourceId))
          parentSourceId = Helper.GetRowValue(row, "Übergeordnet");

        var number = Helper.GetRowValue(row, "Number");
        if (string.IsNullOrWhiteSpace(number))
          number = Helper.GetRowValue(row, "number");
        var street = Helper.GetRowValue(row, "street");
        var zip = Helper.GetRowValue(row, "postal_code");
        if (string.IsNullOrWhiteSpace(zip))
          zip = Helper.GetRowValue(row, "zip");
        var town = Helper.GetRowValue(row, "city");
        if (string.IsNullOrWhiteSpace(town))
          town = Helper.GetRowValue(row, "town");

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

      helper.Message($"Loaded source building map entries: {result.Count}", 2);
      return result;
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
      Helper helper,
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
          helper.Message(
            $"Building synced via PUT (match_by='{matchedBy}', id='{resolvedId}', title='{normalizedTitle}', external_id='{normalizedExternalId}').",
            2
          );
        }
        else
        {
          helper.Message(
            $"Failed to sync building via PUT (match_by='{matchedBy}', id='{resolvedId}', title='{normalizedTitle}', property_id='{propertyId}', external_id='{normalizedExternalId}', status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}'). Response: {updateResponse}",
            1,
            "WARN"
          );
        }
      }

      if (!string.IsNullOrWhiteSpace(normalizedExternalId))
      {
        if (checkedBuildings.TryGetValue(checkedExternalKey, out var checkedByExternalId))
        {
          if (!string.IsNullOrWhiteSpace(checkedByExternalId))
            return checkedByExternalId;
        }
        else
        {
          var resolvedByExternalId = Helper.ExternalIdExists(client, resource, normalizedExternalId);
          if (!string.IsNullOrWhiteSpace(resolvedByExternalId))
          {
            checkedBuildings[checkedExternalKey] = resolvedByExternalId;
            if (!string.IsNullOrWhiteSpace(propertyId) && !string.IsNullOrWhiteSpace(normalizedTitle))
            {
              buildingsByKey[key] = resolvedByExternalId;
              checkedBuildings[checkedKey] = resolvedByExternalId;
            }
            SyncExistingBuilding(resolvedByExternalId, "external_id");
            return resolvedByExternalId;
          }

          checkedBuildings[checkedExternalKey] = string.Empty;
        }
      }

      if (string.IsNullOrWhiteSpace(propertyId) || string.IsNullOrWhiteSpace(normalizedTitle))
        return null;

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
      filterBuilder.Add("title", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, normalizedTitle);
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
          SyncExistingBuilding(resolvedId, "title");
          return resolvedId;
        }
      }

      checkedBuildings[checkedKey] = string.Empty;
      buildingsByKey[key] = string.Empty;

      if (!createOnTheFly)
        return null;

      var payload = JsonConvert.SerializeObject(new
      {
        data = BuildPayload(includeEmptyAddress: false)
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
      if (!string.IsNullOrWhiteSpace(normalizedExternalId))
        checkedBuildings[checkedExternalKey] = newBuildingId;
      helper.Message($"Building created on the fly: '{normalizedTitle}' (property_id='{propertyId}') -> {newBuildingId}", 2);
      return newBuildingId;
    }
  }
}
