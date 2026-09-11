using System.Data;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
      public string? Status { get; set; } = "created";

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
      [JsonConverter(typeof(JsonApi.SingleOrArrayConverter<Data>))]
      public List<Data>? Data { get; set; }

      [JsonProperty("meta")]
      public Meta? Meta { get; set; }
    }

    public class SourceLocationExportInfo
    {
      public string SourceLocationId { get; set; } = string.Empty;
      public string SourceLocationType { get; set; } = string.Empty;
      public string SourceLocationNumber { get; set; } = string.Empty;
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
      dt.Columns.Add("device_model_title", typeof(string));         // device_model_title
      dt.Columns.Add("device_type_title", typeof(string));          // device_type_title
      dt.Columns.Add("manufacturer", typeof(string));               // device_model_manufacturer_according_to_type_plate
      dt.Columns.Add("responsible_manufacturer", typeof(string));   // device_model_current_responsible_manufacturer
      dt.Columns.Add("facility_name", typeof(string));              // tenant_name
      dt.Columns.Add("location_id", typeof(string));                // device_location_id
      dt.Columns.Add("location", typeof(string));                   // device_location_title
      dt.Columns.Add("additional_location_info", typeof(string));   // device_location_path
      dt.Columns.Add("department_id", typeof(string));              // department_id
      dt.Columns.Add("department", typeof(string));                 // device_location_path
      dt.Columns.Add("department_station", typeof(string));         // upload compatibility
      dt.Columns.Add("cost_center_number", typeof(string));         // upload compatibility
      dt.Columns.Add("cost_center_description", typeof(string));    // upload compatibility
      dt.Columns.Add("construction_year", typeof(string));
      dt.Columns.Add("commissioning_at", typeof(string));
      dt.Columns.Add("service_partner", typeof(string));
      dt.Columns.Add("comments_field", typeof(string));
      dt.Columns.Add("description", typeof(string));                // upload compatibility
      dt.Columns.Add("operation_status", typeof(string));
      dt.Columns.Add("last_maintenance", typeof(string));
      dt.Columns.Add("next_maintenance", typeof(string));
      dt.Columns.Add("purchase_price", typeof(string));
      dt.Columns.Add("currency_code", typeof(string));
      dt.Columns.Add("depreciation_in_years", typeof(string));
      dt.Columns.Add("retirement_date", typeof(string));
      dt.Columns.Add("date_of_acquisition", typeof(string));
      dt.Columns.Add("warranty_period", typeof(string));
      dt.Columns.Add("ownership", typeof(string));
      dt.Columns.Add("source_location_number", typeof(string));
      dt.Columns.Add("source_location_type", typeof(string));
      dt.Columns.Add("source_location_id", typeof(string));
      dt.Columns.Add("software_version", typeof(string));
      dt.Columns.Add("changed_at", typeof(string));
      dt.Columns.Add("created_at", typeof(string));

      // ✅ Primary key = Id
      var idColumn = dt.Columns["Id"] ?? throw new InvalidOperationException("The 'Id' column was not found in the DataTable.");
      dt.PrimaryKey = [idColumn];

      ds.Tables.Add(dt);
      return ds;
    }

    public static void FillInventoryDataSet(
      DataSet ds,
      string json,
      NumberFormat numbers,
      Func<Attributes, SourceLocationExportInfo?>? sourceLocationResolver = null)
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
        row["device_model_title"] = attr.DeviceModelTitle ?? "";
        row["device_type_title"] = attr.DeviceTypeTitle ?? "";
        row["manufacturer"] = attr.DeviceModelManufacturerAccordingToTypePlate ?? "";
        row["responsible_manufacturer"] = attr.DeviceModelCurrentResponsibleManufacturer ?? "";
        row["facility_name"] = attr.TenantName ?? "";
        row["location_id"] = attr.DeviceLocationId ?? "";
        row["location"] = attr.DeviceLocationTitle ?? "";
        row["additional_location_info"] = attr.DeviceLocationPath ?? "";
        row["department_id"] = attr.DepartmentId ?? "";
        row["department"] = attr.DepartmentTitle ?? "";
        row["department_station"] = attr.DepartmentTitle ?? "";
        row["cost_center_number"] = "";
        row["cost_center_description"] = "";
        row["construction_year"] = attr.ConstructionYear?.ToString() ?? "";
        row["commissioning_at"] = attr.CommissioningAt ?? "";
        row["service_partner"] = attr.ServicePartner ?? "";
        row["comments_field"] = attr.CommentsField ?? "";
        row["description"] = attr.CommentsField ?? "";
        row["operation_status"] = attr.OperationStatus ?? "";
        row["last_maintenance"] = attr.LastMaintenance ?? "";
        row["next_maintenance"] = attr.NextMaintenance ?? "";
        row["purchase_price"] = attr.PurchasePrice.HasValue ? numbers.Format(attr.PurchasePrice.Value) : "";
        row["currency_code"] = attr.CurrencyCode ?? "";
        row["depreciation_in_years"] = attr.DepreciationInYears?.ToString() ?? "";
        row["retirement_date"] = attr.RetirementDate ?? "";
        row["date_of_acquisition"] = attr.DateOfAcquisition ?? "";
        row["warranty_period"] = attr.WarrantyPeriod ?? "";
        row["ownership"] = attr.Ownership ?? "";
        row["software_version"] = attr.SoftwareVersion ?? "";
        row["changed_at"] = attr.UpdatedAt?.ToString("o", CultureInfo.InvariantCulture) ?? "";
        row["created_at"] = attr.CreatedAt?.ToString("o", CultureInfo.InvariantCulture) ?? "";

        var sourceLocation = sourceLocationResolver?.Invoke(attr);
        row["source_location_number"] = sourceLocation?.SourceLocationNumber ?? "";
        row["source_location_type"] = sourceLocation?.SourceLocationType ?? "";
        row["source_location_id"] = sourceLocation?.SourceLocationId ?? "";

        table.Rows.Add(row);
      }
    }
    /// <summary>
    /// Resolves the inventory a source row refers to: samedis id, then external_id, then
    /// inventory number.
    /// </summary>
    /// <param name="lookup">
    /// Bound to the inventories collection. It remembers hits and misses, which is what the
    /// three dictionaries and three "already checked" sets this replaces did by hand.
    /// </param>
    /// <param name="inventoryId">A samedis inventory id, if the row carries one.</param>
    /// <param name="inventoryExternalId">The source system's own key for the device.</param>
    /// <param name="inventoryNumber">The facility's inventory number.</param>
    /// <param name="fallbackByDeviceNumber">
    /// Whether the inventory number may be used at all. Off for callers that must not match a
    /// record they were not given a stable key for.
    /// </param>
    /// <remarks>
    /// A samedis-id or external_id match is authoritative even when the row's inventory
    /// number differs: external_id is the stable cross-system anchor and the source may
    /// deliver a changed inventory number for the same device (the update reconciles the
    /// number). Falling through to the device-number lookup instead would pick a DIFFERENT
    /// record, and the update would then try to move the row's external_id onto it -- which
    /// the collation-insensitive unique index on (tenant_id, external_id) rejects as a
    /// duplicate key. Cascades.Inventory stops at the first hit for exactly that reason.
    /// <para>
    /// The version this replaces also fetched each resolved record's detail in order to seed
    /// the device-number cache. That is dropped: it cost one extra request per resolved id to
    /// save at most one per inventory number, and the lookup caches both kinds anyway.
    /// </para>
    /// </remarks>
    public static string? ResolveExistingInventoryId(
      ResourceLookup lookup,
      string inventoryId,
      string inventoryExternalId,
      string inventoryNumber,
      bool fallbackByDeviceNumber)
      => Cascades.Inventory(lookup, inventoryId, inventoryExternalId, inventoryNumber,
                            query: "variant=regular",
                            deviceNumberFallback: fallbackByDeviceNumber);

    /// <summary>
    /// Resolves an inventory by its inventory number (device_number).
    /// </summary>
    /// <remarks>
    /// <c>variant=regular</c> asks for the smaller serializer variant; only the id is read.
    /// The lookup caches hits and misses, which is what the dictionary this replaces did.
    /// </remarks>
    public static string ResolveInventoryIdByDeviceNumber(ResourceLookup lookup, string deviceNumber)
      => lookup.ByField("device_number", deviceNumber,
                        FilterBuilder.FilterType.Equals, "variant=regular") ?? string.Empty;

    /// <summary>
    /// Resolves an inventory from a samedis id when the row carries one, otherwise from its
    /// inventory number. Used by the requests upload.
    /// </summary>
    /// <remarks>
    /// The version this replaces returned the source's id unchecked. That is the failure the
    /// rest of this migration removes: a value that is not an id, or names a record this
    /// tenant cannot read, was passed on as if it had been resolved, and only failed later on
    /// the write. Going through the cascade verifies it and otherwise falls through to the
    /// inventory number, which is what the caller wanted in the first place.
    /// </remarks>
    public static string ResolveInventoryIdByIdOrDeviceNumber(ResourceLookup lookup,
                                                              string inventoryId,
                                                              string deviceNumber)
      => Cascades.Inventory(lookup, inventoryId, null, deviceNumber, query: "variant=regular")
         ?? string.Empty;

    public static Dictionary<string, object> BuildInventoryAttributes(
      DataRow row,
      string? departmentId,
      string? locationId,
      NumberFormat numbers,
      string? catalogIdOverride = null,
      bool applyCreateDefaults = false)
    {
      var attributes = new Dictionary<string, object>();
      var modelTitle = Rows.Value(row, "device_model_title");
      if (string.IsNullOrWhiteSpace(modelTitle))
        modelTitle = Rows.Value(row, "title");

      var manufacturer = Rows.Value(row, "manufacturer");
      if (string.IsNullOrWhiteSpace(manufacturer))
        manufacturer = Rows.Value(row, "responsible_manufacturer");
      if (string.IsNullOrWhiteSpace(manufacturer))
        manufacturer = Rows.Value(row, "company");

      var deviceTypeTitle = Rows.Value(row, "device_type_title");
      var placeholderManufacturer = Rows.Value(row, "placeholder_device_model_manufacturer");
      var placeholderModelTitle = Rows.Value(row, "placeholder_device_model_title");
      var placeholderDeviceTypeTitle = Rows.Value(row, "placeholder_device_type_title");
      var isPlaceholder = IsPlaceholderDeviceModel(row);

      if (isPlaceholder)
      {
        if (string.IsNullOrWhiteSpace(placeholderManufacturer))
          placeholderManufacturer = manufacturer;
        if (string.IsNullOrWhiteSpace(placeholderModelTitle))
          placeholderModelTitle = modelTitle;
        if (string.IsNullOrWhiteSpace(placeholderDeviceTypeTitle))
          placeholderDeviceTypeTitle = deviceTypeTitle;
      }

      JsonApi.AddStringAttribute(attributes, "external_id", Rows.Value(row, "external_id"));
      JsonApi.AddStringAttribute(attributes, "device_number", Rows.Value(row, "inventory_number"));
      JsonApi.AddStringAttribute(attributes, "serial_number", Rows.Value(row, "serial_number"));
      var catalogId = string.IsNullOrWhiteSpace(catalogIdOverride) ? Rows.Value(row, "catalog_id") : catalogIdOverride;
      JsonApi.AddStringAttribute(attributes, "catalog_id", catalogId);
      JsonApi.AddStringAttribute(attributes, "commissioning_at", Helper.NormalizeDate(Rows.Value(row, "commissioning_at")));
      JsonApi.AddStringAttribute(attributes, "service_partner", Rows.Value(row, "service_partner"));
      JsonApi.AddStringAttribute(attributes, "comments_field", Rows.Value(row, "comments_field"));
      JsonApi.AddStringAttribute(attributes, "operation_status", NormalizeOperationStatus(Rows.Value(row, "operation_status")));
      // retirement_date is deliberately NOT part of the payload. The backend derives the
      // retirement flag from it on every save (Inventory#update_reference_columns:
      // `self.device_retired = retirement_date.present?`, followed by
      // set_operation_status_on_retire forcing operation_status='retired'). Sending the
      // CSV value therefore RE-RETIRES the device on every successful update -- including
      // the retry right after a recommission -- which is what produced the endless
      // active <-> retired oscillation on the UKT tenant. Retirement is expressed
      // exclusively through device_retired / recommission_device issues; the CSV value is
      // only used as the issue date (see PostDeviceRetiredIssue callers).
      JsonApi.AddStringAttribute(attributes, "status", NormalizeStatus(Rows.Value(row, "status")));
      JsonApi.AddStringAttribute(attributes, "ownership", NormalizeOwnership(Rows.Value(row, "ownership")));
      JsonApi.AddStringAttribute(attributes, "currency_code", NormalizeCurrency(Rows.Value(row, "currency_code")));
      JsonApi.AddStringAttribute(attributes, "date_of_acquisition", Helper.NormalizeDate(Rows.Value(row, "date_of_acquisition")));
      JsonApi.AddStringAttribute(attributes, "delivered_at", Helper.NormalizeDate(Rows.Value(row, "delivered_at")));
      JsonApi.AddStringAttribute(attributes, "installed_at", Helper.NormalizeDate(Rows.Value(row, "installed_at")));
      JsonApi.AddStringAttribute(attributes, "warranty_period", Helper.NormalizeDate(Rows.Value(row, "warranty_period")));
      JsonApi.AddStringAttribute(attributes, "asset_accounting_number", Rows.Value(row, "asset_accounting_number"));
      JsonApi.AddStringAttribute(attributes, "device_condition", Rows.Value(row, "device_condition"));
      JsonApi.AddStringAttribute(attributes, "device_nick_name", Rows.Value(row, "device_nick_name"));
      JsonApi.AddStringAttribute(attributes, "manufacturer_system_number", Rows.Value(row, "manufacturer_system_number"));
      JsonApi.AddStringAttribute(attributes, "network_connectivity", Rows.Value(row, "network_connectivity"));
      JsonApi.AddStringAttribute(attributes, "operating_system", Rows.Value(row, "operating_system"));
      JsonApi.AddStringAttribute(attributes, "software_version", Rows.Value(row, "software_version"));
      JsonApi.AddStringAttribute(attributes, "ip_address", Rows.Value(row, "ip_address"));
      JsonApi.AddStringAttribute(attributes, "mac_address", Rows.Value(row, "mac_address"));
      var qrCodeToken = Rows.Value(row, "qr_code_token");
      if (string.IsNullOrWhiteSpace(qrCodeToken))
        qrCodeToken = Rows.Value(row, "qr_code_resource_token");
      JsonApi.AddStringAttribute(attributes, "qr_code_token", qrCodeToken);
      JsonApi.AddStringAttribute(attributes, "commissioning_through", Rows.Value(row, "commissioning_through"));
      JsonApi.AddStringAttribute(attributes, "linked_image_id", Rows.Value(row, "linked_image_id"));
      JsonApi.AddStringAttribute(attributes, "main_inventory_id", Rows.Value(row, "main_inventory_id"));
      JsonApi.AddStringAttribute(attributes, "main_inventory_number", Rows.Value(row, "main_inventory_number"));
      JsonApi.AddStringAttribute(attributes, "supplier_company_contact_id", Rows.Value(row, "supplier_company_contact_id"));
      JsonApi.AddStringAttribute(attributes, "supplier_company_name", Rows.Value(row, "supplier_company_name"));
      JsonApi.AddStringAttribute(attributes, "placeholder_device_model_manufacturer", placeholderManufacturer);
      JsonApi.AddStringAttribute(attributes, "placeholder_device_model_title", placeholderModelTitle);
      JsonApi.AddStringAttribute(attributes, "placeholder_device_type_title", placeholderDeviceTypeTitle);
      JsonApi.AddStringAttribute(attributes, "variant", Rows.Value(row, "variant"));
      JsonApi.AddStringAttribute(attributes, "type_plate", Rows.Value(row, "type_plate"));
      JsonApi.AddStringAttribute(attributes, "type_plate_data_uri", Rows.Value(row, "type_plate_data_uri"));
      JsonApi.AddStringAttribute(attributes, "depreciation_date", Helper.NormalizeDate(Rows.Value(row, "depreciation_date")));

      // API docs define construction_year as string.
      JsonApi.AddStringAttribute(attributes, "construction_year", Rows.Value(row, "construction_year"));

      if (Strings.TryParseInt(Rows.Value(row, "depreciation_in_years"), out var depreciationInYears))
        attributes["depreciation_in_years"] = depreciationInYears;

      if (Strings.TryParseInt(Rows.Value(row, "lifespan"), out var lifespan))
        attributes["lifespan"] = lifespan;

      if (numbers.TryParseDecimal(Rows.Value(row, "purchase_price"), out var purchasePrice))
        attributes["purchase_price"] = purchasePrice;

      if (Strings.TryParseLong(Rows.Value(row, "purchase_price_in_cents"), out var purchasePriceInCents))
        attributes["purchase_price_in_cents"] = purchasePriceInCents;

      if (Strings.TryParseBool(Rows.Value(row, "accessible_usb_ports"), out var accessibleUsbPorts))
        attributes["accessible_usb_ports"] = accessibleUsbPorts;

      if (Strings.TryParseBool(Rows.Value(row, "contains_patient_data"), out var containsPatientData))
        attributes["contains_patient_data"] = containsPatientData;

      if (applyCreateDefaults)
        attributes["do_maintenance"] = true;
      if (Strings.TryParseBool(Rows.Value(row, "do_maintenance"), out var doMaintenance))
        attributes["do_maintenance"] = doMaintenance;

      if (applyCreateDefaults)
        attributes["no_medical_device"] = false;
      if (Strings.TryParseBool(Rows.Value(row, "no_medical_device"), out var noMedicalDevice))
        attributes["no_medical_device"] = noMedicalDevice;

      if (isPlaceholder)
      {
        attributes["device_model_is_placeholder"] = true;
      }
      else if (Strings.TryParseBool(Rows.Value(row, "device_model_is_placeholder"), out var isPlaceholderValue))
      {
        attributes["device_model_is_placeholder"] = isPlaceholderValue;
      }

      if (Strings.TryParseBool(Rows.Value(row, "has_warranty"), out var hasWarranty))
        attributes["has_warranty"] = hasWarranty;

      if (Strings.TryParseBool(Rows.Value(row, "is_device_system"), out var isDeviceSystem))
        attributes["is_device_system"] = isDeviceSystem;

      var serviceCompanyIds = ParseStringList(Rows.Value(row, "service_company_ids"));
      if (serviceCompanyIds.Count > 0)
        attributes["service_company_ids"] = serviceCompanyIds;

      var teamIds = ParseStringList(Rows.Value(row, "team_ids"));
      if (teamIds.Count > 0)
        attributes["team_ids"] = teamIds;

      var withServiceIntervals = ParseJsonValue(Rows.Value(row, "with_service_intervals"));
      if (withServiceIntervals != null)
        attributes["with_service_intervals"] = withServiceIntervals;

      var nics = ParseJsonValue(Rows.Value(row, "nics"));
      if (nics != null)
        attributes["nics"] = nics;

      var takeAuthority = BuildTakeAuthority(row);
      if (takeAuthority != null)
        attributes["take_authority"] = takeAuthority;

      if (!string.IsNullOrWhiteSpace(departmentId))
        attributes["department_id"] = departmentId;

      if (!string.IsNullOrWhiteSpace(locationId))
        attributes["device_location_id"] = locationId;

      // Device-model-only fields must never be sent via inventory import payload.
      attributes.Remove("ce_marking");
      attributes.Remove("ce_notified_body");
      attributes.Remove("according_to_annex");
      attributes.Remove("risk_level");
      attributes.Remove("last_maintenance");
      attributes.Remove("next_maintenance");

      return attributes;
    }

    public static bool IsPlaceholderDeviceModel(DataRow row)
    {
      return Strings.TryParseBool(Rows.Value(row, "device_model_is_placeholder"), out var isPlaceholder) && isPlaceholder;
    }

    private static List<string> ParseStringList(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return new List<string>();

      var normalized = value.Trim();
      if (normalized.StartsWith("[") && normalized.EndsWith("]"))
        normalized = normalized[1..^1];

      return normalized
        .Split([',', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(item => item.Trim().Trim('"', '\''))
        .Where(item => !string.IsNullOrWhiteSpace(item))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    private static object? ParseJsonValue(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return null;

      try
      {
        return JsonConvert.DeserializeObject<object>(value);
      }
      catch
      {
        return null;
      }
    }

    private static object? BuildTakeAuthority(DataRow row)
    {
      var authority = new Dictionary<string, object>();
      var hasAuthorityValues = false;

      static bool TryParseBoolToken(JToken token, out bool value)
      {
        if (token.Type == JTokenType.Boolean)
        {
          value = token.Value<bool>();
          return true;
        }

        return Strings.TryParseBool(token.ToString(), out value);
      }

      static List<string> ParseProtectedFieldsToken(JToken token)
      {
        if (token.Type == JTokenType.Array)
        {
          var values = token.Values<string>()
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
          return values;
        }

        return ParseStringList(token.ToString());
      }

      var rawAuthority = Rows.Value(row, "take_authority");
      if (!string.IsNullOrWhiteSpace(rawAuthority))
      {
        try
        {
          var parsed = JToken.Parse(rawAuthority);
          if (parsed is JObject authorityObject)
          {
            if (authorityObject.TryGetValue("drop", StringComparison.OrdinalIgnoreCase, out var dropToken) &&
                dropToken != null &&
                TryParseBoolToken(dropToken, out var dropValue))
            {
              authority["drop"] = dropValue;
              hasAuthorityValues = true;
            }

            if (authorityObject.TryGetValue("locked", StringComparison.OrdinalIgnoreCase, out var lockedToken) &&
                lockedToken != null &&
                TryParseBoolToken(lockedToken, out var lockedValue))
            {
              authority["locked"] = lockedValue;
              hasAuthorityValues = true;
            }

            if (authorityObject.TryGetValue("protected_fields", StringComparison.OrdinalIgnoreCase, out var protectedFieldsToken) &&
                protectedFieldsToken != null)
            {
              var protectedFields = ParseProtectedFieldsToken(protectedFieldsToken);
              authority["protected_fields"] = protectedFields;
              hasAuthorityValues = true;
            }
          }
        }
        catch
        {
          // Invalid JSON in take_authority column is ignored.
        }
      }

      var rawDrop = Rows.Value(row, "take_authority_drop");
      if (Strings.TryParseBool(rawDrop, out var drop))
      {
        authority["drop"] = drop;
        hasAuthorityValues = true;
      }

      var rawLocked = Rows.Value(row, "take_authority_locked");
      if (Strings.TryParseBool(rawLocked, out var locked))
      {
        authority["locked"] = locked;
        hasAuthorityValues = true;
      }

      var rawProtectedFields = Rows.Value(row, "take_authority_protected_fields");
      if (!string.IsNullOrWhiteSpace(rawProtectedFields))
      {
        List<string> protectedFields;
        try
        {
          var parsedProtectedFields = JToken.Parse(rawProtectedFields);
          protectedFields = ParseProtectedFieldsToken(parsedProtectedFields);
        }
        catch
        {
          protectedFields = ParseStringList(rawProtectedFields);
        }

        authority["protected_fields"] = protectedFields;
        hasAuthorityValues = true;
      }

      return hasAuthorityValues ? authority : null;
    }

    public static bool IsRetiredOperationStatus(string value)
    {
      return string.Equals(NormalizeOperationStatus(value), "retired", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeOperationStatus(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

      var normalized = value.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
      return normalized switch
      {
        "aktiv" => "active",
        "in_betrieb" => "active",
        "ausgemustert" => "retired",
        "stillgelegt" => "decommissioned",
        "eingelagert" => "decommissioned",
        "ausser_betrieb" => "out_of_order",
        "außer_betrieb" => "out_of_order",
        "undefiniert" => string.Empty,
        "limited" => "limited_use",
        "outoforder" => "out_of_order",
        "decommission" => "retired",
        _ => normalized
      };
    }

    private static string NormalizeStatus(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

      var normalized = value.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
      return normalized switch
      {
        "finalise_creation" => "finalize_creation",
        _ => normalized
      };
    }

    private static string NormalizeOwnership(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

      return value.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
    }

    private static string NormalizeCurrency(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

      return value.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Returns true if a 4xx API response indicates the device is retired and
    /// therefore the regular update endpoint refused the request.
    /// The API returns HTTP 400 with meta.msg.message = "Device retired.".
    /// </summary>
    public static bool IsDeviceRetiredError(string? response)
    {
      if (string.IsNullOrWhiteSpace(response))
        return false;

      try
      {
        var root = JObject.Parse(response);
        var message = root.SelectToken("meta.msg.message")?.ToString();
        if (string.IsNullOrWhiteSpace(message))
          return false;

        // Tolerant comparison; samedis currently returns "Device retired."
        return message.Trim().StartsWith("Device retired", StringComparison.OrdinalIgnoreCase);
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Creates a completed "recommission_device" issue, which the samedis API
    /// uses to flip a retired device back to active. After this call succeeds
    /// the inventory update endpoint will accept normal PUT requests again.
    /// Returns the raw response on success, null on failure.
    /// </summary>
    public static string? PostRecommissionIssue(
      RequestData client,
      string issuesResource,
      string inventoryId,
      string inventoryNumber,
      string inventoryTitle,
      ISyncLog log)
    {
      if (string.IsNullOrWhiteSpace(inventoryId))
        return null;

      var today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
      var safeTitle = string.IsNullOrWhiteSpace(inventoryTitle) ? inventoryNumber : inventoryTitle;

      // NOTE: inventory_operation_status is "cached from inventory" per the API
      // doc and does NOT drive the status change -- the recommission_device issue
      // type itself flips the device back to active, so we must not send it here.
      var payload = JsonConvert.SerializeObject(new
      {
        data = new Dictionary<string, object?>
        {
          ["inventory_id"] = inventoryId,
          ["issue_type"] = "recommission_device",
          ["title"] = $"Auto-recommission via external sync ({safeTitle})",
          ["status"] = "done",
          ["date"] = today,
          ["done_at"] = today
        }
      });

      var response = client.Post(issuesResource, payload);
      if (client.StatusCode >= 200 && client.StatusCode < 300)
      {
        log.Debug($"Recommission issue created for inventory (inventory_number='{inventoryNumber}', id='{inventoryId}').");
        return response;
      }

      log.Warn($"Recommission issue creation failed (inventory_number='{inventoryNumber}', id='{inventoryId}', status={client.StatusCode}). Response: {response}");
      return null;
    }

    /// <summary>
    /// Creates a completed "device_retired" issue, which is the canonical samedis
    /// way to retire ("ausmustern") a device. Writing retirement_date on the inventory
    /// would also retire it (the backend derives device_retired from that field), but it
    /// leaves operation_status inconsistent for the recommission path and is not
    /// reversible -- so retirement must always go through this issue.
    /// Second use: normalizing a device whose device_retired=true does not match its
    /// operation_status, so that a following recommission_device issue takes effect.
    /// Returns the raw response on success, null when no inventory id was provided.
    /// The caller inspects <paramref name="client"/>.StatusCode for success/already-retired.
    /// </summary>
    public static string? PostDeviceRetiredIssue(
      RequestData client,
      string issuesResource,
      string inventoryId,
      string inventoryNumber,
      string inventoryTitle,
      string? retirementDate,
      ISyncLog log,
      bool deleteOpenTasks = true)
    {
      if (string.IsNullOrWhiteSpace(inventoryId))
        return null;

      var date = string.IsNullOrWhiteSpace(retirementDate)
        ? DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        : retirementDate;
      var safeTitle = string.IsNullOrWhiteSpace(inventoryTitle) ? inventoryNumber : inventoryTitle;

      var payload = JsonConvert.SerializeObject(new
      {
        data = new Dictionary<string, object?>
        {
          ["inventory_id"] = inventoryId,
          ["issue_type"] = "device_retired",
          ["title"] = $"Auto-retire via external sync ({safeTitle})",
          ["status"] = "done",
          ["date"] = date,
          ["done_at"] = date,
          // Mirrors the backend's Inventory#auto_retire!: without this the backend
          // rejects retirement ("inventory_cannot_be_retired") whenever the device
          // still has open (not-done) issues. true deletes those open issues first.
          // For the normalize-before-recommission case we pass false so we never
          // destroy the open issues of a device we are about to reactivate.
          ["delete_currently_open_tasks"] = deleteOpenTasks,
          // Confirmation field for devices configured to contain patient data;
          // harmless otherwise.
          ["patient_data_securely_removed"] = true
        }
      });

      var response = client.Post(issuesResource, payload);
      if (client.StatusCode >= 200 && client.StatusCode < 300)
      {
        log.Debug($"Device_retired issue created for inventory (inventory_number='{inventoryNumber}', id='{inventoryId}').");
      }

      return response;
    }

    /// <summary>
    /// Fetches the inventory and returns its current device_retired flag. Used to
    /// avoid creating duplicate device_retired issues on every sync run for devices
    /// that are already retired (the backend would otherwise happily create another
    /// done device_retired issue without changing anything).
    /// Returns false when the inventory cannot be fetched.
    /// </summary>
    public static bool IsInventoryDeviceRetired(
      RequestData client,
      string inventoryResource,
      string inventoryId)
    {
      if (string.IsNullOrWhiteSpace(inventoryId))
        return false;

      var response = client.Get(inventoryResource + "/" + Uri.EscapeDataString(inventoryId));
      if (client.StatusCode != 200 || string.IsNullOrWhiteSpace(response))
        return false;

      try
      {
        var root = JsonConvert.DeserializeObject<Root>(response);
        return root?.Data?.FirstOrDefault()?.Attributes?.DeviceRetired ?? false;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Fetches the inventory and reports the two fields that drive retirement:
    /// device_retired (the flag the update endpoint validates against) and
    /// operation_status. Both are needed because a recommission_device issue only
    /// has an effect when operation_status is 'retired' -- see
    /// Issue#update_operation_status! in the backend, which bails out unless the
    /// issue's cached inventory_operation_status actually changes. A device with
    /// device_retired=true but operation_status='active'/'decommissioned' therefore
    /// swallows the recommission silently (issue created with 2xx, device stays
    /// retired) and must be normalized via a device_retired issue first.
    /// Returns false when the inventory cannot be fetched.
    /// </summary>
    public static bool TryGetRetirementState(
      RequestData client,
      string inventoryResource,
      string inventoryId,
      out bool deviceRetired,
      out string operationStatus)
    {
      deviceRetired = false;
      operationStatus = string.Empty;

      if (string.IsNullOrWhiteSpace(inventoryId))
        return false;

      var response = client.Get(inventoryResource + "/" + Uri.EscapeDataString(inventoryId));
      if (client.StatusCode != 200 || string.IsNullOrWhiteSpace(response))
        return false;

      try
      {
        var attributes = JsonConvert.DeserializeObject<Root>(response)?.Data?.FirstOrDefault()?.Attributes;
        if (attributes == null)
          return false;

        deviceRetired = attributes.DeviceRetired;
        operationStatus = attributes.OperationStatus ?? string.Empty;
        return true;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Returns true if a 4xx API response indicates the device is already retired,
    /// so a repeated device_retired issue is a no-op rather than an error.
    /// </summary>
    public static bool IsAlreadyRetiredError(string? response)
    {
      if (string.IsNullOrWhiteSpace(response))
        return false;

      try
      {
        var root = JObject.Parse(response);
        var message = root.SelectToken("meta.msg.message")?.ToString();
        if (string.IsNullOrWhiteSpace(message))
          return false;

        var trimmed = message.Trim();
        // Tolerant: samedis may return "Device retired." or a message containing
        // "already retired" when the device is already in the retired state.
        return trimmed.StartsWith("Device retired", StringComparison.OrdinalIgnoreCase)
          || trimmed.IndexOf("already retired", StringComparison.OrdinalIgnoreCase) >= 0
          || trimmed.IndexOf("is retired", StringComparison.OrdinalIgnoreCase) >= 0;
      }
      catch
      {
        return false;
      }
    }

  }
}
