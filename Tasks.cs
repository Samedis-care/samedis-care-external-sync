using System.Data;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Tasks
  {
    private static readonly CultureInfo DeCulture = CultureInfo.GetCultureInfo("de-DE");
    public static readonly string[] UploadRequiredColumns =
    [
      "issue_number",
      "inventory_device_number",
      "issue_type",
      "title",
      "status",
      "done_at",
      "responsible_name",
      "maintenance_passed",
      "filename"
    ];

    public class TaskDocuments
    {
      public class Attributes
      {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("created_at")]
        public string? CreatedAt { get; set; }

        [JsonProperty("created_by_user")]
        public string? CreatedByUser { get; set; }

        [JsonProperty("mime_type")]
        public string? MimeType { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("updated_at")]
        public string? UpdatedAt { get; set; }

        [JsonProperty("updated_by_user")]
        public string? UpdatedByUser { get; set; }

        [JsonProperty("updated_by_user_at")]
        public string? UpdatedByUserAt { get; set; }
      }

      public class Links
      {
        [JsonProperty("document")]
        public string? Document { get; set; }
      }

      public class Data
      {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("attributes")]
        public Attributes? Attributes { get; set; }

        [JsonProperty("links")]
        public Links? Links { get; set; }
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
    }

    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("tenant_id")]
      public string? TenantId { get; set; }

      [JsonProperty("authority")]
      public Dictionary<string, object>? Authority { get; set; }

      [JsonProperty("auto_created")]
      public bool? AutoCreated { get; set; }

      [JsonProperty("cost_in_cents")]
      public long? CostInCents { get; set; }

      [JsonProperty("created_at")]
      public string? CreatedAt { get; set; }

      [JsonProperty("created_by_user")]
      public string? CreatedByUser { get; set; }

      [JsonProperty("currency_code")]
      public string? CurrencyCode { get; set; }

      [JsonProperty("date")]
      public string? Date { get; set; }

      [JsonProperty("department_title")]
      public string? DepartmentTitle { get; set; }

      [JsonProperty("device_model_current_responsible_manufacturer")]
      public string? DeviceModelCurrentResponsibleManufacturer { get; set; }

      [JsonProperty("device_model_manufacturer_according_to_type_plate")]
      public string? DeviceModelManufacturerAccordingToTypePlate { get; set; }

      [JsonProperty("device_model_title")]
      public string? DeviceModelTitle { get; set; }

      [JsonProperty("device_model_version")]
      public string? DeviceModelVersion { get; set; }

      [JsonProperty("device_model_version_number")]
      public int? DeviceModelVersionNumber { get; set; }

      [JsonProperty("device_type_title")]
      public string? DeviceTypeTitle { get; set; }

      [JsonProperty("device_type_title_labels")]
      public Dictionary<string, string>? DeviceTypeTitleLabels { get; set; }

      [JsonProperty("done_at")]
      public string? DoneAt { get; set; }

      [JsonProperty("external_id")]
      public string? ExternalId { get; set; }

      [JsonProperty("inventory_device_location_title")]
      public string? InventoryDeviceLocationTitle { get; set; }

      [JsonProperty("inventory_device_number")]
      public string? InventoryDeviceNumber { get; set; }

      [JsonProperty("inventory_device_picture")]
      public string? InventoryDevicePicture { get; set; }

      [JsonProperty("inventory_id")]
      public string? InventoryId { get; set; }

      [JsonProperty("inventory_no_medical_device")]
      public bool? InventoryNoMedicalDevice { get; set; }

      [JsonProperty("inventory_operation_status")]
      public string? InventoryOperationStatus { get; set; }

      [JsonProperty("issue_number")]
      public int? IssueNumber { get; set; }

      [JsonProperty("issue_type")]
      public string? IssueType { get; set; }

      [JsonProperty("maintenance_passed")]
      public bool? MaintenancePassed { get; set; }

      [JsonProperty("patient_data_securely_removed")]
      public bool? PatientDataSecurelyRemoved { get; set; }

      [JsonProperty("responsible_id")]
      public string? ResponsibleId { get; set; }

      [JsonProperty("responsible_name")]
      public string? ResponsibleName { get; set; }

      [JsonProperty("sales_order_number")]
      public string? SalesOrderNumber { get; set; }

      [JsonProperty("status")]
      public string? Status { get; set; }

      [JsonProperty("test_comment")]
      public string? TestComment { get; set; }

      [JsonProperty("test_result")]
      public string? TestResult { get; set; }

      [JsonProperty("test_protocol_url")]
      public string? TestProtocolUrl { get; set; }

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

    public static DataSet CreateTaskDataSet()
    {
      var ds = new DataSet("Tasks");
      var dt = new DataTable("Tasks");

      dt.Columns.Add("id", typeof(string));
      dt.Columns.Add("external_id", typeof(string));
      dt.Columns.Add("title", typeof(string));
      dt.Columns.Add("issue_type", typeof(string));
      dt.Columns.Add("status", typeof(string));
      dt.Columns.Add("issue_number", typeof(string));
      dt.Columns.Add("date", typeof(string));
      dt.Columns.Add("done_at", typeof(string));
      dt.Columns.Add("created_at", typeof(string));
      dt.Columns.Add("updated_at", typeof(string));
      dt.Columns.Add("created_by_user", typeof(string));
      dt.Columns.Add("updated_by_user", typeof(string));
      dt.Columns.Add("updated_by_user_at", typeof(string));
      dt.Columns.Add("department_title", typeof(string));
      dt.Columns.Add("inventory_id", typeof(string));
      dt.Columns.Add("inventory_device_number", typeof(string));
      dt.Columns.Add("inventory_device_location_title", typeof(string));
      dt.Columns.Add("inventory_device_picture", typeof(string));
      dt.Columns.Add("inventory_operation_status", typeof(string));
      dt.Columns.Add("inventory_no_medical_device", typeof(string));
      dt.Columns.Add("device_model_title", typeof(string));
      dt.Columns.Add("device_model_version", typeof(string));
      dt.Columns.Add("device_model_version_number", typeof(string));
      dt.Columns.Add("device_model_current_responsible_manufacturer", typeof(string));
      dt.Columns.Add("device_model_manufacturer_according_to_type_plate", typeof(string));
      dt.Columns.Add("device_type_title", typeof(string));
      dt.Columns.Add("responsible_id", typeof(string));
      dt.Columns.Add("responsible_name", typeof(string));
      dt.Columns.Add("auto_created", typeof(string));
      dt.Columns.Add("maintenance_passed", typeof(string));
      dt.Columns.Add("patient_data_securely_removed", typeof(string));
      dt.Columns.Add("cost_in_cents", typeof(string));
      dt.Columns.Add("currency_code", typeof(string));
      dt.Columns.Add("sales_order_number", typeof(string));
      dt.Columns.Add("test_result", typeof(string));
      dt.Columns.Add("test_comment", typeof(string));
      dt.Columns.Add("tenant_id", typeof(string));
      dt.Columns.Add("authority", typeof(string));
      dt.Columns.Add("device_type_title_labels", typeof(string));

      var idColumn = dt.Columns["Id"] ?? throw new InvalidOperationException("The 'Id' column was not found in the DataTable.");
      dt.PrimaryKey = [idColumn];

      ds.Tables.Add(dt);
      return ds;
    }

    public static void FillTaskDataSet(DataSet ds, string json)
    {
      var root = JsonConvert.DeserializeObject<Tasks.Root>(json);
      if (root?.Data == null || root.Data.Count == 0)
        return;

      var table = ds.Tables["Tasks"];
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
        row["title"] = attr.Title ?? "";
        row["issue_type"] = attr.IssueType ?? "";
        row["status"] = attr.Status ?? "";
        row["issue_number"] = attr.IssueNumber?.ToString() ?? "";
        row["date"] = attr.Date ?? "";
        row["done_at"] = attr.DoneAt ?? "";
        row["created_at"] = attr.CreatedAt ?? "";
        row["updated_at"] = attr.UpdatedAt ?? "";
        row["created_by_user"] = attr.CreatedByUser ?? "";
        row["updated_by_user"] = attr.UpdatedByUser ?? "";
        row["updated_by_user_at"] = attr.UpdatedByUserAt ?? "";
        row["department_title"] = attr.DepartmentTitle ?? "";
        row["inventory_id"] = attr.InventoryId ?? "";
        row["inventory_device_number"] = attr.InventoryDeviceNumber ?? "";
        row["inventory_device_location_title"] = attr.InventoryDeviceLocationTitle ?? "";
        row["inventory_device_picture"] = attr.InventoryDevicePicture ?? "";
        row["inventory_operation_status"] = attr.InventoryOperationStatus ?? "";
        row["inventory_no_medical_device"] = attr.InventoryNoMedicalDevice.HasValue ? (attr.InventoryNoMedicalDevice.Value ? "Yes" : "No") : "";
        row["device_model_title"] = attr.DeviceModelTitle ?? "";
        row["device_model_version"] = attr.DeviceModelVersion ?? "";
        row["device_model_version_number"] = attr.DeviceModelVersionNumber?.ToString() ?? "";
        row["device_model_current_responsible_manufacturer"] = attr.DeviceModelCurrentResponsibleManufacturer ?? "";
        row["device_model_manufacturer_according_to_type_plate"] = attr.DeviceModelManufacturerAccordingToTypePlate ?? "";
        row["device_type_title"] = attr.DeviceTypeTitle ?? "";
        row["responsible_id"] = attr.ResponsibleId ?? "";
        row["responsible_name"] = attr.ResponsibleName ?? "";
        row["auto_created"] = attr.AutoCreated.HasValue ? (attr.AutoCreated.Value ? "Yes" : "No") : "";
        row["maintenance_passed"] = attr.MaintenancePassed.HasValue ? (attr.MaintenancePassed.Value ? "Yes" : "No") : "";
        row["patient_data_securely_removed"] = attr.PatientDataSecurelyRemoved.HasValue ? (attr.PatientDataSecurelyRemoved.Value ? "Yes" : "No") : "";
        row["cost_in_cents"] = attr.CostInCents?.ToString() ?? "";
        row["currency_code"] = attr.CurrencyCode ?? "";
        row["sales_order_number"] = attr.SalesOrderNumber ?? "";
        row["test_result"] = attr.TestResult ?? "";
        row["test_comment"] = attr.TestComment ?? "";
        row["tenant_id"] = attr.TenantId ?? "";
        row["authority"] = attr.Authority != null ? JsonConvert.SerializeObject(attr.Authority) : "";
        row["device_type_title_labels"] = attr.DeviceTypeTitleLabels != null ? JsonConvert.SerializeObject(attr.DeviceTypeTitleLabels) : "";

        table.Rows.Add(row);
      }
    }

    public static string ResolveInventoryIdByDeviceNumber(
      RequestData samedisClient,
      string inventoryResource,
      string inventoryDeviceNumber,
      IDictionary<string, string> inventoryByDeviceNumber)
    {
      if (string.IsNullOrWhiteSpace(inventoryDeviceNumber))
        return string.Empty;

      var normalizedDeviceNumber = inventoryDeviceNumber.Trim();
      if (inventoryByDeviceNumber.TryGetValue(normalizedDeviceNumber, out var cachedInventoryId))
        return cachedInventoryId;

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("device_number", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, normalizedDeviceNumber);

      var requestResource = inventoryResource + $"?page[number]=1&page[limit]=1&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);

      if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300)
      {
        var resolvedInventoryId = Helper.ExtractDataId(response) ?? string.Empty;
        inventoryByDeviceNumber[normalizedDeviceNumber] = resolvedInventoryId;
        return resolvedInventoryId;
      }

      inventoryByDeviceNumber[normalizedDeviceNumber] = string.Empty;
      return string.Empty;
    }

    public static string ResolveIssueIdByIssueNumber(
      RequestData samedisClient,
      string issuesResource,
      string issueNumber,
      IDictionary<string, string> issueByIssueNumber)
    {
      if (string.IsNullOrWhiteSpace(issueNumber))
        return string.Empty;

      var normalizedIssueNumber = issueNumber.Trim();
      if (issueByIssueNumber.TryGetValue(normalizedIssueNumber, out var cachedIssueId))
        return cachedIssueId;

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      if (Helper.TryParseInt(normalizedIssueNumber, out var issueNumberAsInt))
        filterBuilder.Add("issue_number", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Number, issueNumberAsInt);
      else
        filterBuilder.Add("issue_number", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, normalizedIssueNumber);

      var requestResource = issuesResource + $"?page[number]=1&page[limit]=1&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);

      if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300)
      {
        var resolvedIssueId = Helper.ExtractDataId(response) ?? string.Empty;
        issueByIssueNumber[normalizedIssueNumber] = resolvedIssueId;
        return resolvedIssueId;
      }

      issueByIssueNumber[normalizedIssueNumber] = string.Empty;
      return string.Empty;
    }

    public static Dictionary<string, object>? BuildTaskAttributes(
      DataRow row,
      string inventoryId,
      bool setInventoryOperationStatusOnFailedMaintenance,
      out string errorMessage,
      out string warningMessage)
    {
      errorMessage = string.Empty;
      warningMessage = string.Empty;
      static void AppendWarning(ref string warningTarget, string message)
      {
        if (string.IsNullOrWhiteSpace(message))
          return;
        if (string.IsNullOrWhiteSpace(warningTarget))
        {
          warningTarget = message;
          return;
        }
        warningTarget += " " + message;
      }

      var attributes = new Dictionary<string, object>();

      if (string.IsNullOrWhiteSpace(inventoryId))
      {
        errorMessage = "inventory_id could not be resolved.";
        return null;
      }

      var issueTypeRaw = Helper.GetRowValue(row, "issue_type");
      var issueType = string.Empty;
      if (ContainsMaintenanceKeywords(issueTypeRaw))
      {
        issueType = "maintenance";
      }
      else if (!TryNormalizeIssueType(issueTypeRaw, out issueType))
      {
        errorMessage = $"Unsupported issue_type '{issueTypeRaw}'. Allowed: malfunction, maintenance, security_message, occurrence, device_retired, recommission_device.";
        return null;
      }

      var statusRaw = Helper.GetRowValue(row, "status");
      if (!TryNormalizeStatus(statusRaw, out var status))
      {
        errorMessage = $"Unsupported status '{statusRaw}'. Allowed: _new, pending, in_progress, done.";
        return null;
      }

      attributes["inventory_id"] = inventoryId;
      attributes["issue_type"] = issueType;
      attributes["status"] = status;
      Helper.AddStringAttribute(attributes, "external_id", Helper.GetRowValue(row, "issue_number"));
      var maintenanceType = Helper.GetRowValue(row, "maintenance_type");
      if (string.IsNullOrWhiteSpace(maintenanceType))
        maintenanceType = issueType;
      if (string.IsNullOrWhiteSpace(maintenanceType))
        maintenanceType = "maintenance";
      attributes["maintenance_type"] = maintenanceType;
      Helper.AddStringAttribute(attributes, "maintenance_performer", Helper.GetRowValue(row, "responsible_name"));
      var servicesRaw = Helper.GetRowValue(row, "services");
      if (string.IsNullOrWhiteSpace(servicesRaw))
        servicesRaw = string.IsNullOrWhiteSpace(issueTypeRaw) ? issueType : issueTypeRaw;
      attributes["services"] = ParseServicesArray(servicesRaw);

      Helper.AddStringAttribute(attributes, "title", Helper.GetRowValue(row, "title"));
      var normalizedDoneAt = NormalizeTaskDate(Helper.GetRowValue(row, "done_at"));
      if (string.IsNullOrWhiteSpace(normalizedDoneAt))
      {
        errorMessage = "done_at is required to set due_on and done_at.";
        return null;
      }
      Helper.AddStringAttribute(attributes, "date", normalizedDoneAt);
      Helper.AddStringAttribute(attributes, "done_at", normalizedDoneAt);
      Helper.AddStringAttribute(attributes, "responsible_name", Helper.GetRowValue(row, "responsible_name"));
      Helper.AddStringAttribute(attributes, "test_comment", Helper.GetRowValue(row, "test_comment"));
      var inventoryOperationStatus = NormalizeSimpleToken(Helper.GetRowValue(row, "inventory_operation_status"));
      if (!string.IsNullOrWhiteSpace(inventoryOperationStatus))
        attributes["inventory_operation_status"] = inventoryOperationStatus;

      var testResultRaw = Helper.GetRowValue(row, "test_result");
      var maintenancePassedRaw = Helper.GetRowValue(row, "maintenance_passed");
      var hasMaintenancePassed = Helper.TryParseBool(maintenancePassedRaw, out var maintenancePassedValue);

      if (!string.IsNullOrWhiteSpace(maintenancePassedRaw) && !hasMaintenancePassed)
      {
        errorMessage = $"Unsupported maintenance_passed '{maintenancePassedRaw}'. Use boolean values like true/false, yes/no, ja/nein, 1/0.";
        return null;
      }

      string normalizedTestResult = string.Empty;
      if (hasMaintenancePassed)
      {
        normalizedTestResult = maintenancePassedValue ? "passed" : "not_passed";

        if (!string.IsNullOrWhiteSpace(testResultRaw) &&
            TryNormalizeTestResult(testResultRaw, out var normalizedFromCsv) &&
            !string.Equals(normalizedFromCsv, normalizedTestResult, StringComparison.OrdinalIgnoreCase))
        {
          AppendWarning(
            ref warningMessage,
            $"test_result '{testResultRaw}' conflicts with maintenance_passed '{maintenancePassedRaw}'. Using maintenance_passed-derived test_result='{normalizedTestResult}'."
          );
        }
      }
      else if (!string.IsNullOrWhiteSpace(testResultRaw))
      {
        if (!TryNormalizeTestResult(testResultRaw, out normalizedTestResult))
        {
          errorMessage = $"Unsupported test_result '{testResultRaw}'. Allowed: passed, passed_conditionally, not_passed.";
          return null;
        }
      }

      if (!string.IsNullOrWhiteSpace(normalizedTestResult))
        attributes["test_result"] = normalizedTestResult;

      if (setInventoryOperationStatusOnFailedMaintenance &&
          string.Equals(issueType, "maintenance", StringComparison.OrdinalIgnoreCase) &&
          string.Equals(normalizedTestResult, "not_passed", StringComparison.OrdinalIgnoreCase) &&
          (!attributes.TryGetValue("inventory_operation_status", out var existingInventoryStatusObj) ||
           !string.Equals(existingInventoryStatusObj?.ToString(), "limited_use", StringComparison.OrdinalIgnoreCase)))
      {
        var previousInventoryStatus = existingInventoryStatusObj?.ToString() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(previousInventoryStatus))
        {
          AppendWarning(
            ref warningMessage,
            $"inventory_operation_status '{previousInventoryStatus}' overridden to 'limited_use' because maintenance result is not_passed and sync.tasks_upload_set_inventory_operation_status_on_failed_maintenance=true."
          );
        }
        attributes["inventory_operation_status"] = "limited_use";
      }

      return attributes;
    }

    public static string GetTaskDocumentFileName(DataRow row)
    {
      var candidateColumns = new[]
      {
        "document_filename",
        "filename",
        "file_name",
        "document_name",
        "dateiname"
      };

      foreach (var column in row.Table.Columns.Cast<DataColumn>())
      {
        for (var i = 0; i < candidateColumns.Length; i++)
        {
          if (!string.Equals(column.ColumnName, candidateColumns[i], StringComparison.OrdinalIgnoreCase))
            continue;

          var value = row[column];
          if (value == null || value == DBNull.Value)
            continue;

          var normalized = value.ToString()?.Trim() ?? string.Empty;
          if (!string.IsNullOrWhiteSpace(normalized))
            return normalized;
        }
      }

      return string.Empty;
    }

    public static string ResolveTaskDocumentPath(string uploadRoot, string fileNameFromCsv)
    {
      if (string.IsNullOrWhiteSpace(fileNameFromCsv))
        return string.Empty;

      var normalizedFileName = fileNameFromCsv.Trim().Trim('"', '\'');
      if (string.IsNullOrWhiteSpace(normalizedFileName))
        return string.Empty;

      if (Path.IsPathRooted(normalizedFileName) && File.Exists(normalizedFileName))
        return normalizedFileName;

      var candidateFileNames = new List<string> { normalizedFileName };
      if (string.IsNullOrWhiteSpace(Path.GetExtension(normalizedFileName)))
        candidateFileNames.Add(normalizedFileName + ".pdf");

      // Allow values like "task_documents/file.pdf" or "task_documents\\file.pdf".
      foreach (var candidate in candidateFileNames)
      {
        var normalizedCandidatePath = candidate.Replace('\\', '/');
        if (normalizedCandidatePath.StartsWith("task_documents/", StringComparison.OrdinalIgnoreCase))
        {
          var relativePath = normalizedCandidatePath["task_documents/".Length..];
          var explicitTaskDocumentsPath = Path.Combine(uploadRoot, "task_documents", relativePath);
          if (File.Exists(explicitTaskDocumentsPath))
            return explicitTaskDocumentsPath;
        }
      }

      var taskDocumentsDirectory = Path.Combine(uploadRoot, "task_documents");
      if (!Directory.Exists(taskDocumentsDirectory))
        return string.Empty;

      foreach (var candidate in candidateFileNames)
      {
        var path = Path.Combine(taskDocumentsDirectory, candidate);
        if (File.Exists(path))
          return path;
      }

      return string.Empty;
    }

    private static bool TryNormalizeIssueType(string value, out string normalizedIssueType)
    {
      var key = NormalizeKey(value);
      normalizedIssueType = key switch
      {
        "malfunction" => "malfunction",
        "stoerung" => "malfunction",
        "fehler" => "malfunction",
        "defekt" => "malfunction",

        "maintenance" => "maintenance",
        "wartung" => "maintenance",
        "pruefung" => "maintenance",
        "prufung" => "maintenance",
        "wartung_pruefung" => "maintenance",
        "wartung_prufung" => "maintenance",
        "wartung_und_pruefung" => "maintenance",
        "wartung_und_prufung" => "maintenance",

        "security_message" => "security_message",
        "securitymessage" => "security_message",
        "sicherheitsmeldung" => "security_message",
        "sicherheitswarnung" => "security_message",

        "occurrence" => "occurrence",
        "ereignis" => "occurrence",
        "vorkommnis" => "occurrence",
        "vorfall" => "occurrence",

        "device_retired" => "device_retired",
        "deviceretired" => "device_retired",
        "ausgemustert" => "device_retired",
        "stillgelegt" => "device_retired",
        "retired" => "device_retired",

        "recommission_device" => "recommission_device",
        "recommissiondevice" => "recommission_device",
        "recommission" => "recommission_device",
        "wiederinbetriebnahme" => "recommission_device",
        _ => string.Empty
      };

      return !string.IsNullOrWhiteSpace(normalizedIssueType);
    }

    private static bool TryNormalizeStatus(string value, out string normalizedStatus)
    {
      var key = NormalizeKey(value);
      normalizedStatus = key switch
      {
        "_new" => "_new",
        "new" => "_new",
        "neu" => "_new",

        "pending" => "pending",
        "offen" => "pending",
        "ausstehend" => "pending",
        "wartend" => "pending",

        "in_progress" => "in_progress",
        "inprogress" => "in_progress",
        "in_bearbeitung" => "in_progress",
        "inbearbeitung" => "in_progress",
        "bearbeitung" => "in_progress",
        "laufend" => "in_progress",

        "done" => "done",
        "abgeschlossen" => "done",
        "erledigt" => "done",
        "closed" => "done",
        "fertig" => "done",
        _ => string.Empty
      };

      return !string.IsNullOrWhiteSpace(normalizedStatus);
    }

    private static bool TryNormalizeTestResult(string value, out string normalizedTestResult)
    {
      var key = NormalizeKey(value);
      normalizedTestResult = key switch
      {
        "passed" => "passed",
        "bestanden" => "passed",
        "ok" => "passed",
        "io" => "passed",
        "i_o" => "passed",
        "true" => "passed",
        "ja" => "passed",

        "passed_conditionally" => "passed_conditionally",
        "passedconditionally" => "passed_conditionally",
        "bedingt_bestanden" => "passed_conditionally",
        "bedingt" => "passed_conditionally",
        "unter_vorbehalt_bestanden" => "passed_conditionally",

        "not_passed" => "not_passed",
        "notpassed" => "not_passed",
        "nicht_bestanden" => "not_passed",
        "failed" => "not_passed",
        "false" => "not_passed",
        "nein" => "not_passed",
        "nok" => "not_passed",
        _ => string.Empty
      };

      return !string.IsNullOrWhiteSpace(normalizedTestResult);
    }

    private static string NormalizeTaskDate(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

      var trimmed = value.Trim();
      var formats = new[]
      {
        "yyyy-MM-dd",
        "dd.MM.yyyy",
        "d.M.yyyy",
        "dd/MM/yyyy",
        "d/M/yyyy",
        "yyyy/MM/dd",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "dd.MM.yyyy HH:mm:ss",
        "d.M.yyyy H:m:s"
      };

      if (DateTime.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDate))
        return parsedDate.ToString("yyyy-MM-dd");

      if (DateTime.TryParseExact(trimmed, formats, DeCulture, DateTimeStyles.AssumeLocal, out parsedDate))
        return parsedDate.ToString("yyyy-MM-dd");

      if (DateTime.TryParse(trimmed, DeCulture, DateTimeStyles.AssumeLocal, out parsedDate))
        return parsedDate.ToString("yyyy-MM-dd");

      if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsedDate))
        return parsedDate.ToString("yyyy-MM-dd");

      return trimmed;
    }

    private static bool ContainsMaintenanceKeywords(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return false;

      var lower = value.Trim().ToLowerInvariant();
      return lower.Contains("wartung", StringComparison.Ordinal) ||
             lower.Contains("prüfung", StringComparison.Ordinal) ||
             lower.Contains("prufung", StringComparison.Ordinal) ||
             lower.Contains("pruefung", StringComparison.Ordinal);
    }

    private static string NormalizeSimpleToken(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

      return value.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
    }

    private static List<string> ParseServicesArray(string rawServices)
    {
      if (string.IsNullOrWhiteSpace(rawServices))
        return [];

      var normalized = rawServices.Trim();
      if (normalized.StartsWith("[") && normalized.EndsWith("]"))
      {
        try
        {
          var parsed = JsonConvert.DeserializeObject<List<string>>(normalized);
          if (parsed != null)
          {
            var filtered = parsed
              .Select(item => item?.Trim() ?? string.Empty)
              .Where(item => !string.IsNullOrWhiteSpace(item))
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .ToList();
            if (filtered.Count > 0)
              return filtered;
          }
        }
        catch
        {
          // Fallback to delimiter parsing below.
        }
      }

      return normalized
        .Split([',', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(item => item.Trim().Trim('"', '\''))
        .Where(item => !string.IsNullOrWhiteSpace(item))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .DefaultIfEmpty(normalized)
        .ToList();
    }

    private static string NormalizeKey(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

      var normalized = value.Trim().ToLowerInvariant()
        .Replace("ä", "ae")
        .Replace("ö", "oe")
        .Replace("ü", "ue")
        .Replace("ß", "ss");

      var sb = new StringBuilder(normalized.Length);
      var previousWasUnderscore = false;
      foreach (var ch in normalized)
      {
        if (char.IsLetterOrDigit(ch))
        {
          sb.Append(ch);
          previousWasUnderscore = false;
        }
        else if (!previousWasUnderscore)
        {
          sb.Append('_');
          previousWasUnderscore = true;
        }
      }

      return sb.ToString().Trim('_');
    }
  }
}
