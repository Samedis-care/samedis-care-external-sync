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
      [JsonConverter(typeof(Helper.SingleOrArrayConverter<Data>))]
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

    public static string? ResolveDepartmentId(
      RequestData client,
      string resource,
      string departmentId,
      string departmentCostCenterNumber,
      string departmentTitle,
      string departmentNotes,
      bool createOnTheFly,
      string inventoryId,
      string inventoryTitle,
      IDictionary<string, string> departmentsById,
      IDictionary<string, string> departmentsByCostCenter,
      IDictionary<string, string> departmentsByTitle,
      IDictionary<string, string> checkedDepartments,
      Helper helper)
    {
      departmentId = departmentId ?? string.Empty;
      departmentCostCenterNumber = departmentCostCenterNumber ?? string.Empty;
      departmentTitle = departmentTitle ?? string.Empty;
      departmentNotes = departmentNotes ?? string.Empty;

      if (!string.IsNullOrWhiteSpace(departmentId) && departmentsById.TryGetValue(departmentId, out var existingId) && !string.IsNullOrWhiteSpace(existingId))
        return existingId;

      if (!string.IsNullOrWhiteSpace(departmentCostCenterNumber) &&
          departmentsByCostCenter.TryGetValue(departmentCostCenterNumber, out existingId) &&
          !string.IsNullOrWhiteSpace(existingId))
      {
        return existingId;
      }

      if (!string.IsNullOrWhiteSpace(departmentTitle) &&
          departmentsByTitle.TryGetValue(departmentTitle, out existingId) &&
          !string.IsNullOrWhiteSpace(existingId))
      {
        return existingId;
      }

      if (!string.IsNullOrWhiteSpace(departmentId))
      {
        var checkedByIdKey = "id:" + departmentId;
        if (checkedDepartments.TryGetValue(checkedByIdKey, out var checkedById))
        {
          if (!string.IsNullOrWhiteSpace(checkedById))
            return checkedById;
        }
        else
        {
          var detailResponse = client.Get(resource + "/" + Uri.EscapeDataString(departmentId));
          if (client.StatusCode == 200)
          {
            var resolvedId = Helper.ExtractDataId(detailResponse) ?? departmentId;
            departmentsById[departmentId] = resolvedId;
            departmentsById[resolvedId] = resolvedId;

            var detailRoot = string.IsNullOrEmpty(detailResponse) ? null : JsonConvert.DeserializeObject<Departments.Root>(detailResponse);
            var resolvedAttributes = detailRoot?.Data?.FirstOrDefault()?.Attributes;
            var resolvedTitle = resolvedAttributes?.Title;
            var resolvedCostCenterNumber = resolvedAttributes?.CostCenterNumber;
            if (!string.IsNullOrWhiteSpace(resolvedTitle))
              departmentsByTitle[resolvedTitle] = resolvedId;
            if (!string.IsNullOrWhiteSpace(resolvedCostCenterNumber))
              departmentsByCostCenter[resolvedCostCenterNumber] = resolvedId;

            checkedDepartments[checkedByIdKey] = resolvedId;
            return resolvedId;
          }

          if (client.StatusCode == 404)
          {
            checkedDepartments[checkedByIdKey] = string.Empty;
            departmentsById[departmentId] = string.Empty;
          }
          else
          {
            helper.Message(
              $"Department id lookup request failed for '{departmentId}' (status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}').",
              2,
              "WARN"
            );
          }
        }
      }

      if (!string.IsNullOrWhiteSpace(departmentCostCenterNumber))
      {
        var checkedByCostCenterKey = "cost_center:" + departmentCostCenterNumber;
        if (checkedDepartments.TryGetValue(checkedByCostCenterKey, out var checkedByCostCenter))
        {
          if (!string.IsNullOrWhiteSpace(checkedByCostCenter))
            return checkedByCostCenter;
        }
        else
        {
          var filterBuilder = new FilterBuilder();
          filterBuilder.Clear();
          filterBuilder.Add("cost_center_number", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, departmentCostCenterNumber);

          var requestResource = resource + $"?page[number]=1&page[limit]=1&gridfilter={filterBuilder.Get()}";
          helper.Message($"Department cost_center lookup request: {requestResource}", 2, "DEBUG");
          var listResponse = client.Get(requestResource);
          helper.Message(
            $"Department cost_center lookup response: status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}', content_length={(listResponse ?? string.Empty).Length}",
            2,
            "DEBUG"
          );
          if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(listResponse))
          {
            var listRoot = JsonConvert.DeserializeObject<Departments.Root>(listResponse);
            var foundDepartment = listRoot?.Data?.FirstOrDefault();
            var resolvedId = foundDepartment?.Attributes?.Id ?? foundDepartment?.Id;
            var resolvedTitle = foundDepartment?.Attributes?.Title;
            if (!string.IsNullOrWhiteSpace(resolvedId))
            {
              departmentsById[resolvedId] = resolvedId;
              departmentsByCostCenter[departmentCostCenterNumber] = resolvedId;
              if (!string.IsNullOrWhiteSpace(resolvedTitle))
                departmentsByTitle[resolvedTitle] = resolvedId;
              checkedDepartments[checkedByCostCenterKey] = resolvedId;
              return resolvedId;
            }

            checkedDepartments[checkedByCostCenterKey] = string.Empty;
            departmentsByCostCenter[departmentCostCenterNumber] = string.Empty;
          }
          else if (client.StatusCode != 200)
          {
            helper.Message(
              $"Department cost_center lookup request failed for '{departmentCostCenterNumber}' (status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}').",
              2,
              "WARN"
            );
          }
        }
      }

      if (!string.IsNullOrWhiteSpace(departmentTitle))
      {
        var checkedByTitleKey = "title:" + departmentTitle;
        if (checkedDepartments.TryGetValue(checkedByTitleKey, out var checkedByTitle))
        {
          if (!string.IsNullOrWhiteSpace(checkedByTitle))
            return checkedByTitle;
        }
        else
        {
          var filterBuilder = new FilterBuilder();
          filterBuilder.Clear();
          filterBuilder.Add("title", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, departmentTitle);
          var gridFilter = filterBuilder.Get();
          var requestResource = resource + $"?page[number]=1&page[limit]=1&gridfilter={gridFilter}";
          helper.Message($"Department title lookup request: {requestResource}", 2, "DEBUG");
          var listResponse = client.Get(requestResource);
          helper.Message(
            $"Department title lookup response: status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}', content_length={(listResponse ?? string.Empty).Length}",
            2,
            "DEBUG"
          );
          if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(listResponse))
          {
            var listRoot = JsonConvert.DeserializeObject<Departments.Root>(listResponse);
            var foundDepartment = listRoot?.Data?.FirstOrDefault();
            var resolvedId = foundDepartment?.Attributes?.Id ?? foundDepartment?.Id;
            var resolvedCostCenterNumber = foundDepartment?.Attributes?.CostCenterNumber;
            if (!string.IsNullOrWhiteSpace(resolvedId))
            {
              departmentsById[resolvedId] = resolvedId;
              departmentsByTitle[departmentTitle] = resolvedId;
              if (!string.IsNullOrWhiteSpace(departmentCostCenterNumber))
                departmentsByCostCenter[departmentCostCenterNumber] = resolvedId;
              if (!string.IsNullOrWhiteSpace(resolvedCostCenterNumber))
                departmentsByCostCenter[resolvedCostCenterNumber] = resolvedId;
              checkedDepartments[checkedByTitleKey] = resolvedId;
              return resolvedId;
            }

            checkedDepartments[checkedByTitleKey] = string.Empty;
            departmentsByTitle[departmentTitle] = string.Empty;
          }
          else if (client.StatusCode != 200)
          {
            helper.Message(
              $"Department title lookup request failed for '{departmentTitle}' (status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}').",
              2,
              "WARN"
            );
          }
        }
      }

      if (!createOnTheFly)
        return null;

      var effectiveDepartmentTitle = departmentTitle;
      if (string.IsNullOrWhiteSpace(effectiveDepartmentTitle) && !string.IsNullOrWhiteSpace(departmentCostCenterNumber))
        effectiveDepartmentTitle = "KST " + departmentCostCenterNumber;

      if (string.IsNullOrWhiteSpace(effectiveDepartmentTitle))
        return null;

      var payloadData = new Dictionary<string, object?>
      {
        ["title"] = effectiveDepartmentTitle
      };
      if (!string.IsNullOrWhiteSpace(departmentCostCenterNumber))
        payloadData["cost_center_number"] = departmentCostCenterNumber;
      if (!string.IsNullOrWhiteSpace(departmentNotes))
        payloadData["notes"] = departmentNotes;

      var payload = JsonConvert.SerializeObject(new
      {
        data = payloadData
      });

      var response = client.Post(resource, payload);
      if (client.StatusCode < 200 || client.StatusCode >= 300)
      {
        helper.Message(
          $"Failed to create department (id='{departmentId}', title='{effectiveDepartmentTitle}', cost_center_number='{departmentCostCenterNumber}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}', status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}'). Response: {response}",
          1,
          "ERROR"
        );
        return null;
      }

      var newDepartmentId = Helper.ExtractDataId(response);
      if (string.IsNullOrWhiteSpace(newDepartmentId))
      {
        helper.Message(
          $"Failed to create department (id='{departmentId}', title='{effectiveDepartmentTitle}', cost_center_number='{departmentCostCenterNumber}', inventory_id='{inventoryId}', inventory_title='{inventoryTitle}'): API returned no department id.",
          1,
          "ERROR"
        );
        return null;
      }

      departmentsById[newDepartmentId] = newDepartmentId;
      departmentsByTitle[effectiveDepartmentTitle] = newDepartmentId;
      checkedDepartments["title:" + effectiveDepartmentTitle] = newDepartmentId;
      if (!string.IsNullOrWhiteSpace(departmentCostCenterNumber))
      {
        departmentsByCostCenter[departmentCostCenterNumber] = newDepartmentId;
        checkedDepartments["cost_center:" + departmentCostCenterNumber] = newDepartmentId;
      }
      if (!string.IsNullOrWhiteSpace(departmentId))
        checkedDepartments["id:" + departmentId] = newDepartmentId;
      helper.Message($"Department created on the fly: '{effectiveDepartmentTitle}' -> {newDepartmentId}", 2);
      return newDepartmentId;
    }
  }
}
