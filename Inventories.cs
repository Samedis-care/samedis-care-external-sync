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
      [JsonConverter(typeof(Helper.SingleOrArrayConverter<Data>))]
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
        row["purchase_price"] = attr.PurchasePrice.HasValue ? Helper.FormatDecimal(attr.PurchasePrice.Value) : "";
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

    public static string? ResolveExistingInventoryId(
      RequestData client,
      string resource,
      string inventoryId,
      string inventoryExternalId,
      string inventoryNumber,
      string inventoryModelTitle,
      string inventoryManufacturer,
      bool fallbackByDeviceNumber,
      IDictionary<string, string> inventoryById,
      IDictionary<string, string> inventoryByExternalId,
      IDictionary<string, string> inventoryByDeviceNumber,
      IDictionary<string, string> inventoryByModelAndManufacturer,
      ISet<string> checkedInventoryIds,
      ISet<string> checkedInventoryExternalIds,
      ISet<string> checkedInventoryNumbers,
      ISet<string> checkedInventoryModelAndManufacturer)
    {
      string? candidateId = null;
      string? candidateDeviceNumber = null;
      var normalizedExternalId = inventoryExternalId?.Trim() ?? string.Empty;

      if (!string.IsNullOrWhiteSpace(inventoryId))
      {
        if (inventoryById.TryGetValue(inventoryId, out var cachedId))
        {
          if (!string.IsNullOrWhiteSpace(cachedId))
            candidateId = cachedId;
        }

        if (string.IsNullOrWhiteSpace(candidateId) && !checkedInventoryIds.Contains(inventoryId))
        {
          checkedInventoryIds.Add(inventoryId);

          var detailResponse = client.Get(resource + "/" + Uri.EscapeDataString(inventoryId));
          if (client.StatusCode == 200)
          {
            var resolvedId = Helper.ExtractDataId(detailResponse) ?? inventoryId;
            inventoryById[inventoryId] = resolvedId;
            inventoryById[resolvedId] = resolvedId;

            var detailRoot = string.IsNullOrEmpty(detailResponse) ? null : JsonConvert.DeserializeObject<Inventories.Root>(detailResponse);
            var resolvedDeviceNumber = detailRoot?.Data?.FirstOrDefault()?.Attributes?.DeviceNumber;
            if (!string.IsNullOrWhiteSpace(resolvedDeviceNumber))
            {
              inventoryByDeviceNumber[resolvedDeviceNumber] = resolvedId;
              candidateDeviceNumber = resolvedDeviceNumber;
            }

            candidateId = resolvedId;
          }
          else
          {
            // negative cache to prevent repeated requests for same unknown id
            inventoryById[inventoryId] = string.Empty;
          }
        }
      }

      if (string.IsNullOrWhiteSpace(candidateId) && !string.IsNullOrWhiteSpace(normalizedExternalId))
      {
        if (inventoryByExternalId.TryGetValue(normalizedExternalId, out var cachedByExternalId))
        {
          if (!string.IsNullOrWhiteSpace(cachedByExternalId))
            candidateId = cachedByExternalId;
        }

        if (string.IsNullOrWhiteSpace(candidateId) && !checkedInventoryExternalIds.Contains(normalizedExternalId))
        {
          checkedInventoryExternalIds.Add(normalizedExternalId);

          var resolvedByExternalId = Helper.ExternalIdExists(client, resource, normalizedExternalId);
          if (!string.IsNullOrWhiteSpace(resolvedByExternalId))
          {
            candidateId = resolvedByExternalId;
            inventoryByExternalId[normalizedExternalId] = resolvedByExternalId;
            inventoryById[resolvedByExternalId] = resolvedByExternalId;

            var detailResponse = client.Get(resource + "/" + Uri.EscapeDataString(resolvedByExternalId));
            if (client.StatusCode == 200)
            {
              var detailRoot = string.IsNullOrEmpty(detailResponse) ? null : JsonConvert.DeserializeObject<Inventories.Root>(detailResponse);
              var resolvedDeviceNumber = detailRoot?.Data?.FirstOrDefault()?.Attributes?.DeviceNumber;
              if (!string.IsNullOrWhiteSpace(resolvedDeviceNumber))
              {
                inventoryByDeviceNumber[resolvedDeviceNumber] = resolvedByExternalId;
                candidateDeviceNumber = resolvedDeviceNumber;
              }
            }
          }
          else
          {
            inventoryByExternalId[normalizedExternalId] = string.Empty;
          }
        }
      }

      if (!string.IsNullOrWhiteSpace(candidateId) && !string.IsNullOrWhiteSpace(normalizedExternalId))
        inventoryByExternalId[normalizedExternalId] = candidateId;

      if (!string.IsNullOrWhiteSpace(candidateId) &&
          !string.IsNullOrWhiteSpace(candidateDeviceNumber) &&
          string.Equals(candidateDeviceNumber, inventoryNumber, StringComparison.OrdinalIgnoreCase))
      {
        return candidateId;
      }

      if (fallbackByDeviceNumber && !string.IsNullOrWhiteSpace(inventoryNumber))
      {
        if (inventoryByDeviceNumber.TryGetValue(inventoryNumber, out var cachedByDeviceNumber))
          return string.IsNullOrWhiteSpace(cachedByDeviceNumber) ? candidateId : cachedByDeviceNumber;

        if (!checkedInventoryNumbers.Contains(inventoryNumber))
        {
          checkedInventoryNumbers.Add(inventoryNumber);

          var filterBuilder = new FilterBuilder();
          filterBuilder.Clear();
          filterBuilder.Add("device_number", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, inventoryNumber);

          var listResponse = client.Get(
            resource +
            $"?page[number]=1&page[limit]=1&variant=regular&gridfilter={filterBuilder.Get()}"
          );
          if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(listResponse))
          {
            var listRoot = JsonConvert.DeserializeObject<Root>(listResponse);
            var resolvedByDeviceNumber = listRoot?.Data?.FirstOrDefault();
            var resolvedByDeviceNumberId = resolvedByDeviceNumber?.Attributes?.Id ?? resolvedByDeviceNumber?.Id;
            if (!string.IsNullOrWhiteSpace(resolvedByDeviceNumberId))
            {
              inventoryById[resolvedByDeviceNumberId] = resolvedByDeviceNumberId;
              inventoryByDeviceNumber[inventoryNumber] = resolvedByDeviceNumberId;
              return resolvedByDeviceNumberId;
            }
          }

          inventoryByDeviceNumber[inventoryNumber] = string.Empty;
        }
      }

      if (!string.IsNullOrWhiteSpace(candidateId))
        return candidateId;

      if (string.IsNullOrWhiteSpace(inventoryModelTitle))
        return candidateId;

      var normalizedModelTitle = inventoryModelTitle.Trim();
      var normalizedManufacturer = inventoryManufacturer?.Trim() ?? string.Empty;
      var modelManufacturerKey = $"{normalizedModelTitle}|{normalizedManufacturer}";

      if (inventoryByModelAndManufacturer.TryGetValue(modelManufacturerKey, out var cachedByModelManufacturer))
        return string.IsNullOrWhiteSpace(cachedByModelManufacturer) ? candidateId : cachedByModelManufacturer;

      if (checkedInventoryModelAndManufacturer.Contains(modelManufacturerKey))
        return candidateId;

      checkedInventoryModelAndManufacturer.Add(modelManufacturerKey);

      string? ResolveByModelAndManufacturer(string? manufacturerField, string? manufacturerValue)
      {
        var filterBuilder = new FilterBuilder();
        filterBuilder.Clear();
        filterBuilder.Add("device_model_title", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, normalizedModelTitle);
        if (!string.IsNullOrWhiteSpace(manufacturerField) && !string.IsNullOrWhiteSpace(manufacturerValue))
          filterBuilder.Add(manufacturerField, FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, manufacturerValue);

        var listResponse = client.Get(
          resource +
          $"?page[number]=1&page[limit]=1&variant=regular&gridfilter={filterBuilder.Get()}"
        );
        if (client.StatusCode != 200 || string.IsNullOrWhiteSpace(listResponse))
          return null;

        var listRoot = JsonConvert.DeserializeObject<Root>(listResponse);
        var found = listRoot?.Data?.FirstOrDefault();
        var foundId = found?.Attributes?.Id ?? found?.Id;
        return string.IsNullOrWhiteSpace(foundId) ? null : foundId;
      }

      string? resolvedByModelAndManufacturer = null;
      if (!string.IsNullOrWhiteSpace(normalizedManufacturer))
      {
        resolvedByModelAndManufacturer = ResolveByModelAndManufacturer("device_model_manufacturer_according_to_type_plate", normalizedManufacturer);
        resolvedByModelAndManufacturer ??= ResolveByModelAndManufacturer("device_model_current_responsible_manufacturer", normalizedManufacturer);
      }
      else
      {
        resolvedByModelAndManufacturer = ResolveByModelAndManufacturer(null, null);
      }

      inventoryByModelAndManufacturer[modelManufacturerKey] = resolvedByModelAndManufacturer ?? string.Empty;
      if (!string.IsNullOrWhiteSpace(resolvedByModelAndManufacturer))
      {
        inventoryById[resolvedByModelAndManufacturer] = resolvedByModelAndManufacturer;
        return resolvedByModelAndManufacturer;
      }

      return candidateId;
    }

    public static Dictionary<string, object> BuildInventoryAttributes(
      DataRow row,
      string? departmentId,
      string? locationId,
      string? catalogIdOverride = null,
      bool applyCreateDefaults = false)
    {
      var attributes = new Dictionary<string, object>();
      var modelTitle = Helper.GetRowValue(row, "device_model_title");
      if (string.IsNullOrWhiteSpace(modelTitle))
        modelTitle = Helper.GetRowValue(row, "title");

      var manufacturer = Helper.GetRowValue(row, "manufacturer");
      if (string.IsNullOrWhiteSpace(manufacturer))
        manufacturer = Helper.GetRowValue(row, "responsible_manufacturer");
      if (string.IsNullOrWhiteSpace(manufacturer))
        manufacturer = Helper.GetRowValue(row, "company");

      var deviceTypeTitle = Helper.GetRowValue(row, "device_type_title");
      var placeholderManufacturer = Helper.GetRowValue(row, "placeholder_device_model_manufacturer");
      var placeholderModelTitle = Helper.GetRowValue(row, "placeholder_device_model_title");
      var placeholderDeviceTypeTitle = Helper.GetRowValue(row, "placeholder_device_type_title");
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

      Helper.AddStringAttribute(attributes, "external_id", Helper.GetRowValue(row, "external_id"));
      Helper.AddStringAttribute(attributes, "device_number", Helper.GetRowValue(row, "inventory_number"));
      Helper.AddStringAttribute(attributes, "serial_number", Helper.GetRowValue(row, "serial_number"));
      var catalogId = string.IsNullOrWhiteSpace(catalogIdOverride) ? Helper.GetRowValue(row, "catalog_id") : catalogIdOverride;
      Helper.AddStringAttribute(attributes, "catalog_id", catalogId);
      Helper.AddStringAttribute(attributes, "commissioning_at", Helper.NormalizeDate(Helper.GetRowValue(row, "commissioning_at")));
      Helper.AddStringAttribute(attributes, "service_partner", Helper.GetRowValue(row, "service_partner"));
      Helper.AddStringAttribute(attributes, "comments_field", Helper.GetRowValue(row, "comments_field"));
      Helper.AddStringAttribute(attributes, "operation_status", NormalizeOperationStatus(Helper.GetRowValue(row, "operation_status")));
      Helper.AddStringAttribute(attributes, "retirement_date", Helper.NormalizeDate(Helper.GetRowValue(row, "retirement_date")));
      Helper.AddStringAttribute(attributes, "status", NormalizeStatus(Helper.GetRowValue(row, "status")));
      Helper.AddStringAttribute(attributes, "ownership", NormalizeOwnership(Helper.GetRowValue(row, "ownership")));
      Helper.AddStringAttribute(attributes, "currency_code", NormalizeCurrency(Helper.GetRowValue(row, "currency_code")));
      Helper.AddStringAttribute(attributes, "date_of_acquisition", Helper.NormalizeDate(Helper.GetRowValue(row, "date_of_acquisition")));
      Helper.AddStringAttribute(attributes, "delivered_at", Helper.NormalizeDate(Helper.GetRowValue(row, "delivered_at")));
      Helper.AddStringAttribute(attributes, "installed_at", Helper.NormalizeDate(Helper.GetRowValue(row, "installed_at")));
      Helper.AddStringAttribute(attributes, "warranty_period", Helper.NormalizeDate(Helper.GetRowValue(row, "warranty_period")));
      Helper.AddStringAttribute(attributes, "asset_accounting_number", Helper.GetRowValue(row, "asset_accounting_number"));
      Helper.AddStringAttribute(attributes, "device_condition", Helper.GetRowValue(row, "device_condition"));
      Helper.AddStringAttribute(attributes, "device_nick_name", Helper.GetRowValue(row, "device_nick_name"));
      Helper.AddStringAttribute(attributes, "manufacturer_system_number", Helper.GetRowValue(row, "manufacturer_system_number"));
      Helper.AddStringAttribute(attributes, "network_connectivity", Helper.GetRowValue(row, "network_connectivity"));
      Helper.AddStringAttribute(attributes, "operating_system", Helper.GetRowValue(row, "operating_system"));
      Helper.AddStringAttribute(attributes, "software_version", Helper.GetRowValue(row, "software_version"));
      Helper.AddStringAttribute(attributes, "ip_address", Helper.GetRowValue(row, "ip_address"));
      Helper.AddStringAttribute(attributes, "mac_address", Helper.GetRowValue(row, "mac_address"));
      var qrCodeToken = Helper.GetRowValue(row, "qr_code_token");
      if (string.IsNullOrWhiteSpace(qrCodeToken))
        qrCodeToken = Helper.GetRowValue(row, "qr_code_resource_token");
      Helper.AddStringAttribute(attributes, "qr_code_token", qrCodeToken);
      Helper.AddStringAttribute(attributes, "commissioning_through", Helper.GetRowValue(row, "commissioning_through"));
      Helper.AddStringAttribute(attributes, "linked_image_id", Helper.GetRowValue(row, "linked_image_id"));
      Helper.AddStringAttribute(attributes, "main_inventory_id", Helper.GetRowValue(row, "main_inventory_id"));
      Helper.AddStringAttribute(attributes, "main_inventory_number", Helper.GetRowValue(row, "main_inventory_number"));
      Helper.AddStringAttribute(attributes, "supplier_company_contact_id", Helper.GetRowValue(row, "supplier_company_contact_id"));
      Helper.AddStringAttribute(attributes, "supplier_company_name", Helper.GetRowValue(row, "supplier_company_name"));
      Helper.AddStringAttribute(attributes, "placeholder_device_model_manufacturer", placeholderManufacturer);
      Helper.AddStringAttribute(attributes, "placeholder_device_model_title", placeholderModelTitle);
      Helper.AddStringAttribute(attributes, "placeholder_device_type_title", placeholderDeviceTypeTitle);
      Helper.AddStringAttribute(attributes, "variant", Helper.GetRowValue(row, "variant"));
      Helper.AddStringAttribute(attributes, "type_plate", Helper.GetRowValue(row, "type_plate"));
      Helper.AddStringAttribute(attributes, "type_plate_data_uri", Helper.GetRowValue(row, "type_plate_data_uri"));
      Helper.AddStringAttribute(attributes, "depreciation_date", Helper.NormalizeDate(Helper.GetRowValue(row, "depreciation_date")));

      // API docs define construction_year as string.
      Helper.AddStringAttribute(attributes, "construction_year", Helper.GetRowValue(row, "construction_year"));

      if (Helper.TryParseInt(Helper.GetRowValue(row, "depreciation_in_years"), out var depreciationInYears))
        attributes["depreciation_in_years"] = depreciationInYears;

      if (Helper.TryParseInt(Helper.GetRowValue(row, "lifespan"), out var lifespan))
        attributes["lifespan"] = lifespan;

      if (Helper.TryParseDecimal(Helper.GetRowValue(row, "purchase_price"), out var purchasePrice))
        attributes["purchase_price"] = purchasePrice;

      if (Helper.TryParseLong(Helper.GetRowValue(row, "purchase_price_in_cents"), out var purchasePriceInCents))
        attributes["purchase_price_in_cents"] = purchasePriceInCents;

      if (Helper.TryParseBool(Helper.GetRowValue(row, "accessible_usb_ports"), out var accessibleUsbPorts))
        attributes["accessible_usb_ports"] = accessibleUsbPorts;

      if (Helper.TryParseBool(Helper.GetRowValue(row, "contains_patient_data"), out var containsPatientData))
        attributes["contains_patient_data"] = containsPatientData;

      if (applyCreateDefaults)
        attributes["do_maintenance"] = true;
      if (Helper.TryParseBool(Helper.GetRowValue(row, "do_maintenance"), out var doMaintenance))
        attributes["do_maintenance"] = doMaintenance;

      if (applyCreateDefaults)
        attributes["no_medical_device"] = false;
      if (Helper.TryParseBool(Helper.GetRowValue(row, "no_medical_device"), out var noMedicalDevice))
        attributes["no_medical_device"] = noMedicalDevice;

      if (isPlaceholder)
      {
        attributes["device_model_is_placeholder"] = true;
      }
      else if (Helper.TryParseBool(Helper.GetRowValue(row, "device_model_is_placeholder"), out var isPlaceholderValue))
      {
        attributes["device_model_is_placeholder"] = isPlaceholderValue;
      }

      if (Helper.TryParseBool(Helper.GetRowValue(row, "has_warranty"), out var hasWarranty))
        attributes["has_warranty"] = hasWarranty;

      if (Helper.TryParseBool(Helper.GetRowValue(row, "is_device_system"), out var isDeviceSystem))
        attributes["is_device_system"] = isDeviceSystem;

      var serviceCompanyIds = ParseStringList(Helper.GetRowValue(row, "service_company_ids"));
      if (serviceCompanyIds.Count > 0)
        attributes["service_company_ids"] = serviceCompanyIds;

      var teamIds = ParseStringList(Helper.GetRowValue(row, "team_ids"));
      if (teamIds.Count > 0)
        attributes["team_ids"] = teamIds;

      var withServiceIntervals = ParseJsonValue(Helper.GetRowValue(row, "with_service_intervals"));
      if (withServiceIntervals != null)
        attributes["with_service_intervals"] = withServiceIntervals;

      var nics = ParseJsonValue(Helper.GetRowValue(row, "nics"));
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
      return Helper.TryParseBool(Helper.GetRowValue(row, "device_model_is_placeholder"), out var isPlaceholder) && isPlaceholder;
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

        return Helper.TryParseBool(token.ToString(), out value);
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

      var rawAuthority = Helper.GetRowValue(row, "take_authority");
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

      var rawDrop = Helper.GetRowValue(row, "take_authority_drop");
      if (Helper.TryParseBool(rawDrop, out var drop))
      {
        authority["drop"] = drop;
        hasAuthorityValues = true;
      }

      var rawLocked = Helper.GetRowValue(row, "take_authority_locked");
      if (Helper.TryParseBool(rawLocked, out var locked))
      {
        authority["locked"] = locked;
        hasAuthorityValues = true;
      }

      var rawProtectedFields = Helper.GetRowValue(row, "take_authority_protected_fields");
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

  }
}
