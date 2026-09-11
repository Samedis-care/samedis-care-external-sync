using System.Data;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SamedisExternalSync
{
  public class DeviceModels
  {

    /// <summary>
    /// One device model id the source system still uses that samedis has since merged away.
    /// </summary>
    /// <remarks>
    /// Carries both titles on purpose. The id pair is what a machine needs; the two titles
    /// are what a person needs to find the right record in the source system -- "you call it
    /// Perfusor-Space, it is Perfusor Space here now".
    /// </remarks>
    public sealed class MergedCatalogRemap
    {
      public string OldCatalogId { get; init; } = string.Empty;
      public string CatalogId { get; init; } = string.Empty;
      public string SourceDeviceModelTitle { get; init; } = string.Empty;
      public string DeviceModelTitle { get; init; } = string.Empty;
      public string Manufacturer { get; init; } = string.Empty;
      public DateTime DetectedAt { get; init; } = DateTime.Now;

      /// <summary>How many rows of this run carried the historic id.</summary>
      public int AffectedInventories { get; set; }
    }

    /// <summary>
    /// Column headers of <c>device_model_merges.csv</c>, in file order.
    /// </summary>
    public static readonly string[] MergeReportHeaders =
    {
      "old_catalog_id", "catalog_id", "source_device_model_title",
      "device_model_title", "manufacturer", "affected_inventories", "detected_at"
    };

    /// <summary>
    /// Renders the collected remaps as CSV rows, in the order of <see cref="MergeReportHeaders"/>.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<string>> MergeReportRows(
      IEnumerable<MergedCatalogRemap> remaps)
      => remaps
         .OrderBy(r => r.DeviceModelTitle, StringComparer.OrdinalIgnoreCase)
         .ThenBy(r => r.OldCatalogId, StringComparer.Ordinal)
         .Select(r => (IReadOnlyList<string>)new[]
         {
           r.OldCatalogId,
           r.CatalogId,
           r.SourceDeviceModelTitle,
           r.DeviceModelTitle,
           r.Manufacturer,
           r.AffectedInventories.ToString(CultureInfo.InvariantCulture),
           r.DetectedAt.ToString(LogFormat.TimeFormat, CultureInfo.InvariantCulture)
         })
         .ToList();

    /// <summary>
    /// Everything the catalog resolution needs that does not change from row to row.
    /// </summary>
    public sealed record InventoryCatalogContext(
      IApiClient Client,
      string TenantId,
      ResourceLookup DeviceModelLookup,
      ResourceLookup DeviceTypeLookup,
      ResourceLookup ManufacturerLookup,
      IDictionary<string, string> TenantModelBySourceKey,
      bool MayCreateLocalDeviceModels,
      ISyncLog Log)
    {
      /// <summary>
      /// Device model ids the source still uses that resolved to a different record, keyed by
      /// the historic id. Filled while inventory rows are processed and written out as
      /// <c>device_model_merges.csv</c>.
      /// <para>
      /// Stays empty against the API in production, where a merged-away id is a plain 404
      /// and there is nothing to resolve it to. It fills once samedis-care-issues#2347 is
      /// deployed. The file is written either way, so the channel exists the day the
      /// backend side lands rather than having to be built then.
      /// </para>
      /// <para>
      /// Why a file of its own and not a column on the model export: the device model
      /// download is incremental (<c>updated_at &gt; lastRun</c>), and a merge records
      /// itself on the survivor with an atomic update that does not touch
      /// <c>updated_at</c> -- so the survivor would not appear in that download at all.
      /// Only the ids the source itself sends reveal a merge.
      /// </para>
      public IDictionary<string, MergedCatalogRemap> Remaps { get; }
        = new Dictionary<string, MergedCatalogRemap>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Decides which device model an inventory row is written with: the id the source gave,
    /// the id it resolves to today, one looked up from title and manufacturer, or a newly
    /// created facility-local model.
    /// </summary>
    /// <param name="sourceCatalogId">The <c>catalog_id</c> column of the source row, if any.</param>
    /// <param name="isCreateOperation">
    /// Whether this row has no inventory in samedis yet. Load-bearing: creating a device
    /// model is a create-only step -- see the comment on that branch.
    /// </param>
    /// <returns>
    /// The catalog id to write, or an empty string when none could be established. Empty is
    /// not an error on an update -- the attribute is then simply left out and the inventory
    /// keeps the device model it has.
    /// </returns>
    public static string ResolveCatalogIdForInventoryRow(
      InventoryCatalogContext ctx,
      string sourceCatalogId,
      string title,
      string manufacturer,
      string deviceTypeTitle,
      bool isCreateOperation,
      bool isPlaceholder,
      string rowId = "",
      string inventoryNumber = "")
    {
      var catalogId = sourceCatalogId ?? string.Empty;

      if (string.IsNullOrWhiteSpace(catalogId))
      {
        if (isPlaceholder)
        {
          ctx.Log.Debug($"Placeholder device model row detected. Skipping catalog/device-model lookup and local model/type/manufacturer creation (inventory_number='{inventoryNumber}', title='{title}').");
        }
        else
        {
          catalogId = DeviceModels.ResolveCatalogId(
            ctx.DeviceModelLookup,
            title,
            manufacturer
          ) ?? string.Empty;

          if (!string.IsNullOrWhiteSpace(catalogId))
          {
            ctx.Log.Debug($"Resolved catalog_id '{catalogId}' via device model lookup (title='{title}', manufacturer='{manufacturer}').");
          }
          else if (ctx.MayCreateLocalDeviceModels && isCreateOperation)
          {
            catalogId = DeviceModels.ResolveOrCreateTenantCatalogIdForInventory(
              ctx.Client,
              ctx.TenantId,
              title,
              manufacturer,
              deviceTypeTitle,
              ctx.DeviceModelLookup,
              ctx.DeviceTypeLookup,
              ctx.ManufacturerLookup,
              ctx.TenantModelBySourceKey,
              ctx.Log,
              rowId,
              inventoryNumber
            ) ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(catalogId))
            {
              ctx.Log.Debug($"Resolved catalog_id '{catalogId}' via local tenant device model lookup/create (title='{title}', manufacturer='{manufacturer}', device_type_title='{deviceTypeTitle}', inventory_number='{inventoryNumber}').");
            }
            else if (!string.IsNullOrWhiteSpace(title))
            {
              ctx.Log.Warn($"No device model match found and local tenant device model creation failed/skipped (title='{title}', manufacturer='{manufacturer}', device_type_title='{deviceTypeTitle}', inventory_number='{inventoryNumber}').");
            }
          }
          else if (ctx.MayCreateLocalDeviceModels && !string.IsNullOrWhiteSpace(title))
          {
            // Creating a device model is a CREATE-only step, deliberately.
            //
            // An existing inventory always has one already -- catalog is a required
            // belongs_to on the backend -- so there is nothing here to bootstrap, only
            // something to overwrite.
            //
            // And a miss is exactly what a merge looks like. Briefing::MergeDeviceModelsJob
            // moves the inventories to the survivor and then hard-destroys the source, so
            // the keys that belonged to the destroyed record -- its external_id, and the
            // pair (tenant_id, external_id) -- are certainly gone. The title and
            // manufacturer this cascade sends come from the source ROW, not from the
            // destroyed record, so those steps often still land on the survivor; when they
            // do not, the model is gone for good.
            //
            // Creating on that miss recreated the model an operator had just merged away
            // and pulled the device back onto the duplicate -- the merge undone on the next
            // run, every run, without an error anywhere.
            //
            // The cost of the rule is that a source system moving a device to a model
            // that does not exist in samedis yet no longer creates it on an update.
            // From here the two cases look the same, and this is the one that loses
            // curated data rather than delaying it: the next run creating the device
            // fresh, or an operator, resolves it.
            ctx.Log.Warn($"No device model match found for an EXISTING inventory; keeping the device model it already has and creating nothing (title='{title}', manufacturer='{manufacturer}', device_type_title='{deviceTypeTitle}', inventory_number='{inventoryNumber}'). If the model was merged away, this is intended -- see samedis-care-issues#2347.");
          }
          else if (!string.IsNullOrWhiteSpace(title))
          {
            ctx.Log.Warn($"No device model match found for catalog lookup (title='{title}', manufacturer='{manufacturer}', inventory_number='{inventoryNumber}').");
          }
        }
      }
      else
      {
        // The source carries a samedis catalog id of its own. Asking the server about it
        // costs one request per DISTINCT id per run -- ResourceLookup caches the answer
        // under the id that was asked for -- and it buys a diagnosis the write itself
        // cannot give.
        //
        // On the API in production a device model that was merged away is a plain 404:
        // the merge hard-destroys the source record and nothing records where it went.
        // The write then fails with "Device model can't be blank", which is exactly what
        // sending no device model at all produces -- indistinguishable from the client
        // side. Asking first turns that into a warning naming the id.
        //
        // The resolution work is built but not deployed (samedis-care-issues#2347:
        // merged_catalog_ids on the survivor, a merge fallback on the id route). Once it
        // ships this same call starts answering with the survivor's id instead of null,
        // and the branches below pick it up without a change here -- ById returns the id
        // the SERVER answered with, not the one that was asked for.
        var resolvedCatalogId = ctx.DeviceModelLookup.ById(catalogId);

        if (string.IsNullOrWhiteSpace(resolvedCatalogId))
        {
          ctx.Log.Warn($"catalog_id '{catalogId}' from the source resolves to no device model this facility can see (inventory_number='{inventoryNumber}', title='{title}'). Sending it unchanged; the backend will reject the row with 'Device model can't be blank'. Either the id never existed here, or the model was merged away -- a merge destroys the record and today leaves nothing to resolve it by (samedis-care-issues#2347).");
        }
        else if (!string.Equals(resolvedCatalogId, catalogId, StringComparison.Ordinal))
        {
          ctx.Log.Info($"Device model '{catalogId}' was merged into '{resolvedCatalogId}'; writing the current id (inventory_number='{inventoryNumber}', title='{title}'). Reported in device_model_merges.csv so the source system can update its own reference.");
          RecordRemap(ctx, catalogId, resolvedCatalogId!, title);
          catalogId = resolvedCatalogId;
        }
      }

      return catalogId;
    }

    /// <summary>
    /// Notes that <paramref name="oldCatalogId"/> resolved to a different record, for
    /// <c>device_model_merges.csv</c>. Counts every row that carried the historic id, but
    /// asks the API for the surviving model's name only the first time -- one extra request
    /// per actual merge per run, and merges are rare by construction.
    /// </summary>
    private static void RecordRemap(InventoryCatalogContext ctx, string oldCatalogId,
                                    string newCatalogId, string sourceTitle)
    {
      if (ctx.Remaps.TryGetValue(oldCatalogId, out var known))
      {
        known.AffectedInventories++;
        return;
      }

      var title = string.Empty;
      var maker = string.Empty;

      // Best effort: the id pair is the part that matters, and it is already established.
      // A failure to read the survivor's name must not cost us the mapping.
      try
      {
        var body = ctx.Client.Get($"{ctx.DeviceModelLookup.Resource}/{Uri.EscapeDataString(newCatalogId)}");
        if (JsonApi.IsSuccess(ctx.Client.StatusCode))
        {
          var attributes = JsonConvert.DeserializeObject<Root>(body)?.Data?.FirstOrDefault()?.Attributes;
          title = attributes?.Title ?? string.Empty;
          maker = attributes?.ManufacturerAccordingToTypePlate
                  ?? attributes?.CurrentResponsibleManufacturer ?? string.Empty;
        }
        else
        {
          ctx.Log.Debug($"Could not read the surviving device model '{newCatalogId}' for the merge report (status={ctx.Client.StatusCode}); reporting the id pair without its name.");
        }
      }
      catch (Exception ex)
      {
        ctx.Log.Debug($"Could not read the surviving device model '{newCatalogId}' for the merge report: {ex.Message}");
      }

      ctx.Remaps[oldCatalogId] = new MergedCatalogRemap
      {
        OldCatalogId = oldCatalogId,
        CatalogId = newCatalogId,
        SourceDeviceModelTitle = sourceTitle ?? string.Empty,
        DeviceModelTitle = title,
        Manufacturer = maker,
        AffectedInventories = 1
      };
    }

    /// <summary>
    /// Resolves the facility's own device model for an inventory row, creating it -- together
    /// with its device type and manufacturer -- when it does not exist yet.
    /// </summary>
    /// <param name="resolvedBySourceKey">
    /// What each source combination of title, manufacturer and device type resolved to this
    /// run, including the ones that resolved to nothing. Not an API lookup cache: it is keyed
    /// by the source's values before any of them have been turned into ids, and it exists so a
    /// row that cannot be resolved does not repeat the device-type and manufacturer round
    /// trips -- and the warning -- for every further row like it.
    /// </param>
    /// <remarks>
    /// A create rejected because the model already exists is not treated as a failure: the
    /// server names the colliding record in <c>meta.msg.error_details</c> and
    /// <see cref="Records.Create"/> reuses it. That replaces this file's own
    /// TryExtractDuplicateModelId.
    /// </remarks>
    public static string? ResolveOrCreateTenantCatalogIdForInventory(
      IApiClient client,
      string tenantId,
      string modelTitle,
      string manufacturer,
      string deviceTypeTitle,
      ResourceLookup deviceModelLookup,
      ResourceLookup deviceTypeLookup,
      ResourceLookup manufacturerLookup,
      IDictionary<string, string> resolvedBySourceKey,
      ISyncLog log,
      string contextId = "",
      string inventoryNumber = "")
    {
      var title = modelTitle?.Trim() ?? string.Empty;
      if (string.IsNullOrWhiteSpace(title))
        return null;

      var maker = manufacturer?.Trim() ?? string.Empty;
      var typeTitle = deviceTypeTitle?.Trim() ?? string.Empty;
      var sourceKey = title + "|" + maker + "|" + typeTitle;
      var where = $"(model_title='{title}', manufacturer='{maker}', device_type_title='{typeTitle}', source_id='{contextId}', inventory_number='{inventoryNumber}')";

      if (resolvedBySourceKey.TryGetValue(sourceKey, out var known))
        return string.IsNullOrWhiteSpace(known) ? null : known;

      string? Give(string? id)
      {
        resolvedBySourceKey[sourceKey] = id ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(id))
        {
          // Seed the plain title/manufacturer lookup as well, so a later row carrying only
          // those two is answered from memory.
          Cascades.RememberDeviceModel(deviceModelLookup, title, maker, id,
                                       caseInsensitiveTitleMatch: false);
          log.Debug($"Local tenant device model resolved/created {where} -> catalog_id '{id}'.");
        }
        return id;
      }

      // Both are required to create a model, and neither can be invented.
      if (string.IsNullOrWhiteSpace(typeTitle))
      {
        log.Warn($"Local device model creation skipped: device_type_title missing {where}.");
        return Give(null);
      }

      if (string.IsNullOrWhiteSpace(maker))
      {
        log.Warn($"Local device model creation skipped: manufacturer missing {where}.");
        return Give(null);
      }

      var deviceTypeId = DeviceTypes.ResolveDeviceTypeId(
        client, deviceTypeLookup.Resource, typeTitle, createOnTheFly: true,
        deviceTypeLookup, log, tenantId, contextId, title);
      if (string.IsNullOrWhiteSpace(deviceTypeId))
        return Give(null);

      var manufacturerId = Contacts.ResolveCompanyContactId(
        client, manufacturerLookup.Resource, maker, createOnTheFly: true,
        manufacturerLookup, log, contextId, title);
      if (string.IsNullOrWhiteSpace(manufacturerId))
        return Give(null);

      // The facility's own model is identified by title, device type and manufacturer
      // together. The server checks a duplicate against title, manufacturer and version.
      var conditions = new (string, string?)[]
      {
        ("title", title),
        ("device_type_id", deviceTypeId),
        ("manufacturer_according_to_type_plate", maker),
      };
      const string bothScopes = "filter[scope]=public_and_tenant";

      return Give(Records.FindOrCreate(
        client, deviceModelLookup.Resource,
        find: () => deviceModelLookup.ByFields(conditions, FilterBuilder.FilterType.Equals, bothScopes),
        attributes: new Dictionary<string, object?>
        {
          ["title"] = title,
          ["device_type_id"] = deviceTypeId,
          ["manufacturer_according_to_type_plate"] = maker,
          ["current_responsible_manufacturer"] = maker,
          ["manufacturer_company_contact_id"] = manufacturerId,
          ["responsible_company_contact_id"] = manufacturerId,
          ["is_public"] = false
        },
        log, $"local device model {where}",
        remember: id => deviceModelLookup.RememberFields(conditions, id,
                                                         FilterBuilder.FilterType.Equals, bothScopes)));
    }

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

      // Ids of device models that were merged INTO this one.
      //
      // Not served by the API in production yet -- the field arrives with
      // samedis-care-issues#2347, and only on the detail serializer, never on the paged
      // list. Deserialising it costs nothing while it is absent (null -> empty column),
      // and the export already fetches each model's detail for the service intervals, so
      // the column fills by itself the day the backend side ships.
      [JsonProperty("merged_catalog_ids")]
      [BackendReadOnly]
      public List<string>? MergedCatalogIds { get; set; }

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
      [JsonConverter(typeof(JsonApi.SingleOrArrayConverter<Data>))]
      public List<Data>? Data { get; set; }

      [JsonProperty("meta")]
      public Meta? Meta { get; set; }
    }
    public static DataSet CreateDeviceDataSet()
    {
      var ds = new DataSet("Devices");
      var dt = new DataTable("Devices");

      dt.Columns.Add("id", typeof(string));                   // Id
      dt.Columns.Add("title", typeof(string));                // Typ
      dt.Columns.Add("external_id", typeof(string));          // externe id
      // Alt-Ids, die in dieses Modell gemergt wurden -- kommagetrennt, damit das
      // Quellsystem eine gespeicherte Id von vor einem Merge wiedererkennt und auf die
      // heutige umschreiben kann. Komma, nicht Semikolon: das ist das CSV-Trennzeichen.
      dt.Columns.Add("merged_catalog_ids", typeof(string));   // Alt-Ids nach Merge
      dt.Columns.Add("device_type_id", typeof(string));       // Art Id
      dt.Columns.Add("device_type_title", typeof(string));    // Art Bezeichnung (DE)
      dt.Columns.Add("emtec_code", typeof(string));           // Regulatory Emtec TypCode, wenn vorhanden
      // Zwei verschiedene Dinge, die hier lange eines waren:
      //   application_risk    Catalog#risk_level -- das Anwendungsrisiko, unknown/0/1/2
      //   training_mandatory  daraus abgeleitet: braucht das Geraet eine Einweisung?
      // Die MDR-Risikoklasse steht weiter unten in risk_class und kommt aus
      // regulatory.eu_mdr, nicht von hier.
      dt.Columns.Add("application_risk", typeof(string));     // Anwendungsrisiko (Rohwert)
      dt.Columns.Add("training_mandatory", typeof(string));   // daraus: Einweisung noetig?
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
        row["merged_catalog_ids"] = attr.MergedCatalogIds != null ? string.Join(",", attr.MergedCatalogIds) : "";
        row["device_type_id"] = attr.DeviceTypeId;
        row["device_type_title"] = attr.DeviceTypeTitle;
        row["emtec_code"] = attr.Regulatory != null && attr.Regulatory.TryGetValue("emtec_code", out string? emtec_value) ? emtec_value : "";
        // Kam bis hierher aus operator_ordinance, also aus der Betreiberverordnung. Das ist
        // eine andere Frage: die Anlage sagt, welchen Regeln ein Geraet unterliegt, das
        // Anwendungsrisiko sagt, ob jemand eingewiesen werden muss. Die Anlage steht
        // unveraendert in according_to_annex.
        row["application_risk"] = attr.RiskLevel ?? "";
        row["training_mandatory"] = CatalogValues.RequiresTraining(attr.RiskLevel) switch
        {
          true => "Ja",
          false => "Nein",
          null => "",
        };
        row["software_version"] = attr.VersionList != null ? string.Join("; ", attr.VersionList) : "";
        row["manufacturer_id"] = attr.ManufacturerCompanyContactId;
        row["manufacturer"] = attr.ManufacturerAccordingToTypePlate;
        row["end_of_service"] = attr.EndOfServiceAt;
        row["end_of_life"] = attr.EndOfLifeAt;
        row["ce_marking"] = attr.Regulatory != null && attr.Regulatory.ContainsKey("ce") ? "Ja" : "Nein";
        row["ce_notified_body"] = attr.Regulatory != null && attr.Regulatory.TryGetValue("ce", out string? ce_value) ? ce_value.ToLower().Equals("ce") ? "" : ce_value : "";
        row["responsible_5_mpg"] = attr.CurrentResponsibleManufacturer;
        row["according_to_annex"] = CatalogValues.OperatorOrdinanceMap(attr.OperatorOrdinance ?? string.Empty);
        // Aus regulatory.eu_mdr, nicht aus risk_level. Die beiden Felder ueberlappen in
        // ihren Werten ("1", "2"), weshalb die Verwechslung nie aufgefallen ist: ein Geraet,
        // das bloss eine Anwendereinweisung braucht, stand hier als MDR-Klasse I.
        row["risk_class"] = CatalogValues.MdrRiskClassMap(
          attr.Regulatory != null && attr.Regulatory.TryGetValue("eu_mdr", out var euMdr) ? euMdr : null);
        row["active_device"] = attr.DeviceTypeTitle != null && attr.DeviceTypeTitle.ToLower().Contains("mechanisch") ? "Nein" : "Ja";
        row["udi_di"] = attr.Regulatory != null && attr.Regulatory.TryGetValue("eudamed_id", out string? udi_value) ? udi_value : "";

        table.Rows.Add(row);
      }
    }
    /// <summary>
    /// Resolves a device model by title and manufacturer.
    /// </summary>
    /// <remarks>
    /// The manufacturer is tried against two fields because source systems use them
    /// interchangeably: the type-plate manufacturer first, then the currently responsible
    /// one. Both steps search the tenant's own models and the public catalog.
    /// <para>
    /// The title is compared case-sensitively, which is what this did before the migration.
    /// Source data and catalog entries routinely differ only in casing ("seca 954" against
    /// "Seca 954"), and those are missed today; passing
    /// <c>caseInsensitiveTitleMatch: true</c> would catch them. Left as it was because it
    /// changes which record a row resolves to, which is a decision about the data, not the
    /// code.
    /// </para>
    /// </remarks>
    public static string? ResolveCatalogId(ResourceLookup lookup, string title, string manufacturer)
      => Cascades.DeviceModel(lookup, null, title, manufacturer, caseInsensitiveTitleMatch: false);
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
