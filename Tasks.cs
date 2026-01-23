using System.Data;
using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Tasks
  {
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
  }
}
