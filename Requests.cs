using System.Data;
using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Requests
  {
    public static readonly string[] UploadRequiredColumns = ["id", "incident_number"];

    public static readonly string[] MessageUploadRequiredColumns = ["id", "incident_id", "incident_number", "content"];

    private static readonly HashSet<string> AllowedStatusValues = new(StringComparer.OrdinalIgnoreCase)
    {
      "new",
      "pending",
      "in_progress",
      "done"
    };

    public class RequestMessages
    {
      public class Attributes
      {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("content")]
        public string? Content { get; set; }

        [JsonProperty("created_at")]
        public string? CreatedAt { get; set; }

        [JsonProperty("created_by")]
        public string? CreatedBy { get; set; }

        [JsonProperty("created_by_user")]
        public string? CreatedByUser { get; set; }

        [JsonProperty("deleted")]
        public bool? Deleted { get; set; }

        [JsonProperty("has_uploads")]
        public bool? HasUploads { get; set; }

        [JsonProperty("incident_id")]
        public string? IncidentId { get; set; }

        [JsonProperty("system")]
        public bool? System { get; set; }

        [JsonProperty("updated_at")]
        public string? UpdatedAt { get; set; }

        [JsonProperty("updated_by")]
        public string? UpdatedBy { get; set; }

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
    }

    public class RequestUploads
    {
      public class Attributes
      {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("created_at")]
        public string? CreatedAt { get; set; }

        [JsonProperty("created_by_user")]
        public string? CreatedByUser { get; set; }

        [JsonProperty("incident_id")]
        public string? IncidentId { get; set; }

        [JsonProperty("message_id")]
        public string? MessageId { get; set; }

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

      [JsonProperty("content")]
      public string? Content { get; set; }

      [JsonProperty("created_at")]
      public string? CreatedAt { get; set; }

      [JsonProperty("created_by")]
      public string? CreatedBy { get; set; }

      [JsonProperty("created_by_user")]
      public string? CreatedByUser { get; set; }

      [JsonProperty("device_model_current_responsible_manufacturer")]
      public string? DeviceModelCurrentResponsibleManufacturer { get; set; }

      [JsonProperty("device_model_id")]
      public string? DeviceModelId { get; set; }

      [JsonProperty("device_model_manufacturer_according_to_type_plate")]
      public string? DeviceModelManufacturerAccordingToTypePlate { get; set; }

      [JsonProperty("device_model_operator_ordinance")]
      public string? DeviceModelOperatorOrdinance { get; set; }

      [JsonProperty("device_model_risk_level")]
      public string? DeviceModelRiskLevel { get; set; }

      [JsonProperty("device_model_title")]
      public string? DeviceModelTitle { get; set; }

      [JsonProperty("device_model_version")]
      public string? DeviceModelVersion { get; set; }

      [JsonProperty("device_model_version_number")]
      public int? DeviceModelVersionNumber { get; set; }

      [JsonProperty("device_type_id")]
      public string? DeviceTypeId { get; set; }

      [JsonProperty("device_type_title")]
      public string? DeviceTypeTitle { get; set; }

      [JsonProperty("device_type_title_labels")]
      public Dictionary<string, string>? DeviceTypeTitleLabels { get; set; }

      [JsonProperty("external_id")]
      public string? ExternalId { get; set; }

      [JsonProperty("has_uploads")]
      public bool? HasUploads { get; set; }

      [JsonProperty("incident_number")]
      public int? IncidentNumber { get; set; }

      [JsonProperty("inventory_device_location_id")]
      public string? InventoryDeviceLocationId { get; set; }

      [JsonProperty("inventory_device_location_title")]
      public string? InventoryDeviceLocationTitle { get; set; }

      [JsonProperty("inventory_device_number")]
      public string? InventoryDeviceNumber { get; set; }

      [JsonProperty("inventory_device_picture")]
      public string? InventoryDevicePicture { get; set; }

      [JsonProperty("inventory_id")]
      public string? InventoryId { get; set; }

      [JsonProperty("inventory_operation_status")]
      public string? InventoryOperationStatus { get; set; }

      [JsonProperty("last_activity_at")]
      public string? LastActivityAt { get; set; }

      [JsonProperty("last_activity_by")]
      public string? LastActivityBy { get; set; }

      [JsonProperty("last_activity_by_user")]
      public string? LastActivityByUser { get; set; }

      [JsonProperty("needs_transport")]
      public bool? NeedsTransport { get; set; }

      [JsonProperty("responsible_id")]
      public string? ResponsibleId { get; set; }

      [JsonProperty("responsible_name")]
      public string? ResponsibleName { get; set; }

      [JsonProperty("responsible_user_id")]
      public string? ResponsibleUserId { get; set; }

      [JsonProperty("status")]
      public string? Status { get; set; }

      [JsonProperty("updated_at")]
      public string? UpdatedAt { get; set; }

      [JsonProperty("updated_by")]
      public string? UpdatedBy { get; set; }

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

    public static DataSet CreateRequestDataSet()
    {
      var ds = new DataSet("Requests");
      var dt = new DataTable("Requests");

      dt.Columns.Add("id", typeof(string));
      dt.Columns.Add("external_id", typeof(string));
      dt.Columns.Add("content", typeof(string));
      dt.Columns.Add("status", typeof(string));
      dt.Columns.Add("incident_number", typeof(string));
      dt.Columns.Add("created_at", typeof(string));
      dt.Columns.Add("updated_at", typeof(string));
      dt.Columns.Add("created_by", typeof(string));
      dt.Columns.Add("updated_by", typeof(string));
      dt.Columns.Add("created_by_user", typeof(string));
      dt.Columns.Add("updated_by_user", typeof(string));
      dt.Columns.Add("updated_by_user_at", typeof(string));
      dt.Columns.Add("last_activity_at", typeof(string));
      dt.Columns.Add("last_activity_by", typeof(string));
      dt.Columns.Add("last_activity_by_user", typeof(string));
      dt.Columns.Add("tenant_id", typeof(string));
      dt.Columns.Add("device_model_id", typeof(string));
      dt.Columns.Add("device_model_title", typeof(string));
      dt.Columns.Add("device_model_version", typeof(string));
      dt.Columns.Add("device_model_version_number", typeof(string));
      dt.Columns.Add("device_model_current_responsible_manufacturer", typeof(string));
      dt.Columns.Add("device_model_manufacturer_according_to_type_plate", typeof(string));
      dt.Columns.Add("device_model_operator_ordinance", typeof(string));
      dt.Columns.Add("device_model_risk_level", typeof(string));
      dt.Columns.Add("device_type_id", typeof(string));
      dt.Columns.Add("device_type_title", typeof(string));
      dt.Columns.Add("device_type_title_labels", typeof(string));
      dt.Columns.Add("inventory_id", typeof(string));
      dt.Columns.Add("inventory_device_number", typeof(string));
      dt.Columns.Add("inventory_device_location_id", typeof(string));
      dt.Columns.Add("inventory_device_location_title", typeof(string));
      dt.Columns.Add("inventory_device_picture", typeof(string));
      dt.Columns.Add("inventory_operation_status", typeof(string));
      dt.Columns.Add("responsible_id", typeof(string));
      dt.Columns.Add("responsible_name", typeof(string));
      dt.Columns.Add("responsible_user_id", typeof(string));
      dt.Columns.Add("has_uploads", typeof(string));
      dt.Columns.Add("needs_transport", typeof(string));

      var idColumn = dt.Columns["Id"] ?? throw new InvalidOperationException("The 'Id' column was not found in the DataTable.");
      dt.PrimaryKey = [idColumn];

      ds.Tables.Add(dt);
      return ds;
    }

    public static void FillRequestDataSet(DataSet ds, string json)
    {
      var root = JsonConvert.DeserializeObject<Requests.Root>(json);
      if (root?.Data == null || root.Data.Count == 0)
        return;

      var table = ds.Tables["Requests"];
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
        row["content"] = attr.Content ?? "";
        row["status"] = attr.Status ?? "";
        row["incident_number"] = attr.IncidentNumber?.ToString() ?? "";
        row["created_at"] = attr.CreatedAt ?? "";
        row["updated_at"] = attr.UpdatedAt ?? "";
        row["created_by"] = attr.CreatedBy ?? "";
        row["updated_by"] = attr.UpdatedBy ?? "";
        row["created_by_user"] = attr.CreatedByUser ?? "";
        row["updated_by_user"] = attr.UpdatedByUser ?? "";
        row["updated_by_user_at"] = attr.UpdatedByUserAt ?? "";
        row["last_activity_at"] = attr.LastActivityAt ?? "";
        row["last_activity_by"] = attr.LastActivityBy ?? "";
        row["last_activity_by_user"] = attr.LastActivityByUser ?? "";
        row["tenant_id"] = attr.TenantId ?? "";
        row["device_model_id"] = attr.DeviceModelId ?? "";
        row["device_model_title"] = attr.DeviceModelTitle ?? "";
        row["device_model_version"] = attr.DeviceModelVersion ?? "";
        row["device_model_version_number"] = attr.DeviceModelVersionNumber?.ToString() ?? "";
        row["device_model_current_responsible_manufacturer"] = attr.DeviceModelCurrentResponsibleManufacturer ?? "";
        row["device_model_manufacturer_according_to_type_plate"] = attr.DeviceModelManufacturerAccordingToTypePlate ?? "";
        row["device_model_operator_ordinance"] = attr.DeviceModelOperatorOrdinance ?? "";
        row["device_model_risk_level"] = attr.DeviceModelRiskLevel ?? "";
        row["device_type_id"] = attr.DeviceTypeId ?? "";
        row["device_type_title"] = attr.DeviceTypeTitle ?? "";
        row["device_type_title_labels"] = attr.DeviceTypeTitleLabels != null ? JsonConvert.SerializeObject(attr.DeviceTypeTitleLabels) : "";
        row["inventory_id"] = attr.InventoryId ?? "";
        row["inventory_device_number"] = attr.InventoryDeviceNumber ?? "";
        row["inventory_device_location_id"] = attr.InventoryDeviceLocationId ?? "";
        row["inventory_device_location_title"] = attr.InventoryDeviceLocationTitle ?? "";
        row["inventory_device_picture"] = attr.InventoryDevicePicture ?? "";
        row["inventory_operation_status"] = attr.InventoryOperationStatus ?? "";
        row["responsible_id"] = attr.ResponsibleId ?? "";
        row["responsible_name"] = attr.ResponsibleName ?? "";
        row["responsible_user_id"] = attr.ResponsibleUserId ?? "";
        row["has_uploads"] = attr.HasUploads.HasValue ? (attr.HasUploads.Value ? "Yes" : "No") : "";
        row["needs_transport"] = attr.NeedsTransport.HasValue ? (attr.NeedsTransport.Value ? "Yes" : "No") : "";

        table.Rows.Add(row);
      }
    }

    public static DataSet CreateRequestMessageDataSet()
    {
      var ds = new DataSet("RequestMessages");
      var dt = new DataTable("RequestMessages");

      dt.Columns.Add("id", typeof(string));
      dt.Columns.Add("incident_id", typeof(string));
      dt.Columns.Add("incident_number", typeof(string));
      dt.Columns.Add("content", typeof(string));
      dt.Columns.Add("system", typeof(string));
      dt.Columns.Add("deleted", typeof(string));
      dt.Columns.Add("has_uploads", typeof(string));
      dt.Columns.Add("created_at", typeof(string));
      dt.Columns.Add("created_by", typeof(string));
      dt.Columns.Add("created_by_user", typeof(string));
      dt.Columns.Add("updated_at", typeof(string));
      dt.Columns.Add("updated_by", typeof(string));
      dt.Columns.Add("updated_by_user", typeof(string));
      dt.Columns.Add("updated_by_user_at", typeof(string));

      var idColumn = dt.Columns["id"] ?? throw new InvalidOperationException("The 'id' column was not found in the DataTable.");
      dt.PrimaryKey = [idColumn];

      ds.Tables.Add(dt);
      return ds;
    }

    public static void FillRequestMessageDataSet(DataSet ds, string json, string parentIncidentId, string? parentIncidentNumber = null)
    {
      var root = JsonConvert.DeserializeObject<RequestMessages.Root>(json);
      if (root?.Data == null || root.Data.Count == 0)
        return;

      var table = ds.Tables["RequestMessages"];
      if (table == null) return;

      foreach (var data in root.Data)
      {
        var attr = data.Attributes;
        if (attr == null) continue;

        var rowId = attr.Id ?? data.Id;
        if (string.IsNullOrEmpty(rowId)) continue;
        if (table.Rows.Contains(rowId))
          continue;

        var row = table.NewRow();

        row["id"] = rowId;
        row["incident_id"] = !string.IsNullOrEmpty(attr.IncidentId) ? attr.IncidentId : parentIncidentId;
        row["incident_number"] = parentIncidentNumber ?? "";
        row["content"] = attr.Content ?? "";
        row["system"] = attr.System.HasValue ? (attr.System.Value ? "Yes" : "No") : "";
        row["deleted"] = attr.Deleted.HasValue ? (attr.Deleted.Value ? "Yes" : "No") : "";
        row["has_uploads"] = attr.HasUploads.HasValue ? (attr.HasUploads.Value ? "Yes" : "No") : "";
        row["created_at"] = attr.CreatedAt ?? "";
        row["created_by"] = attr.CreatedBy ?? "";
        row["created_by_user"] = attr.CreatedByUser ?? "";
        row["updated_at"] = attr.UpdatedAt ?? "";
        row["updated_by"] = attr.UpdatedBy ?? "";
        row["updated_by_user"] = attr.UpdatedByUser ?? "";
        row["updated_by_user_at"] = attr.UpdatedByUserAt ?? "";

        table.Rows.Add(row);
      }
    }

    public static string ResolveIncidentIdByIncidentNumber(
      RequestData samedisClient,
      string incidentsResource,
      string incidentNumber,
      IDictionary<string, string> incidentByIncidentNumber)
    {
      if (string.IsNullOrWhiteSpace(incidentNumber))
        return string.Empty;

      var normalizedIncidentNumber = incidentNumber.Trim();
      if (incidentByIncidentNumber.TryGetValue(normalizedIncidentNumber, out var cachedIncidentId))
        return cachedIncidentId;

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      if (Helper.TryParseInt(normalizedIncidentNumber, out var incidentNumberAsInt))
        filterBuilder.Add("incident_number", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Number, incidentNumberAsInt);
      else
        filterBuilder.Add("incident_number", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, normalizedIncidentNumber);

      var requestResource = incidentsResource + $"?page[number]=1&page[limit]=1&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);

      if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300)
      {
        var resolvedIncidentId = Helper.ExtractDataId(response) ?? string.Empty;
        incidentByIncidentNumber[normalizedIncidentNumber] = resolvedIncidentId;
        return resolvedIncidentId;
      }

      incidentByIncidentNumber[normalizedIncidentNumber] = string.Empty;
      return string.Empty;
    }

    public static Dictionary<string, object>? BuildRequestUpdateAttributes(
      DataRow row,
      out string errorMessage,
      out string warningMessage)
    {
      errorMessage = string.Empty;
      warningMessage = string.Empty;

      var attributes = new Dictionary<string, object>();

      var statusRaw = Helper.GetRowValue(row, "status");
      if (!string.IsNullOrWhiteSpace(statusRaw))
      {
        var normalizedStatus = statusRaw.Trim().ToLowerInvariant();
        if (!AllowedStatusValues.Contains(normalizedStatus))
        {
          errorMessage = $"Unsupported status '{statusRaw}'. Allowed values: new, pending, in_progress, done.";
          return null;
        }
        attributes["status"] = normalizedStatus;
      }

      Helper.AddStringAttribute(attributes, "responsible_id", Helper.GetRowValue(row, "responsible_id"));
      Helper.AddStringAttribute(attributes, "external_id", Helper.GetRowValue(row, "external_id"));
      Helper.AddStringAttribute(attributes, "inventory_operation_status", Helper.GetRowValue(row, "inventory_operation_status"));

      var needsTransportRaw = Helper.GetRowValue(row, "needs_transport");
      if (!string.IsNullOrWhiteSpace(needsTransportRaw))
      {
        if (Helper.TryParseBool(needsTransportRaw, out var needsTransport))
        {
          attributes["needs_transport"] = needsTransport;
        }
        else
        {
          errorMessage = $"Unsupported needs_transport '{needsTransportRaw}'. Use boolean values like true/false, yes/no, ja/nein, 1/0.";
          return null;
        }
      }

      if (attributes.Count == 0)
        warningMessage = "No writable fields populated; nothing to update.";

      return attributes;
    }

    public static Dictionary<string, object>? BuildMessageCreateAttributes(
      DataRow row,
      out string errorMessage)
    {
      errorMessage = string.Empty;
      var content = Helper.GetRowValue(row, "content");
      if (string.IsNullOrWhiteSpace(content))
      {
        errorMessage = "content is empty.";
        return null;
      }

      var attributes = new Dictionary<string, object>
      {
        ["content"] = content
      };
      return attributes;
    }

    public static string ResolveRequestDocumentPath(string uploadRoot, string fileNameFromCsv)
    {
      if (string.IsNullOrWhiteSpace(fileNameFromCsv))
        return string.Empty;

      var trimmed = fileNameFromCsv.Trim();
      if (Path.IsPathRooted(trimmed))
        return File.Exists(trimmed) ? trimmed : string.Empty;

      var normalized = trimmed.Replace('\\', '/');
      if (normalized.StartsWith("request_documents/", StringComparison.OrdinalIgnoreCase))
      {
        var relativePath = normalized["request_documents/".Length..];
        if (!string.IsNullOrWhiteSpace(relativePath))
        {
          var explicitRequestDocumentsPath = Path.Combine(uploadRoot, "request_documents", relativePath);
          if (File.Exists(explicitRequestDocumentsPath))
            return explicitRequestDocumentsPath;
        }
      }

      var requestDocumentsDirectory = Path.Combine(uploadRoot, "request_documents");
      if (!Directory.Exists(requestDocumentsDirectory))
        return string.Empty;

      foreach (var candidate in new[] { trimmed, trimmed + ".pdf" })
      {
        var path = Path.Combine(requestDocumentsDirectory, candidate);
        if (File.Exists(path))
          return path;
      }

      return string.Empty;
    }
  }
}
