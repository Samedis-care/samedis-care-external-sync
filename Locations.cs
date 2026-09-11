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
      [JsonConverter(typeof(JsonApi.SingleOrArrayConverter<Data>))]
      public List<Data>? Data { get; set; }

      [JsonProperty("meta")]
      public Meta? Meta { get; set; }
    }

    public static Dictionary<string, SourceRoom> LoadSourceRooms(string csvPath, ISyncLog log)
    {
      var result = new Dictionary<string, SourceRoom>(StringComparer.OrdinalIgnoreCase);
      if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
        return result;

      DataTable sourceTable;
      try
      {
        sourceTable = Csv.Read(csvPath, tableName: "SourceRooms", trimFields: true);
      }
      catch (Exception ex)
      {
        log.Warn($"Failed to read source rooms CSV '{csvPath}': {ex.Message}");
        return result;
      }
      var hasPlisCodeColumn = sourceTable.Columns.Contains("plis_code");

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
        var number = Rows.Value(row, "Number");
        if (string.IsNullOrWhiteSpace(number))
          number = Rows.Value(row, "number");
        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(number))
          title = $"{title} ({number})";
        var sourceFloorId = Rows.Value(row, "parent_id");
        if (string.IsNullOrWhiteSpace(sourceFloorId))
          sourceFloorId = Rows.Value(row, "Übergeordnet");

        result[sourceId] = new SourceRoom
        {
          SourceId = sourceId,
          SourceFloorId = sourceFloorId,
          Number = number,
          Title = title,
          PlisCode = hasPlisCodeColumn ? Rows.Value(row, "plis_code") : string.Empty
        };
      }

      log.Debug($"Loaded source room map entries: {result.Count}");
      return result;
    }

    public static DataSet CreateLocationDataSet()
    {
      var ds = new DataSet("Locations");
      var dt = new DataTable("Locations");

      dt.Columns.Add("id", typeof(string));
      dt.Columns.Add("external_id", typeof(string));
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
        row["external_id"] = attr.ExternalId ?? "";
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

    /// <summary>
    /// Resolves the room the source names, creating it below whichever part of the hierarchy
    /// the row carries.
    /// </summary>
    /// <remarks>
    /// Unlike a building or a floor, a room may sit directly under a property, so the parent
    /// is optional here. It also carries an id of its own -- the inventory row names its room
    /// by id where the source knows one -- which is why this is the one node with an id step
    /// in its cascade.
    /// <para>
    /// The notes field is sent on an update even when empty, so a note removed upstream is
    /// cleared rather than left standing.
    /// </para>
    /// </remarks>
    public static string? ResolveLocationId(
      IApiClient client,
      string resource,
      string locationId,
      string locationTitle,
      bool createOnTheFly,
      string inventoryId,
      string inventoryTitle,
      ResourceLookup lookup,
      ISyncLog log,
      string? propertyId = null,
      string? buildingId = null,
      string? floorId = null,
      string? locationNotes = null,
      string externalId = "",
      bool updateOnExisting = false)
    {
      var title = locationTitle?.Trim() ?? string.Empty;
      var external = externalId?.Trim() ?? string.Empty;
      var notes = locationNotes?.Trim() ?? string.Empty;

      Dictionary<string, object?> Attributes(bool clearing)
      {
        var payload = new Dictionary<string, object?> { ["title"] = title };
        if (!string.IsNullOrWhiteSpace(external)) payload["external_id"] = external;
        if (!string.IsNullOrWhiteSpace(propertyId)) payload["property_id"] = propertyId;
        if (!string.IsNullOrWhiteSpace(buildingId)) payload["building_id"] = buildingId;
        if (!string.IsNullOrWhiteSpace(floorId)) payload["floor_id"] = floorId;
        if (clearing || !string.IsNullOrWhiteSpace(notes)) payload["notes"] = notes;
        return payload;
      }

      return Hierarchy.Resolve(client, resource,
        new Hierarchy.Node("Location", title, external,
                           new (string, string?)[]
                           {
                             ("property_id", propertyId),
                             ("building_id", buildingId),
                             ("floor_id", floorId),
                           },
                           Attributes)
        {
          Id = locationId,
          RequiresScope = false,
          Context = $"inventory_id='{inventoryId}', inventory_title='{inventoryTitle}'",
        },
        lookup, log, createOnTheFly, updateOnExisting);
    }
  }
}
