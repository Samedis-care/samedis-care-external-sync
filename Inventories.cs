using System.Data;
using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Inventories
  {
    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("created_at")]
      public DateTime? CreatedAt { get; set; }

      [JsonProperty("updated_at")]
      public DateTime? UpdatedAt { get; set; }

      [JsonProperty("created_by_user")]
      public string? CreatedByUser { get; set; }

      [JsonProperty("updated_by_user")]
      public string? UpdatedByUser { get; set; }

      [JsonProperty("external_id")]
      public string? ExternalId { get; set; }

      [JsonProperty("device_type_title")]
      public string? DeviceTypeTitle { get; set; }

      [JsonProperty("device_model_current_responsible_manufacturer")]
      public string? DeviceModelCurrentResponsibleManufacturer { get; set; }

      [JsonProperty("device_model_risk_level")]
      public string? DeviceModelRiskLevel { get; set; }

      [JsonProperty("device_model_notified_body_ce")]
      public string? DeviceModelNotifiedBodyCe { get; set; }

      [JsonProperty("device_model_operator_ordinance")]
      public string? DeviceModelOperatorOrdinance { get; set; }

      [JsonProperty("device_model_trust_level")]
      public string? DeviceModelTrustLevel { get; set; }

      [JsonProperty("device_model_version")]
      public string? DeviceModelVersion { get; set; }

      [JsonProperty("device_model_version_number")]
      public int? DeviceModelVersionNumber { get; set; }

      [JsonProperty("date_of_acquisition")]
      public string? DateOfAcquisition { get; set; }

      [JsonProperty("do_maintenance")]
      public bool DoMaintenance { get; set; }

      [JsonProperty("ownership")]
      public string? Ownership { get; set; }

      [JsonProperty("status")]
      public string? Status { get; set; }

      [JsonProperty("device_number")]
      public string? DeviceNumber { get; set; }

      [JsonProperty("serial_number")]
      public string? SerialNumber { get; set; }

      [JsonProperty("retirement_date")]
      public string? RetirementDate { get; set; }

      [JsonProperty("last_maintenance")]
      public string? LastMaintenance { get; set; }

      [JsonProperty("last_maintenance_at")]
      public string? LastMaintenanceAt { get; set; }

      [JsonProperty("last_maintenance_issue_id")]
      public string? LastMaintenanceIssueId { get; set; }

      [JsonProperty("next_maintenance")]
      public string? NextMaintenance { get; set; }

      [JsonProperty("next_maintenance_at")]
      public string? NextMaintenanceAt { get; set; }

      [JsonProperty("next_maintenance_issue_id")]
      public string? NextMaintenanceIssueId { get; set; }

      [JsonProperty("next_inspection_at")]
      public string? NextInspectionAt { get; set; }

      [JsonProperty("no_medical_device")]
      public bool NoMedicalDevice { get; set; }

      [JsonProperty("comments_field")]
      public string? CommentsField { get; set; }

      [JsonProperty("warranty_period")]
      public string? WarrantyPeriod { get; set; }

      [JsonProperty("has_warranty")]
      public bool HasWarranty { get; set; }

      [JsonProperty("construction_year")]
      public int? ConstructionYear { get; set; }

      [JsonProperty("device_retired")]
      public bool DeviceRetired { get; set; }

      [JsonProperty("inventory_found_at")]
      public string? InventoryFoundAt { get; set; }

      [JsonProperty("inventory_not_found_at")]
      public string? InventoryNotFoundAt { get; set; }

      [JsonProperty("device_location_title")]
      public string? DeviceLocationTitle { get; set; }

      [JsonProperty("device_location_id")]
      public string? DeviceLocationId { get; set; }

      [JsonProperty("device_location_path")]
      public string? DeviceLocationPath { get; set; }

      [JsonProperty("service_partner")]
      public string? ServicePartner { get; set; }

      [JsonProperty("department_title")]
      public string? DepartmentTitle { get; set; }

      [JsonProperty("department_id")]
      public string? DepartmentId { get; set; }

      [JsonProperty("profit_center_title")]
      public string? ProfitCenterTitle { get; set; }

      [JsonProperty("commissioning_at")]
      public string? CommissioningAt { get; set; }

      [JsonProperty("commissioning_through")]
      public string? CommissioningThrough { get; set; }

      [JsonProperty("device_nick_name")]
      public string? DeviceNickName { get; set; }

      [JsonProperty("manufacturer_system_number")]
      public string? ManufacturerSystemNumber { get; set; }

      [JsonProperty("main_inventory_number")]
      public string? MainInventoryNumber { get; set; }

      [JsonProperty("main_inventory_id")]
      public string? MainInventoryId { get; set; }

      [JsonProperty("device_condition")]
      public string? DeviceCondition { get; set; }

      [JsonProperty("asset_accounting_number")]
      public string? AssetAccountingNumber { get; set; }

      [JsonProperty("purchase_price")]
      public decimal? PurchasePrice { get; set; }

      [JsonProperty("purchase_price_in_cents")]
      public long? PurchasePriceInCents { get; set; }

      [JsonProperty("currency_code")]
      public string? CurrencyCode { get; set; }

      [JsonProperty("depreciation_in_years")]
      public int? DepreciationInYears { get; set; }

      [JsonProperty("depreciation_date")]
      public string? DepreciationDate { get; set; }

      [JsonProperty("software_version")]
      public string? SoftwareVersion { get; set; }

      [JsonProperty("operating_system")]
      public string? OperatingSystem { get; set; }

      [JsonProperty("network_connectivity")]
      public string? NetworkConnectivity { get; set; }

      [JsonProperty("ip_address")]
      public string? IpAddress { get; set; }

      [JsonProperty("mac_address")]
      public string? MacAddress { get; set; }

      [JsonProperty("accessible_usb_ports")]
      public bool AccessibleUsbPorts { get; set; }

      [JsonProperty("contains_patient_data")]
      public bool ContainsPatientData { get; set; }

      [JsonProperty("service_intervals")]
      public Dictionary<string, object>? ServiceIntervals { get; set; }

      [JsonProperty("issue_statistics")]
      public Dictionary<string, object>? IssueStatistics { get; set; }

      [JsonProperty("qr_code_resource_token")]
      public string? QrCodeResourceToken { get; set; }

      [JsonProperty("operation_status")]
      public string? OperationStatus { get; set; }

      [JsonProperty("is_device_system")]
      public bool IsDeviceSystem { get; set; }

      [JsonProperty("supplier_company_name")]
      public string? SupplierCompanyName { get; set; }

      [JsonProperty("lifespan")]
      public int? Lifespan { get; set; }

      [JsonProperty("delivered_at")]
      public string? DeliveredAt { get; set; }

      [JsonProperty("installed_at")]
      public string? InstalledAt { get; set; }

      [JsonProperty("tenant_id")]
      public string? TenantId { get; set; }

      [JsonProperty("tenant_name")]
      public string? TenantName { get; set; }

      [JsonProperty("catalog_id")]
      public string? CatalogId { get; set; }

      [JsonProperty("linked_image_id")]
      public string? LinkedImageId { get; set; }

      [JsonProperty("device_model_title")]
      public string? DeviceModelTitle { get; set; }

      [JsonProperty("device_model_manufacturer_according_to_type_plate")]
      public string? DeviceModelManufacturerAccordingToTypePlate { get; set; }

      [JsonProperty("device_type_title_labels")]
      public Dictionary<string, string>? DeviceTypeTitleLabels { get; set; }

      [JsonProperty("device_model_image")]
      public string? DeviceModelImage { get; set; }

      [JsonProperty("supplier_company_contact_id")]
      public string? SupplierCompanyContactId { get; set; }

      [JsonProperty("authority")]
      public Dictionary<string, object>? Authority { get; set; }

      [JsonProperty("regulatory")]
      public Dictionary<string, string>? Regulatory { get; set; }

      [JsonProperty("parent_device_model_combo_search")]
      public string? ParentDeviceModelComboSearch { get; set; }

      [JsonProperty("parent_device_model_title")]
      public string? ParentDeviceModelTitle { get; set; }

      [JsonProperty("parent_device_type_title")]
      public string? ParentDeviceTypeTitle { get; set; }

      [JsonProperty("parent_manufacturer_according_to_type_plate")]
      public string? ParentManufacturerAccordingToTypePlate { get; set; }

      [JsonProperty("placeholder_device_model_manufacturer")]
      public string? PlaceholderDeviceModelManufacturer { get; set; }

      [JsonProperty("placeholder_device_model_title")]
      public string? PlaceholderDeviceModelTitle { get; set; }

      [JsonProperty("placeholder_device_type_title")]
      public string? PlaceholderDeviceTypeTitle { get; set; }

      [JsonProperty("service_company_ids")]
      public List<string>? ServiceCompanyIds { get; set; }

      [JsonProperty("team_ids")]
      public List<string>? TeamIds { get; set; }

      [JsonProperty("updated_by_user_at")]
      public string? UpdatedByUserAt { get; set; }

      [JsonProperty("urn")]
      public string? Urn { get; set; }
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
      public Dictionary<string, object>? Links { get; set; }
    }

    public class Msg
    {
      [JsonProperty("success")]
      public bool Success { get; set; }

      [JsonProperty("message")]
      public string? Message { get; set; }
    }

    public class JsonApiOptions
    {
      [JsonProperty("limit")]
      public int? Limit { get; set; }

      [JsonProperty("page")]
      public int? Page { get; set; }

      [JsonProperty("padding")]
      public int Padding { get; set; }

      [JsonProperty("include")]
      public List<object>? Include { get; set; }

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

    public static DataSet CreateInventoryDataSet()
    {
      var ds = new DataSet("Inventories");
      var dt = new DataTable("Inventories");

      dt.Columns.Add("id", typeof(string));
      dt.Columns.Add("external_id", typeof(string));
      dt.Columns.Add("inventory_number", typeof(string));           // device_number
      dt.Columns.Add("serial_number", typeof(string));              // serial_number
      dt.Columns.Add("catalog_id", typeof(string));                 // device_model_catalog_id
      dt.Columns.Add("title", typeof(string));                      // device_model_title
      dt.Columns.Add("device_type_title", typeof(string));          // device_type_title
      dt.Columns.Add("manufacturer", typeof(string));               // device_model_manufacturer_according_to_type_plate
      dt.Columns.Add("responsible_manufacturer", typeof(string));   // device_model_current_responsible_manufacturer
      dt.Columns.Add("facility_name", typeof(string));              // tenant_name
      dt.Columns.Add("location_id", typeof(string));                // device_location_id
      dt.Columns.Add("location", typeof(string));                   // device_location_title
      dt.Columns.Add("department_id", typeof(string));              // department_id
      dt.Columns.Add("department", typeof(string));                 // device_location_path
      dt.Columns.Add("construction_year", typeof(string));
      dt.Columns.Add("commissioning_at", typeof(string));
      dt.Columns.Add("service_partner", typeof(string));
      dt.Columns.Add("comments_field", typeof(string));
      dt.Columns.Add("operation_status", typeof(string));
      dt.Columns.Add("last_maintenance", typeof(string));
      dt.Columns.Add("next_maintenance", typeof(string));
      dt.Columns.Add("ce_marking", typeof(string));                 // regulatory.ce
      dt.Columns.Add("ce_notified_body", typeof(string));           // device_model_notified_body_ce
      dt.Columns.Add("according_to_annex", typeof(string));         // device_model_operator_ordinance
      dt.Columns.Add("risk_level", typeof(string));                 // device_model_risk_level
      dt.Columns.Add("purchase_price", typeof(string));
      dt.Columns.Add("depreciation_in_years", typeof(string));
      dt.Columns.Add("retirement_date", typeof(string));

      // ✅ Primary key = Id
      var idColumn = dt.Columns["Id"] ?? throw new InvalidOperationException("The 'Id' column was not found in the DataTable.");
      dt.PrimaryKey = [idColumn];

      ds.Tables.Add(dt);
      return ds;
    }

    public static void FillInventoryDataSet(DataSet ds, string json)
    {
      var root = JsonConvert.DeserializeObject<Inventories.Root>(json);
      if (root?.Data == null || root.Data.Count == 0)
        return;

      var table = ds.Tables["Inventories"];
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
        row["inventory_number"] = attr.DeviceNumber ?? "";
        row["serial_number"] = attr.SerialNumber ?? "";
        row["catalog_id"] = attr.CatalogId ?? "";
        row["title"] = attr.DeviceModelTitle ?? "";
        row["device_type_title"] = attr.DeviceTypeTitle ?? "";
        row["manufacturer"] = attr.DeviceModelManufacturerAccordingToTypePlate ?? "";
        row["responsible_manufacturer"] = attr.DeviceModelCurrentResponsibleManufacturer ?? "";
        row["facility_name"] = attr.TenantName ?? "";
        row["location_id"] = attr.DeviceLocationId ?? "";
        row["location"] = attr.DeviceLocationTitle ?? "";
        row["department_id"] = attr.DepartmentId ?? "";
        row["department"] = attr.DepartmentTitle ?? "";
        row["construction_year"] = attr.ConstructionYear?.ToString() ?? "";
        row["commissioning_at"] = attr.CommissioningAt ?? "";
        row["service_partner"] = attr.ServicePartner ?? "";
        row["comments_field"] = attr.CommentsField ?? "";
        row["operation_status"] = attr.OperationStatus ?? "";
        row["last_maintenance"] = attr.LastMaintenance ?? "";
        row["next_maintenance"] = attr.NextMaintenance ?? "";
        row["ce_marking"] = (attr.Regulatory != null && attr.Regulatory.ContainsKey("ce")) ? "Yes" : "No";
        row["ce_notified_body"] = attr.DeviceModelNotifiedBodyCe ?? "";
        row["according_to_annex"] = Helper.OrdinanceMap(attr.DeviceModelOperatorOrdinance ?? string.Empty);
        row["risk_level"] = Helper.RiskClassMap(attr.DeviceModelRiskLevel ?? string.Empty);
        row["purchase_price"] = attr.PurchasePrice?.ToString("F2") ?? "";
        row["depreciation_in_years"] = attr.DepreciationInYears?.ToString() ?? "";
        row["retirement_date"] = attr.RetirementDate ?? "";

        table.Rows.Add(row);
      }
    }

  }
}
