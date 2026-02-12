using System.Data;
using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Locations
  {
    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

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

    public static void LoadLookups(
      RequestData client,
      string resource,
      int pageSize,
      IDictionary<string, string> locationsById,
      IDictionary<string, string> locationsByTitle)
    {
      var firstResponse = client.Get(resource + "?page[number]=1&page[limit]=0&quickfilter=&gridfilter={}");
      var list = string.IsNullOrEmpty(firstResponse) ? null : JsonConvert.DeserializeObject<Locations.Root>(firstResponse);
      var totalRecords = list?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      for (var page = 1; page <= Math.Max(1, pages); page++)
      {
        var response = client.Get(resource + $"?page[number]={page}&page[limit]={pageSize}&quickfilter=&gridfilter={{}}");
        if (string.IsNullOrEmpty(response)) continue;

        var root = JsonConvert.DeserializeObject<Locations.Root>(response);
        if (root?.Data == null) continue;

        foreach (var data in root.Data)
        {
          var attr = data.Attributes;
          var id = attr?.Id ?? data.Id;
          if (string.IsNullOrWhiteSpace(id)) continue;

          locationsById[id] = id;

          var title = attr?.Title;
          if (!string.IsNullOrWhiteSpace(title))
            locationsByTitle[title] = id;
        }
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
      Helper helper)
    {
      if (!string.IsNullOrWhiteSpace(locationId) && locationsById.TryGetValue(locationId, out var existingId) && !string.IsNullOrWhiteSpace(existingId))
        return existingId;

      if (!string.IsNullOrWhiteSpace(locationTitle) && locationsByTitle.TryGetValue(locationTitle, out existingId) && !string.IsNullOrWhiteSpace(existingId))
        return existingId;

      if (!string.IsNullOrWhiteSpace(locationId))
      {
        var checkedByIdKey = "id:" + locationId;
        if (checkedLocations.TryGetValue(checkedByIdKey, out var checkedById))
        {
          if (!string.IsNullOrWhiteSpace(checkedById))
            return checkedById;
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
            var resolvedTitle = detailRoot?.Data?.FirstOrDefault()?.Attributes?.Title;
            if (!string.IsNullOrWhiteSpace(resolvedTitle))
              locationsByTitle[resolvedTitle] = resolvedId;

            checkedLocations[checkedByIdKey] = resolvedId;
            return resolvedId;
          }

          checkedLocations[checkedByIdKey] = string.Empty;
          locationsById[locationId] = string.Empty;
        }
      }

      if (!string.IsNullOrWhiteSpace(locationTitle))
      {
        var checkedByTitleKey = "title:" + locationTitle;
        if (checkedLocations.TryGetValue(checkedByTitleKey, out var checkedByTitle))
        {
          if (!string.IsNullOrWhiteSpace(checkedByTitle))
            return checkedByTitle;
        }
        else
        {
          var listResponse = client.Get(
            resource +
            $"?page[number]=1&page[limit]=1&filter[title]={Uri.EscapeDataString(locationTitle)}&quickfilter=&gridfilter={{}}"
          );
          if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(listResponse))
          {
            var listRoot = JsonConvert.DeserializeObject<Locations.Root>(listResponse);
            var foundLocation = listRoot?.Data?.FirstOrDefault();
            var resolvedId = foundLocation?.Attributes?.Id ?? foundLocation?.Id;
            if (!string.IsNullOrWhiteSpace(resolvedId))
            {
              locationsById[resolvedId] = resolvedId;
              locationsByTitle[locationTitle] = resolvedId;
              checkedLocations[checkedByTitleKey] = resolvedId;
              return resolvedId;
            }
          }

          checkedLocations[checkedByTitleKey] = string.Empty;
          locationsByTitle[locationTitle] = string.Empty;
        }
      }

      if (!createOnTheFly)
        return null;

      if (string.IsNullOrWhiteSpace(locationTitle))
        return null;

      var payload = JsonConvert.SerializeObject(new
      {
        data = new
        {
          type = "device_locations",
          attributes = new Dictionary<string, object?>
          {
            ["title"] = locationTitle
          }
        }
      });

      var response = client.Post(resource, payload);
      if (client.StatusCode < 200 || client.StatusCode >= 300)
      {
        helper.Message(
          $"Failed to create location (id='{locationId}', title='{locationTitle}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}', status={client.StatusCode}). Response: {response}",
          1,
          "ERROR"
        );
        return null;
      }

      var newLocationId = Helper.ExtractDataId(response);
      if (string.IsNullOrWhiteSpace(newLocationId))
      {
        helper.Message(
          $"Failed to create location (id='{locationId}', title='{locationTitle}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}'): API returned no location id.",
          1,
          "ERROR"
        );
        return null;
      }

      locationsById[newLocationId] = newLocationId;
      locationsByTitle[locationTitle] = newLocationId;
      checkedLocations["title:" + locationTitle] = newLocationId;
      if (!string.IsNullOrWhiteSpace(locationId))
        checkedLocations["id:" + locationId] = newLocationId;
      helper.Message($"Location created on the fly: '{locationTitle}' -> {newLocationId}", 2);
      return newLocationId;
    }
  }
}
