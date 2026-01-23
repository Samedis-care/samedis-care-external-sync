using System.Data;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SamedisExternalSync
{
  public class DeviceModels
  {
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class LocalizableContentAttribute : Attribute
    {
      public LocalizableContentAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class BackendReadOnlyAttribute : Attribute
    {
      public BackendReadOnlyAttribute() { }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Attributes
    {
      [JsonProperty("id")]
      [BackendReadOnly]
      public string? Id { get; set; }

      [JsonProperty("external_id")]
      public string? ExternalId { get; set; }

      [JsonProperty("created_at")]
      [BackendReadOnly]
      public DateTime? CreatedAt { get; set; } = null;

      [JsonProperty("updated_at")]
      [BackendReadOnly]
      public DateTime? UpdatedAt { get; set; } = null;

      [JsonProperty("created_by_user")]
      [BackendReadOnly]
      public string? CreatedByUser { get; set; }

      [JsonProperty("updated_by_user")]
      [BackendReadOnly]
      public string? UpdatedByUser { get; set; }

      [JsonProperty("tenant_name")]
      [BackendReadOnly]
      public string? TenantName { get; set; }

      [JsonProperty("is_public")]
      public bool IsPublic { get; set; }

      [JsonProperty("manufacturer_according_to_type_plate")]
      public string? ManufacturerAccordingToTypePlate { get; set; }

      [JsonProperty("current_responsible_manufacturer")]
      public string? CurrentResponsibleManufacturer { get; set; }

      [JsonProperty("risk_level")]
      public string? RiskLevel { get; set; }

      [JsonProperty("operator_ordinance")]
      public string? OperatorOrdinance { get; set; }

      [JsonProperty("title")]
      public string? Title { get; set; }

      [JsonProperty("device_type_title")]
      [BackendReadOnly]
      public string? DeviceTypeTitle { get; set; }

      [JsonProperty("device_type_title_labels")]
      [BackendReadOnly]
      public Dictionary<string, string>? DeviceTypeTitleLabels { get; set; }

      [JsonProperty("end_of_life_at")]
      public string? EndOfLifeAt { get; set; }

      [JsonProperty("end_of_life")]
      public bool EndOfLife { get; set; }

      [JsonProperty("end_of_service_at")]
      public string? EndOfServiceAt { get; set; }

      [JsonProperty("end_of_service")]
      public bool EndOfService { get; set; }

      [JsonProperty("trust_level")]
      public string? TrustLevel { get; set; }

      [JsonProperty("manufacturer_product_url")]
      public string? ManufacturerProductUrl { get; set; }

      [JsonProperty("manufacturer_product_url_labels")]
      public Dictionary<string, string>? ManufacturerProductUrlLabels { get; set; }

      [JsonProperty("manufacturer_service_url")]
      public string? ManufacturerServiceUrl { get; set; }

      [JsonProperty("manufacturer_service_url_labels")]
      public Dictionary<string, string>? ManufacturerServiceUrlLabels { get; set; }

      [JsonProperty("manufacturer_warranty_months")]
      public int? ManufacturerWarrantyMonths { get; set; }

      [JsonProperty("network_connections")]
      public List<string>? NetworkConnections { get; set; }

      [JsonProperty("stores_patient_data")]
      public bool StoresPatientData { get; set; }

      [JsonProperty("software_operating_system")]
      public string? SoftwareOperatingSystem { get; set; }

      [JsonProperty("available_in_countries")]
      public List<string>? AvailableInCountries { get; set; }

      [JsonProperty("published_languages")]
      public List<string>? PublishedLanguages { get; set; }

      [JsonProperty("published")]
      public bool Published { get; set; }

      [JsonProperty("version_number")]
      [BackendReadOnly]
      public int? VersionNumber { get; set; }

      [JsonProperty("version")]
      public string? Version { get; set; }

      [JsonProperty("version_list")]
      public string[]? VersionList { get; set; }

      [JsonProperty("device_type_id")]
      public string? DeviceTypeId { get; set; }

      [JsonProperty("tenant_id")]
      [BackendReadOnly]
      public string? TenantId { get; set; }

      [JsonProperty("responsible_company_contact_id")]
      public string? ResponsibleCompanyContactId { get; set; }

      [JsonProperty("manufacturer_company_contact_id")]
      public string? ManufacturerCompanyContactId { get; set; }

      [JsonProperty("linked_image_id")]
      [BackendReadOnly]
      public string? LinkedImageId { get; set; }

      [JsonProperty("briefing_count")]
      [BackendReadOnly]
      public int? BriefingCount { get; set; }

      [JsonProperty("vendor_briefing_count")]
      [BackendReadOnly]
      public int? VendorBriefingCount { get; set; }

      [JsonProperty("inventory_count")]
      [BackendReadOnly]
      public int? InventoryCount { get; set; }

      [JsonProperty("inventory_tenant_count")]
      [BackendReadOnly]
      public int? InventoryTenantCount { get; set; }

      [JsonProperty("inventory_patient_count")]
      [BackendReadOnly]
      public int? InventoryPatientCount { get; set; }

      [JsonProperty("inventory_no_owner_count")]
      [BackendReadOnly]
      public int? InventoryNoOwnerCount { get; set; }

      [JsonProperty("device_picture")]
      [BackendReadOnly]
      public string? DevicePicture { get; set; }

      [JsonProperty("device_tag_ids")]
      public List<string>? DeviceTagIds { get; set; }

      [JsonProperty("embedded_device_tags")]
      [BackendReadOnly]
      public List<EmbeddedDeviceTag>? EmbeddedDeviceTags { get; set; }

      [JsonProperty("associated_version_ids")]
      public List<string>? AssociatedVersionIds { get; set; }

      [JsonProperty("training_shared_version_ids")]
      public List<string>? TrainingSharedVersionIds { get; set; }

      // available in detail endpoint only
      [JsonProperty("with_service_intervals")]
      public List<WithServiceInterval>? WithServiceIntervals { get; set; }

      // available in detail endpoint only
      [JsonProperty("regulatory")]
      public Dictionary<string, string>? Regulatory { get; set; }

      public string? ToPutOrPostJson()
      {
        var dataObject = new JObject();

        foreach (var property in typeof(Attributes).GetProperties())
        {
          // Skip properties marked with BackendReadOnlyAttribute
          if (property.GetCustomAttribute<BackendReadOnlyAttribute>() != null)
            continue;

          var attrObj = Attribute.GetCustomAttribute(property, typeof(JsonPropertyAttribute));
          var jsonPropertyAttribute = attrObj as JsonPropertyAttribute;
          var propertyName = jsonPropertyAttribute?.PropertyName ?? property.Name.ToLower();

          var value = property.GetValue(this);
          if (value != null && !value.Equals(Helper.GetDefault(property.PropertyType)))
          {
            dataObject[propertyName] = JToken.FromObject(value);
          }
        }

        return new JObject { ["data"] = dataObject }.ToString(Formatting.None);
      }
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
      public Relationships? Relationships { get; set; }

      [JsonProperty("links")]
      public Links? Links { get; set; }
    }

    public class EmbeddedDeviceTag
    {
      [JsonProperty("labels")]
      public Dictionary<string, string>? Labels { get; set; }

      [JsonProperty("type")]
      public string? Type { get; set; }

      [JsonProperty("name")]
      public string? Name { get; set; }

      [JsonProperty("id")]
      public string? Id { get; set; }
    }

    public class Fields
    {
    }

    public class JsonApiOptions
    {
      [JsonProperty("padding")]
      public int Padding { get; set; }

      [JsonProperty("include")]
      public List<object>? Include { get; set; }

      [JsonProperty("fields")]
      public Fields? Fields { get; set; }
    }

    public class Links
    {
      [JsonProperty("device_picture")]
      public string? DevicePicture { get; set; }
    }

    public class Meta
    {
      [JsonProperty("git_version")]
      public string? GitVersion { get; set; }

      [JsonProperty("json_api_options")]
      public JsonApiOptions? JsonApiOptions { get; set; }

      [JsonProperty("locale")]
      public string? Locale { get; set; }

      [JsonProperty("total")]
      public int Total { get; set; }

      [JsonProperty("msg")]
      public Msg? Msg { get; set; }
    }

    public class Msg
    {
      [JsonProperty("success")]
      public bool Success { get; set; }
      public string? Message { get; set; }
    }

    public class Relationships
    {
      [JsonProperty("relationships")]
      public JObject? RelationshipsData { get; set; }
    }

    public class Root
    {
      [JsonProperty("data")]
      [JsonConverter(typeof(Helper.SingleOrArrayConverter<Data>))]
      public List<Data>? Data { get; set; }

      [JsonProperty("meta")]
      public Meta? Meta { get; set; }
    }

    public class ErrorResponse
    {
      [JsonProperty("meta")]
      public Meta? Meta { get; set; }
    }

    public static string GetErrorMessage(string jsonResponse)
    {
      try
      {
        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(jsonResponse);
        if (errorResponse?.Meta?.Msg != null && !errorResponse.Meta.Msg.Success)
        {
          return $"Error response: {errorResponse.Meta.Msg.Message}";
        }
      }
      catch (Exception ex)
      {
        return $"Failed to parse error message: {ex.Message}";
      }
      return "No error message found.";
    }

    public static DataSet CreateDeviceDataSet()
    {
      var ds = new DataSet("Devices");
      var dt = new DataTable("Devices");

      dt.Columns.Add("id", typeof(string));                   // Id
      dt.Columns.Add("title", typeof(string));                // Typ
      dt.Columns.Add("external_id", typeof(string));          // externe id
      dt.Columns.Add("device_type_id", typeof(string));       // Art Id
      dt.Columns.Add("device_type_title", typeof(string));    // Art Bezeichnung (DE)
      dt.Columns.Add("emtec_code", typeof(string));           // Regulatory Emtec TypCode, wenn vorhanden
      dt.Columns.Add("training_mandatory", typeof(string));   // Anwendungsrisiko
      dt.Columns.Add("software_version", typeof(string));     // Firmware Versionen
      dt.Columns.Add("manufacturer_id", typeof(string));      // Hersteller Typenschild Id
      dt.Columns.Add("manufacturer", typeof(string));         // Hersteller Typenschild
      dt.Columns.Add("end_of_service", typeof(string));
      dt.Columns.Add("end_of_life", typeof(string));
      dt.Columns.Add("ce_marking", typeof(string));
      dt.Columns.Add("ce_notified_body", typeof(string));
      dt.Columns.Add("responsible_5_mpg", typeof(string));    // Verantwortlicher Hersteller
      dt.Columns.Add("according_to_annex", typeof(string));   // Anlage MP
      dt.Columns.Add("risk_class", typeof(string));           // Risikoklasse
      dt.Columns.Add("active_device", typeof(string));        // Geräteart (elektrisch?)
      dt.Columns.Add("udi_di", typeof(string));

      ds.Tables.Add(dt);
      return ds;
    }

    public static void FillDeviceDataSet(DataSet ds, string json)
    {
      var root = JsonConvert.DeserializeObject<DeviceModels.Root>(json);
      if (root?.Data == null || root.Data.Count == 0)
        return;

      var table = ds.Tables["Devices"];
      if (table == null)
        return;

      foreach (var data in root.Data)
      {
        var attr = data.Attributes;
        if (attr == null)
          continue;
        var row = table.NewRow();

        row["id"] = attr.Id;
        row["title"] = attr.Title;
        row["external_id"] = attr.ExternalId;
        row["device_type_id"] = attr.DeviceTypeId;
        row["device_type_title"] = attr.DeviceTypeTitle;
        row["emtec_code"] = attr.Regulatory != null && attr.Regulatory.TryGetValue("emtec_code", out string? emtec_value) ? emtec_value : "";
        row["training_mandatory"] = string.IsNullOrEmpty(attr.OperatorOrdinance) ? "Nein" : "Ja";
        row["software_version"] = attr.VersionList != null ? string.Join("; ", attr.VersionList) : "";
        row["manufacturer_id"] = attr.ManufacturerCompanyContactId;
        row["manufacturer"] = attr.ManufacturerAccordingToTypePlate;
        row["end_of_service"] = attr.EndOfServiceAt;
        row["end_of_life"] = attr.EndOfLifeAt;
        row["ce_marking"] = attr.Regulatory != null && attr.Regulatory.ContainsKey("ce") ? "Ja" : "Nein";
        row["ce_notified_body"] = attr.Regulatory != null && attr.Regulatory.TryGetValue("ce", out string? ce_value) ? ce_value.ToLower().Equals("ce") ? "" : ce_value : "";
        row["responsible_5_mpg"] = attr.CurrentResponsibleManufacturer;
        row["according_to_annex"] = Helper.OrdinanceMap(attr.OperatorOrdinance ?? string.Empty);
        row["risk_class"] = Helper.RiskClassMap(attr.RiskLevel ?? string.Empty);
        row["active_device"] = attr.DeviceTypeTitle != null && attr.DeviceTypeTitle.ToLower().Contains("mechanisch") ? "Nein" : "Ja";
        row["udi_di"] = attr.Regulatory != null && attr.Regulatory.TryGetValue("eudamed_id", out string? udi_value) ? udi_value : "";

        table.Rows.Add(row);
      }
    }

  }

  public class WithServiceInterval
  {
    [JsonProperty("category")]
    public string? Category { get; set; }

    [JsonProperty("label")]
    public string? Label { get; set; }

    [JsonProperty("labels")]
    public Dictionary<string, string>? Labels { get; set; }

    [JsonProperty("value")]
    public int Value { get; set; }

    [JsonProperty("unit")]
    public string? Unit { get; set; }

    [JsonProperty("language")]
    public string? Language { get; set; }
  }

}
