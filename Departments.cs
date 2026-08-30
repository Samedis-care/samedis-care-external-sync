using System.Data;
using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Departments
  {
    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("tenant_id")]
      public string? TenantId { get; set; }

      [JsonProperty("cost_center_number")]
      public string? CostCenterNumber { get; set; }

      [JsonProperty("created_at")]
      public string? CreatedAt { get; set; }

      [JsonProperty("created_by_user")]
      public string? CreatedByUser { get; set; }

      [JsonProperty("inventory_count")]
      public int? InventoryCount { get; set; }

      [JsonProperty("is_active")]
      public bool IsActive { get; set; }

      [JsonProperty("notes")]
      public string? Notes { get; set; }

      [JsonProperty("profit_center_title")]
      public string? ProfitCenterTitle { get; set; }

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

    public static DataSet CreateDepartmentDataSet()
    {
      var ds = new DataSet("Departments");
      var dt = new DataTable("Departments");

      dt.Columns.Add("id", typeof(string));
      dt.Columns.Add("tenant_id", typeof(string));
      dt.Columns.Add("cost_center_number", typeof(string));
      dt.Columns.Add("title", typeof(string));
      dt.Columns.Add("notes", typeof(string));
      dt.Columns.Add("profit_center_title", typeof(string));
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

    public static void FillDepartmentDataSet(DataSet ds, string json)
    {
      var root = JsonConvert.DeserializeObject<Departments.Root>(json);
      if (root?.Data == null || root.Data.Count == 0)
        return;

      var table = ds.Tables["Departments"];
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
        row["cost_center_number"] = attr.CostCenterNumber ?? "";
        row["title"] = attr.Title ?? "";
        row["notes"] = attr.Notes ?? "";
        row["profit_center_title"] = attr.ProfitCenterTitle ?? "";
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
    /// Resolves the department a row belongs to: samedis id, then cost centre number, then
    /// title. Creates it when asked to.
    /// </summary>
    /// <param name="lookup">
    /// Bound to the departments collection. It remembers hits and misses, which is what the
    /// three dictionaries and the "already checked" set this replaces did by hand.
    /// </param>
    /// <param name="syncedProfitCenters">
    /// Which department/profit-centre pairs were already written this run. Not a lookup
    /// cache: it records the outcome of a write so the same PUT is not repeated.
    /// </param>
    public static string? ResolveDepartmentId(
      IApiClient client,
      string resource,
      string departmentId,
      string departmentCostCenterNumber,
      string departmentTitle,
      string departmentNotes,
      bool createOnTheFly,
      string inventoryId,
      string inventoryTitle,
      ResourceLookup lookup,
      IDictionary<string, string> syncedProfitCenters,
      ISyncLog log,
      string profitCenterTitle = "")
    {
      departmentId = departmentId?.Trim() ?? string.Empty;
      departmentCostCenterNumber = departmentCostCenterNumber?.Trim() ?? string.Empty;
      departmentTitle = departmentTitle?.Trim() ?? string.Empty;
      departmentNotes = departmentNotes?.Trim() ?? string.Empty;
      var targetProfitCenter = profitCenterTitle?.Trim() ?? string.Empty;

      var existing = lookup.First(
        () => lookup.ById(departmentId),
        () => lookup.ByField("cost_center_number", departmentCostCenterNumber),
        () => lookup.ByField("title", departmentTitle));

      if (!string.IsNullOrWhiteSpace(existing))
      {
        EnsureProfitCenter(client, resource, existing, targetProfitCenter, syncedProfitCenters, log);
        return existing;
      }

      if (!createOnTheFly)
        return null;

      // A row may carry only a cost centre. Naming the department after it is better than
      // skipping the row, and keeps the two identifiable together.
      var effectiveTitle = string.IsNullOrWhiteSpace(departmentTitle) && departmentCostCenterNumber.Length > 0
        ? "KST " + departmentCostCenterNumber
        : departmentTitle;

      if (string.IsNullOrWhiteSpace(effectiveTitle))
        return null;

      var attributes = new Dictionary<string, object?> { ["title"] = effectiveTitle };
      JsonApi.AddStringAttribute(attributes!, "cost_center_number", departmentCostCenterNumber);
      JsonApi.AddStringAttribute(attributes!, "notes", departmentNotes);
      JsonApi.AddStringAttribute(attributes!, "profit_center_title", targetProfitCenter);

      return Records.Create(
        client, resource, attributes, log,
        $"department '{effectiveTitle}' (inventory_id='{inventoryId}', inventory_title='{inventoryTitle}')")
        is { } created && !string.IsNullOrWhiteSpace(created)
          ? Remember(lookup, created, departmentCostCenterNumber, effectiveTitle)
          : null;
    }

    private static string Remember(ResourceLookup lookup, string id, string costCenter, string title)
    {
      lookup.RememberId(id);
      lookup.RememberField("cost_center_number", costCenter, id);
      lookup.RememberField("title", title, id);
      return id;
    }

    /// <summary>
    /// Puts the department under the configured profit centre, unless it is already there.
    /// </summary>
    /// <remarks>
    /// The current value is read back before writing. The version this replaces got it for
    /// free from the record it had just deserialized during the lookup; going through the
    /// lookup returns only an id, and writing blindly would mean one PUT per department on
    /// every run. One GET instead of one PUT is the cheaper and safer trade.
    /// </remarks>
    private static void EnsureProfitCenter(IApiClient client, string resource, string departmentId,
                                           string profitCenterTitle,
                                           IDictionary<string, string> synced, ISyncLog log)
    {
      if (string.IsNullOrWhiteSpace(profitCenterTitle) || string.IsNullOrWhiteSpace(departmentId))
        return;

      var syncKey = departmentId + ":" + profitCenterTitle;
      if (synced.ContainsKey(syncKey))
        return;

      var detail = client.Get(resource + "/" + Uri.EscapeDataString(departmentId));
      if (JsonApi.IsSuccess(client.StatusCode))
      {
        var current = JsonApi.FirstData(detail)?["attributes"]?["profit_center_title"]?.ToString();
        if (string.Equals(current?.Trim() ?? string.Empty, profitCenterTitle,
                          StringComparison.OrdinalIgnoreCase))
        {
          synced[syncKey] = departmentId;
          return;
        }
      }

      var payload = JsonConvert.SerializeObject(new
      {
        data = new Dictionary<string, object?> { ["profit_center_title"] = profitCenterTitle }
      });
      var response = client.Put(resource, departmentId, payload);

      if (JsonApi.IsSuccess(client.StatusCode))
      {
        synced[syncKey] = departmentId;
        log.Debug($"Department profit center updated (department_id='{departmentId}', profit_center_title='{profitCenterTitle}').");
      }
      else
      {
        synced[syncKey] = string.Empty;
        log.Warn($"Failed to set department profit center (department_id='{departmentId}', profit_center_title='{profitCenterTitle}', status={client.StatusCode}). Response: {response}");
      }
    }


  }
}
