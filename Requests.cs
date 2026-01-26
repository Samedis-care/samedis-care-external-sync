using System.Data;
using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Requests
  {
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
  }
}
