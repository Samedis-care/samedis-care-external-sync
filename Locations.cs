using System.Data;
using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Locations
  {
    public class SourceRoom
    {
      public string SourceId { get; set; } = string.Empty;
      public string SourceFloorId { get; set; } = string.Empty;
      public string Number { get; set; } = string.Empty;
      public string Title { get; set; } = string.Empty;
      public string PlisCode { get; set; } = string.Empty;
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

      [JsonProperty("created_at")]
      public string? CreatedAt { get; set; }

      [JsonProperty("created_by_user")]
      public string? CreatedByUser { get; set; }

      [JsonProperty("floor_id")]
      public string? FloorId { get; set; }

      [JsonProperty("inventory_count")]
      public int? InventoryCount { get; set; }

      [JsonProperty("is_active")]
      public bool IsActive { get; set; }

      [JsonProperty("notes")]
      public string? Notes { get; set; }

      [JsonProperty("path")]
      public string? Path { get; set; }

      [JsonProperty("property_id")]
      public string? PropertyId { get; set; }

      [JsonProperty("title")]
      public string? Title { get; set; }

      [JsonProperty("updated_at")]
      public string? UpdatedAt { get; set; }

      [JsonProperty("updated_by_user")]
      public string? UpdatedByUser { get; set; }

      [JsonProperty("updated_by_user_at")]
      public string? UpdatedByUserAt { get; set; }
    }

    public class Data
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("type")]
      public string? Type { get; set; }

      [JsonProperty("attributes")]
      public Attributes? Attributes { get; set; }

      [JsonProperty("relationships")]
      public Dictionary<string, object>? Relationships { get; set; }
    }

    public class Msg
    {
      [JsonProperty("success")]
      public bool Success { get; set; }

      [JsonProperty("message")]
      public string? Message { get; set; }

      [JsonProperty("error")]
      public string? Error { get; set; }

      [JsonProperty("error_details")]
      public string? ErrorDetails { get; set; }
    }

    public class JsonApiOptions
    {
      [JsonProperty("limit")]
      public int? Limit { get; set; }

      [JsonProperty("page")]
      public int? Page { get; set; }

      [JsonProperty("fields")]
      public Dictionary<string, object>? Fields { get; set; }
    }

    public class Meta
    {
      [JsonProperty("git_version")]
      public string? GitVersion { get; set; }

      [JsonProperty("json_api_options")]
      public JsonApiOptions? JsonApiOptions { get; set; }

      [JsonProperty("locale")]
      public string? Locale { get; set; }

      [JsonProperty("current_user_id")]
      public string? CurrentUserId { get; set; }

      [JsonProperty("status")]
      public int? Status { get; set; }

      [JsonProperty("total")]
      public int? Total { get; set; }

      [JsonProperty("msg")]
      public Msg? Msg { get; set; }
    }

    public class Root
    {
      [JsonProperty("data")]
      [JsonConverter(typeof(Helper.SingleOrArrayConverter<Data>))]
      public List<Data>? Data { get; set; }

      [JsonProperty("meta")]
      public Meta? Meta { get; set; }
    }

    public static Dictionary<string, SourceRoom> LoadSourceRooms(string csvPath, Helper helper)
    {
      var result = new Dictionary<string, SourceRoom>(StringComparer.OrdinalIgnoreCase);
      if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
        return result;

      DataTable sourceTable;
      try
      {
        sourceTable = Helper.ImportCsvToDataTable(csvPath, "SourceRooms");
      }
      catch (Exception ex)
      {
        helper.Message($"Failed to read source rooms CSV '{csvPath}': {ex.Message}", 1, "WARN");
        return result;
      }
      var hasPlisCodeColumn = sourceTable.Columns.Contains("plis_code");

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
        var number = Helper.GetRowValue(row, "Number");
        if (string.IsNullOrWhiteSpace(number))
          number = Helper.GetRowValue(row, "number");
        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(number))
          title = $"{title} ({number})";
        var sourceFloorId = Helper.GetRowValue(row, "parent_id");
        if (string.IsNullOrWhiteSpace(sourceFloorId))
          sourceFloorId = Helper.GetRowValue(row, "Übergeordnet");

        result[sourceId] = new SourceRoom
        {
          SourceId = sourceId,
          SourceFloorId = sourceFloorId,
          Number = number,
          Title = title,
          PlisCode = hasPlisCodeColumn ? Helper.GetRowValue(row, "plis_code") : string.Empty
        };
      }

      helper.Message($"Loaded source room map entries: {result.Count}", 2);
      return result;
    }

    public static DataSet CreateLocationDataSet()
    {
      var ds = new DataSet("Locations");
      var dt = new DataTable("Locations");

      dt.Columns.Add("id", typeof(string));
      dt.Columns.Add("tenant_id", typeof(string));
      dt.Columns.Add("building_id", typeof(string));
      dt.Columns.Add("floor_id", typeof(string));
      dt.Columns.Add("property_id", typeof(string));
      dt.Columns.Add("title", typeof(string));
      dt.Columns.Add("path", typeof(string));
      dt.Columns.Add("notes", typeof(string));
      dt.Columns.Add("inventory_count", typeof(string));
      dt.Columns.Add("is_active", typeof(string));
      dt.Columns.Add("created_at", typeof(string));
      dt.Columns.Add("created_by_user", typeof(string));
      dt.Columns.Add("updated_at", typeof(string));
      dt.Columns.Add("updated_by_user", typeof(string));
      dt.Columns.Add("updated_by_user_at", typeof(string));

      var idColumn = dt.Columns["Id"] ?? throw new InvalidOperationException("The 'Id' column was not found in the DataTable.");
      dt.PrimaryKey = [idColumn];

      ds.Tables.Add(dt);
      return ds;
    }

    public static void FillLocationDataSet(DataSet ds, string json)
    {
      var root = JsonConvert.DeserializeObject<Locations.Root>(json);
      if (root?.Data == null || root.Data.Count == 0)
        return;

      var table = ds.Tables["Locations"];
      if (table == null) return;

      foreach (var data in root.Data)
      {
        var attr = data.Attributes;
        if (attr == null) continue;

        if (table.Rows.Contains(attr.Id))
          continue;

        var row = table.NewRow();

        row["id"] = attr.Id;
        row["tenant_id"] = attr.TenantId ?? "";
        row["building_id"] = attr.BuildingId ?? "";
        row["floor_id"] = attr.FloorId ?? "";
        row["property_id"] = attr.PropertyId ?? "";
        row["title"] = attr.Title ?? "";
        row["path"] = attr.Path ?? "";
        row["notes"] = attr.Notes ?? "";
        row["inventory_count"] = attr.InventoryCount?.ToString() ?? "";
        row["is_active"] = attr.IsActive ? "Yes" : "No";
        row["created_at"] = attr.CreatedAt ?? "";
        row["created_by_user"] = attr.CreatedByUser ?? "";
        row["updated_at"] = attr.UpdatedAt ?? "";
        row["updated_by_user"] = attr.UpdatedByUser ?? "";
        row["updated_by_user_at"] = attr.UpdatedByUserAt ?? "";

        table.Rows.Add(row);
      }
    }

    public static string? ResolveLocationId(
      RequestData client,
      string resource,
      string locationId,
      string locationTitle,
      bool createOnTheFly,
      string inventoryId,
      string inventoryTitle,
      IDictionary<string, string> locationsById,
      IDictionary<string, string> locationsByTitle,
      IDictionary<string, string> checkedLocations,
      Helper helper,
      string? propertyId = null,
      string? buildingId = null,
      string? floorId = null,
      string? locationNotes = null,
      string externalId = "",
      bool updateOnExisting = false)
    {
      if (!string.IsNullOrWhiteSpace(locationId) && locationsById.TryGetValue(locationId, out var existingId) && !string.IsNullOrWhiteSpace(existingId))
        return existingId;

      var normalizedLocationTitle = locationTitle?.Trim() ?? string.Empty;
      var normalizedExternalId = externalId?.Trim() ?? string.Empty;
      var normalizedLocationNotes = locationNotes?.Trim() ?? string.Empty;
      var hasHierarchyScope = !string.IsNullOrWhiteSpace(propertyId) || !string.IsNullOrWhiteSpace(buildingId) || !string.IsNullOrWhiteSpace(floorId);
      var scopeKey = hasHierarchyScope ? $"{propertyId ?? string.Empty}|{buildingId ?? string.Empty}|{floorId ?? string.Empty}" : string.Empty;
      var titleLookupKey = hasHierarchyScope ? $"{scopeKey}|{normalizedLocationTitle}" : normalizedLocationTitle;
      var useScopedExternalLookup = hasHierarchyScope && !updateOnExisting;
      var externalLookupKey = useScopedExternalLookup ? $"{scopeKey}|{normalizedExternalId}" : normalizedExternalId;

      Dictionary<string, object?> BuildPayload(bool includeEmptyNotes)
      {
        var payload = new Dictionary<string, object?>
        {
          ["title"] = normalizedLocationTitle
        };
        if (!string.IsNullOrWhiteSpace(normalizedExternalId))
          payload["external_id"] = normalizedExternalId;
        if (!string.IsNullOrWhiteSpace(propertyId))
          payload["property_id"] = propertyId;
        if (!string.IsNullOrWhiteSpace(buildingId))
          payload["building_id"] = buildingId;
        if (!string.IsNullOrWhiteSpace(floorId))
          payload["floor_id"] = floorId;
        if (includeEmptyNotes || !string.IsNullOrWhiteSpace(normalizedLocationNotes))
          payload["notes"] = normalizedLocationNotes;

        return payload;
      }

      void SyncExistingLocation(string resolvedId, string matchedBy)
      {
        if (!updateOnExisting || string.IsNullOrWhiteSpace(resolvedId) || string.IsNullOrWhiteSpace(normalizedLocationTitle))
          return;

        var updatePayload = JsonConvert.SerializeObject(new
        {
          data = BuildPayload(includeEmptyNotes: true)
        });
        var updateResponse = client.Put(resource, resolvedId, updatePayload);
        if (client.StatusCode >= 200 && client.StatusCode < 300)
        {
          helper.Message(
            $"Location synced via PUT (match_by='{matchedBy}', id='{resolvedId}', title='{normalizedLocationTitle}', external_id='{normalizedExternalId}').",
            2
          );
        }
        else
        {
          helper.Message(
            $"Failed to sync location via PUT (match_by='{matchedBy}', id='{resolvedId}', title='{normalizedLocationTitle}', property_id='{propertyId}', building_id='{buildingId}', floor_id='{floorId}', external_id='{normalizedExternalId}', status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}'). Response: {updateResponse}",
            1,
            "WARN"
          );
        }
      }

      if (!string.IsNullOrWhiteSpace(normalizedExternalId))
      {
        var checkedByExternalKey = "external_id:" + externalLookupKey;
        if (checkedLocations.TryGetValue(checkedByExternalKey, out var checkedByExternal))
        {
          if (!string.IsNullOrWhiteSpace(checkedByExternal))
          {
            SyncExistingLocation(checkedByExternal, "external_id_cache");
            return checkedByExternal;
          }
        }
        else
        {
          var resolvedExternalId = Helper.ExternalIdExists(client, resource, normalizedExternalId);
          if (!string.IsNullOrWhiteSpace(resolvedExternalId))
          {
            locationsById[resolvedExternalId] = resolvedExternalId;
            checkedLocations[checkedByExternalKey] = resolvedExternalId;
            if (!string.IsNullOrWhiteSpace(normalizedLocationTitle))
            {
              locationsByTitle[titleLookupKey] = resolvedExternalId;
              checkedLocations["title:" + titleLookupKey] = resolvedExternalId;
            }
            SyncExistingLocation(resolvedExternalId, "external_id");
            return resolvedExternalId;
          }

          helper.Message(
            $"Location lookup by external_id returned no match (external_id='{normalizedExternalId}', property_id='{propertyId}', building_id='{buildingId}', floor_id='{floorId}', status={client.StatusCode} {client.Status}).",
            2,
            "WARN"
          );

          checkedLocations[checkedByExternalKey] = string.Empty;
        }
      }

      if (!string.IsNullOrWhiteSpace(normalizedLocationTitle) && locationsByTitle.TryGetValue(titleLookupKey, out existingId) && !string.IsNullOrWhiteSpace(existingId))
      {
        SyncExistingLocation(existingId, "title_cache");
        return existingId;
      }

      if (!string.IsNullOrWhiteSpace(locationId))
      {
        var checkedByIdKey = "id:" + locationId;
        if (checkedLocations.TryGetValue(checkedByIdKey, out var checkedById))
        {
          if (!string.IsNullOrWhiteSpace(checkedById))
          {
            SyncExistingLocation(checkedById, "id_cache");
            return checkedById;
          }
        }
        else
        {
          var detailResponse = client.Get(resource + "/" + Uri.EscapeDataString(locationId));
          if (client.StatusCode == 200)
          {
            var resolvedId = Helper.ExtractDataId(detailResponse) ?? locationId;
            locationsById[locationId] = resolvedId;
            locationsById[resolvedId] = resolvedId;

            var detailRoot = string.IsNullOrEmpty(detailResponse) ? null : JsonConvert.DeserializeObject<Locations.Root>(detailResponse);
            var resolvedAttributes = detailRoot?.Data?.FirstOrDefault()?.Attributes;
            var resolvedTitle = resolvedAttributes?.Title;
            if (!string.IsNullOrWhiteSpace(resolvedTitle))
            {
              var resolvedTitleKey = hasHierarchyScope
                ? $"{resolvedAttributes?.PropertyId ?? propertyId ?? string.Empty}|{resolvedAttributes?.BuildingId ?? buildingId ?? string.Empty}|{resolvedAttributes?.FloorId ?? floorId ?? string.Empty}|{resolvedTitle}"
                : resolvedTitle;
              locationsByTitle[resolvedTitleKey] = resolvedId;
            }

            checkedLocations[checkedByIdKey] = resolvedId;
            SyncExistingLocation(resolvedId, "id");
            return resolvedId;
          }

          checkedLocations[checkedByIdKey] = string.Empty;
          locationsById[locationId] = string.Empty;
        }
      }

      if (!string.IsNullOrWhiteSpace(normalizedLocationTitle))
      {
        var checkedByTitleKey = "title:" + titleLookupKey;
        if (checkedLocations.TryGetValue(checkedByTitleKey, out var checkedByTitle))
        {
          if (!string.IsNullOrWhiteSpace(checkedByTitle))
            return checkedByTitle;
        }
        else
        {
          var filterBuilder = new FilterBuilder();
          filterBuilder.Clear();
          filterBuilder.Add("title", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, normalizedLocationTitle);
          if (!string.IsNullOrWhiteSpace(propertyId))
            filterBuilder.Add("property_id", FilterBuilder.FilterType.Equals, FilterBuilder.Type.ObjectId, propertyId);
          if (!string.IsNullOrWhiteSpace(buildingId))
            filterBuilder.Add("building_id", FilterBuilder.FilterType.Equals, FilterBuilder.Type.ObjectId, buildingId);
          if (!string.IsNullOrWhiteSpace(floorId))
            filterBuilder.Add("floor_id", FilterBuilder.FilterType.Equals, FilterBuilder.Type.ObjectId, floorId);

          var listResponse = client.Get(
            resource +
            $"?page[number]=1&page[limit]=1&gridfilter={filterBuilder.Get()}"
          );
          if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(listResponse))
          {
            var listRoot = JsonConvert.DeserializeObject<Locations.Root>(listResponse);
            var foundLocation = listRoot?.Data?.FirstOrDefault();
            var resolvedId = foundLocation?.Attributes?.Id ?? foundLocation?.Id;
            if (!string.IsNullOrWhiteSpace(resolvedId))
            {
              locationsById[resolvedId] = resolvedId;
              locationsByTitle[titleLookupKey] = resolvedId;
              checkedLocations[checkedByTitleKey] = resolvedId;
              SyncExistingLocation(resolvedId, "title");
              return resolvedId;
            }
          }

          checkedLocations[checkedByTitleKey] = string.Empty;
          locationsByTitle[titleLookupKey] = string.Empty;
        }
      }

      if (!createOnTheFly)
        return null;

      if (string.IsNullOrWhiteSpace(normalizedLocationTitle))
        return null;

      var payload = JsonConvert.SerializeObject(new
      {
        data = BuildPayload(includeEmptyNotes: false)
      });

      var response = client.Post(resource, payload);
      if (client.StatusCode < 200 || client.StatusCode >= 300)
      {
        helper.Message(
          $"Failed to create location (id='{locationId}', title='{normalizedLocationTitle}', property_id='{propertyId}', building_id='{buildingId}', floor_id='{floorId}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}', status={client.StatusCode}). Response: {response}",
          1,
          "ERROR"
        );
        return null;
      }

      var newLocationId = Helper.ExtractDataId(response);
      if (string.IsNullOrWhiteSpace(newLocationId))
      {
        helper.Message(
          $"Failed to create location (id='{locationId}', title='{normalizedLocationTitle}', property_id='{propertyId}', building_id='{buildingId}', floor_id='{floorId}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}'): API returned no location id.",
          1,
          "ERROR"
        );
        return null;
      }

      locationsById[newLocationId] = newLocationId;
      locationsByTitle[titleLookupKey] = newLocationId;
      checkedLocations["title:" + titleLookupKey] = newLocationId;
      if (!string.IsNullOrWhiteSpace(locationId))
        checkedLocations["id:" + locationId] = newLocationId;
      helper.Message($"Location created on the fly: '{normalizedLocationTitle}' -> {newLocationId}", 2);
      return newLocationId;
    }
  }
}
