using Newtonsoft.Json;
using System.Data;
using System.Globalization;

namespace SamedisExternalSync;

internal class Program
{
  static void Main(string[] args)
  {
    #region init
    // Bootstrap logger with the previous defaults (level 1, console + file), because the
    // config that carries the real level and mode is only read below - and reading it can
    // already fail and needs to log.
    //
    // The date is formatted invariantly, not with ToShortDateString(): that is
    // culture-dependent and yields "8/28/2026" in several cultures, whose slash turns the
    // file name into a directory path. yyyy-MM-dd also sorts.
    var logFile = Path.Combine("log", $"Logfile_{DateTime.Now:yyyy-MM-dd}.log");
    ISyncLog log = new FileSyncLog(1, LogMode.Both, logFile);

    // read config
    var ymlFilePath = "config.yml";
    if (!File.Exists(ymlFilePath))
      Abort(log, $"The file {ymlFilePath} does not exists. Stopping Import.");

    AppConfig config = ConfigStore.Load<AppConfig>(ymlFilePath, ignoreUnmatchedProperties: false);

    // Now with the configured level and mode.
    log = new FileSyncLog(config.Logging.Level, (LogMode)config.Logging.Mode, logFile);

    // Passed explicitly wherever numbers are parsed. The version this replaces kept the
    // separator in a mutable static, so the meaning of a parse depended on load order.
    NumberFormat numberFormat;
    try
    {
      numberFormat = NumberFormat.FromSetting(config.Formatting?.DecimalSeparator);
    }
    catch (ArgumentException ex)
    {
      // Caught rather than left to surface as a stack trace: this is the first thing the run
      // does with the config, and a typo here (the CSV delimiter is a frequent one) should
      // read as a configuration problem, not as a crash.
      Abort(log, $"formatting.decimal_separator in config.yml is invalid: {ex.Message}");
      return;
    }

    log.Info("Sync started.");

    // last run handler (supports legacy date formats and writes ISO datetime with timezone)
    const string lastRunFormat = "yyyy-MM-ddTHH:mm:ss.fffzzz";
    var lastRunFallback = new DateTimeOffset(2022, 1, 1, 0, 0, 0, DateTimeOffset.Now.Offset).ToString(lastRunFormat, CultureInfo.InvariantCulture);
    var lastRunFilePath = "lastrun.txt";
    var lastRunRaw = File.Exists(lastRunFilePath) ? File.ReadAllText(lastRunFilePath).Trim() : lastRunFallback;

    var acceptedLastRunFormats = new[]
    {
      lastRunFormat,
      "o",
      "yyyy-MM-ddTHH:mm:sszzz",
      "yyyy-MM-ddTHH:mm:ssK",
      "yyyy-MM-dd HH:mm:ss",
      "yyyy-MM-dd"
    };

    var parsedLastRunOk =
      DateTimeOffset.TryParseExact(lastRunRaw, acceptedLastRunFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedLastRun) ||
      DateTimeOffset.TryParse(lastRunRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsedLastRun);

    var lastRun = parsedLastRunOk ? parsedLastRun.ToString(lastRunFormat, CultureInfo.InvariantCulture) : lastRunFallback;
    log.Debug($"Last run: {lastRun}");
    var syncStartTime = DateTimeOffset.Now;

    // init authentication
    var authUri = config.Auth.Uri;
    var authClientId = config.Auth.ClientId;
    var authClientSecret = config.Auth.ClientSecret;
    if (string.IsNullOrWhiteSpace(authUri) || string.IsNullOrWhiteSpace(authClientId) || string.IsNullOrWhiteSpace(authClientSecret))
    {
      log.Error($"Authentication configuration invalid, please check config.yml.");
      return;
    }

    var samedisUri = config.Samedis.Uri;
    var samedisApiVersion = config.Samedis.ApiVersion;
    var samedisTenantId = config.Samedis.TenantId;

    if (string.IsNullOrWhiteSpace(samedisUri) || string.IsNullOrWhiteSpace(samedisApiVersion) || string.IsNullOrWhiteSpace(samedisTenantId))
    {
      log.Error($"Samedis configuration invalid, please check config.yml.");
      return;
    }

    // The one place that knows how a resource path is built. This sync is tenant-scoped and
    // stays that way; the paths are centralised so the call sites carry no path knowledge.
    ITenantScope scope = TenantScope.Standard(samedisTenantId, samedisApiVersion);

    var httpSettings = new HttpSettings()
    {
      Proxy = config.Http.Proxy,
      ProxyUsername = config.Http.ProxyUsername,
      ProxyPassword = config.Http.ProxyPassword,
      ValidateCertificate = config.Http.ValidCertificate,
    };

    if (!httpSettings.ValidateCertificate)
      log.Warn("WARNING: TLS certificate validation is disabled (http.valid_certificate: false). Do not use in production.");

    var samedisAuth = new Authenticate(authUri, authClientId, authClientSecret, httpSettings, log);
    log.Info($"Credential checkup Status: {samedisAuth.StatusCode} {samedisAuth.Status} User: {samedisAuth.User}");
    var bearerToken = samedisAuth.BearerToken;

    //define resource
    var samedisClient = new RequestData(samedisUri, bearerToken, httpSettings, log);

    // tenant-level settings
    var tenantSettings = Tenant.GetSettings(samedisClient, samedisApiVersion, samedisTenantId, log);
    var useExtendedDeviceLocations = tenantSettings.UseExtendedDeviceLocations;
    var useProfitCenters = tenantSettings.UseProfitCenters;
    var locationMode = useExtendedDeviceLocations ? "property" : "standard";
    log.Info($"Tenant settings loaded. TenantId: {tenantSettings.TenantId} Name: {tenantSettings.Name} LocationMode: {locationMode} use_profit_centers: {useProfitCenters}");

    // list settings
    var pageSize = 250; // max 250

    var defaultDownloadRoot = Path.Combine("data", "from_samedis");
    var defaultUploadRoot = Path.Combine("data", "to_samedis");
    var downloadRoot = string.IsNullOrWhiteSpace(config.Paths?.FromSamedis) ? defaultDownloadRoot : config.Paths.FromSamedis.Trim();
    var uploadRoot = string.IsNullOrWhiteSpace(config.Paths?.ToSamedis) ? defaultUploadRoot : config.Paths.ToSamedis.Trim();
    log.Debug($"Data paths: from_samedis='{downloadRoot}', to_samedis='{uploadRoot}'");

    // clean up download folder only, keep upload folder for import procedures
    if (Directory.Exists(downloadRoot))
      Directory.Delete(downloadRoot, true);
    Directory.CreateDirectory(downloadRoot);
    Directory.CreateDirectory(uploadRoot);
    #endregion

    #region Inventories Upload
    if (!config.Sync.InventoriesUpload)
    {
      log.Info("Inventories Upload sync disabled in config.yml");
    }
    else
    {
      log.Info("Inventories Upload sync starting.");

      var inventoryResource = scope.Resource("inventories");
      var inventoryWriteResource = inventoryResource + "?locale=en";
      var departmentsResource = scope.Resource("departments");
      var profitCentersResource = scope.Resource("profit_centers");
      var propertiesResource = scope.Resource("properties");
      var buildingsResource = scope.Resource("buildings");
      var floorsResource = scope.Resource("floors");
      var locationsResource = scope.Resource("device_locations");
      var deviceModelsResource = scope.Resource("device_models");
      var deviceTypesResource = scope.Resource("device_types");
      var contactsResource = scope.Resource("contacts");
      var issuesResource = scope.Resource("issues");
      var createLocalDeviceModelsOnInventoryLookup = config.Sync.CreateLocalDeviceModelsOnInventoryLookup;
      var resolveServicePartnerCompany = config.Sync.InventoriesUploadResolveServicePartnerCompany;
      var inventoryCsvPath = Path.Combine(uploadRoot, "inventories.csv");
      var departmentsCsvPath = Path.Combine(uploadRoot, "departments.csv");

      RequireAccess(log, samedisClient, inventoryResource);
      RequireAccess(log, samedisClient, departmentsResource);
      RequireAccess(log, samedisClient, locationsResource);
      RequireAccess(log, samedisClient, deviceModelsResource);
      if (createLocalDeviceModelsOnInventoryLookup)
      {
        RequireAccess(log, samedisClient, deviceTypesResource);
        RequireAccess(log, samedisClient, contactsResource);
      }
      if (useExtendedDeviceLocations)
      {
        RequireAccess(log, samedisClient, propertiesResource);
        RequireAccess(log, samedisClient, buildingsResource);
        RequireAccess(log, samedisClient, floorsResource);
      }

      if (!File.Exists(inventoryCsvPath) || Files.IsEffectivelyEmpty(inventoryCsvPath))
      {
        log.Info($"Inventories Upload skipped: no data in CSV ({inventoryCsvPath}).");
      }
      else
      {
        DataTable uploadTable;
        try
        {
          uploadTable = Csv.Read(inventoryCsvPath, tableName: "InventoriesUpload", trimFields: true);
        }
        catch (Exception ex)
        {
          log.Error($"Inventories Upload failed to read CSV {inventoryCsvPath}: {ex.Message}");
          uploadTable = new DataTable("InventoriesUpload");
        }

        var requiredColumns = new[]
        {
          "inventory_number"
        };

        if (uploadTable.Rows.Count == 0)
        {
          log.Warn("Inventories Upload skipped because CSV contains no rows.");
        }
        else if (!Csv.HasColumns(uploadTable, requiredColumns))
        {
          log.Error($"Inventories Upload skipped. CSV missing one or more required columns: {string.Join(", ", requiredColumns)}");
        }
        else
        {
          // Remembers hits and misses per key kind, which is what the three dictionaries and
          // three "already checked" sets here used to do by hand.
          var inventoryLookup = new ResourceLookup(samedisClient, inventoryResource, scope.KeyLookup);
          var departmentLookup = new ResourceLookup(samedisClient, departmentsResource, scope.KeyLookup);
          // Records which department/profit-centre pairs were already written this run.
          var syncedDepartmentProfitCenters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var profitCenterLookup = new ResourceLookup(samedisClient, profitCentersResource, scope.KeyLookup);
          // Records which department/profit-centre pairs were already linked this run.
          var linkedProfitCenterDepartments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var locationLookup = new ResourceLookup(samedisClient, locationsResource, scope.KeyLookup);
          // Not a lookup cache: this maps the SOURCE system's room id onto the samedis id,
          // filled by the property-mode pre-sync pass. The source key is not an ObjectId, so
          // it has no place in ResourceLookup.
          var roomIdBySourceId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var propertyLookup = new ResourceLookup(samedisClient, propertiesResource, scope.KeyLookup);
          var buildingLookup = new ResourceLookup(samedisClient, buildingsResource, scope.KeyLookup);
          var floorLookup = new ResourceLookup(samedisClient, floorsResource, scope.KeyLookup);
          var deviceModelLookup = new ResourceLookup(samedisClient, deviceModelsResource, scope.KeyLookup);
          var deviceTypeLookup = new ResourceLookup(samedisClient, deviceTypesResource, scope.KeyLookup);
          var manufacturerLookup = new ResourceLookup(samedisClient, contactsResource, scope.KeyLookup);
          // Keyed by the source's own title/manufacturer/device-type combination, before any
          // of them are resolved to ids, so an unresolvable row is not retried per row.
          var tenantDeviceModelBySourceKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

          var sourceLocationCsvFiles = Directory.Exists(uploadRoot)
            ? Directory.GetFiles(uploadRoot, "*.csv")
            : Array.Empty<string>();

          var sourceBuildingsCsvPath = sourceLocationCsvFiles.FirstOrDefault(path =>
            Path.GetFileName(path).Equals("buildings.csv", StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(path).StartsWith("StandorteGeba", StringComparison.OrdinalIgnoreCase));
          var sourceFloorsCsvPath = sourceLocationCsvFiles.FirstOrDefault(path =>
            Path.GetFileName(path).Equals("floors.csv", StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(path).StartsWith("StandorteEbe", StringComparison.OrdinalIgnoreCase));
          var sourceRoomsCsvPath = sourceLocationCsvFiles.FirstOrDefault(path =>
            Path.GetFileName(path).Equals("rooms.csv", StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(path).StartsWith("StandorteRau", StringComparison.OrdinalIgnoreCase));

          var sourceBuildings = Buildings.LoadSourceBuildings(sourceBuildingsCsvPath ?? string.Empty, log);
          var sourceFloors = Floors.LoadSourceFloors(sourceFloorsCsvPath ?? string.Empty, log);
          var sourceRooms = Locations.LoadSourceRooms(sourceRoomsCsvPath ?? string.Empty, log);
          var roomPlaceholderTitle = string.IsNullOrWhiteSpace(config.Sync.LocationsRoomPlaceholder)
            ? "Keine Raumzuordnung"
            : config.Sync.LocationsRoomPlaceholder.Trim();
          var floorPlaceholderTitle = string.IsNullOrWhiteSpace(config.Sync.LocationsFloorPlaceholder)
            ? "Keine Ebenenzuordnung"
            : config.Sync.LocationsFloorPlaceholder.Trim();
          var hasDepartmentNotesColumn = uploadTable.Columns.Contains("notes");
          var hasDepartmentProfitCenterColumn =
            uploadTable.Columns.Contains("profit_center") ||
            uploadTable.Columns.Contains("wirtschaftende_einheit");
          var createStandardLocationsOnTheFly = !useExtendedDeviceLocations && config.Sync.InventoriesUploadCreateLocationsOnTheFly;
          var createPropertyHierarchyOnImport = useExtendedDeviceLocations;
          string? propertyIdForHierarchySync = null;

          if (useExtendedDeviceLocations)
          {
            if (config.Sync.InventoriesUploadCreateLocationsOnTheFly)
            {
              log.Info("Property mode: sync.inventories_upload_create_locations_on_the_fly is only used in standard mode. Property mode uses hierarchy pre-sync and row-level location assignment resolves references only.");
            }

            // Resolve the property up front regardless of whether buildings/floors/rooms CSV
            // data is present. The on-the-fly placeholder-location creation in the floor- and
            // building-fallback paths (further down in the row loop) requires a valid
            // property_id; without it the API rejects the POST with HTTP 400.
            var propertyTitle = string.IsNullOrWhiteSpace(tenantSettings.Name) ? "Default Property" : tenantSettings.Name;
            propertyIdForHierarchySync = Properties.ResolvePropertyId(
              samedisClient,
              propertiesResource,
              propertyTitle,
              createPropertyHierarchyOnImport,
              propertyLookup,
              log
            );

            if (string.IsNullOrWhiteSpace(propertyIdForHierarchySync))
            {
              log.Warn($"Property mode: property '{propertyTitle}' could not be resolved/created. On-the-fly placeholder locations will not work for floor-/building-only references.");
            }

            if (sourceBuildings.Count == 0 && sourceFloors.Count == 0 && sourceRooms.Count == 0)
            {
              log.Warn("Property mode hierarchy pre-sync skipped because no buildings/floors/rooms CSV data was found.");
            }
            else if (string.IsNullOrWhiteSpace(propertyIdForHierarchySync))
            {
              log.Warn($"Property mode hierarchy pre-sync skipped because property '{propertyTitle}' could not be resolved/created.");
            }
            else
            {
              log.Info($"Property mode hierarchy pre-sync starting. Source buildings: {sourceBuildings.Count}, floors: {sourceFloors.Count}, rooms: {sourceRooms.Count}");

                var sourceBuildingToApiId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var sourceFloorToApiId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var sourceFloorToBuildingApiId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var buildingsResolved = 0;
                var buildingsUnresolved = 0;
                var buildingsSkippedNoTitle = 0;

                foreach (var sourceBuilding in sourceBuildings.Values.OrderBy(item => item.SourceId, StringComparer.OrdinalIgnoreCase))
                {
                  if (string.IsNullOrWhiteSpace(sourceBuilding.Title))
                  {
                    buildingsSkippedNoTitle++;
                    continue;
                  }

                  var resolvedBuildingId = Buildings.ResolveBuildingId(
                    samedisClient,
                    buildingsResource,
                    propertyIdForHierarchySync,
                    sourceBuilding.Title,
                    createPropertyHierarchyOnImport,
                    sourceBuilding.SourceId,
                    sourceBuilding.Title,
                    buildingLookup,
                    log,
                    sourceBuilding.SourceId,
                    sourceBuilding.Street,
                    sourceBuilding.Zip,
                    sourceBuilding.Town,
                    true
                  );

                  if (string.IsNullOrWhiteSpace(resolvedBuildingId))
                  {
                    buildingsUnresolved++;
                    continue;
                  }

                  sourceBuildingToApiId[sourceBuilding.SourceId] = resolvedBuildingId;
                  buildingsResolved++;
                }

                var floorsResolved = 0;
                var floorsUnresolved = 0;
                var floorsMissingBuildingParent = 0;
                var floorsSkippedNoTitle = 0;

                foreach (var sourceFloor in sourceFloors.Values.OrderBy(item => item.SourceId, StringComparer.OrdinalIgnoreCase))
                {
                  if (string.IsNullOrWhiteSpace(sourceFloor.SourceBuildingId) || !sourceBuildingToApiId.TryGetValue(sourceFloor.SourceBuildingId, out var parentBuildingId))
                  {
                    floorsMissingBuildingParent++;
                    continue;
                  }

                  if (string.IsNullOrWhiteSpace(sourceFloor.Title))
                  {
                    floorsSkippedNoTitle++;
                    continue;
                  }

                  var resolvedFloorId = Floors.ResolveFloorId(
                    samedisClient,
                    floorsResource,
                    parentBuildingId,
                    sourceFloor.Title,
                    createPropertyHierarchyOnImport,
                    sourceFloor.SourceId,
                    sourceFloor.Title,
                    floorLookup,
                    log,
                    sourceFloor.SourceId,
                    true
                  );

                  if (string.IsNullOrWhiteSpace(resolvedFloorId))
                  {
                    floorsUnresolved++;
                    continue;
                  }

                  sourceFloorToApiId[sourceFloor.SourceId] = resolvedFloorId;
                  sourceFloorToBuildingApiId[sourceFloor.SourceId] = parentBuildingId;
                  floorsResolved++;
                }

                var roomsResolved = 0;
                var roomsUnresolved = 0;
                var roomsMissingFloorParent = 0;
                var roomsSkippedNoTitle = 0;

                foreach (var sourceRoom in sourceRooms.Values.OrderBy(item => item.SourceId, StringComparer.OrdinalIgnoreCase))
                {
                  if (string.IsNullOrWhiteSpace(sourceRoom.SourceFloorId) || !sourceFloorToApiId.TryGetValue(sourceRoom.SourceFloorId, out var parentFloorId))
                  {
                    roomsMissingFloorParent++;
                    continue;
                  }

                  if (string.IsNullOrWhiteSpace(sourceRoom.Title))
                  {
                    roomsSkippedNoTitle++;
                    continue;
                  }

                  sourceFloorToBuildingApiId.TryGetValue(sourceRoom.SourceFloorId, out var parentBuildingId);
                  var roomNotes = string.IsNullOrWhiteSpace(sourceRoom.PlisCode) ? string.Empty : $"PLIS Code: {sourceRoom.PlisCode.Trim()}";

                  var resolvedRoomId = Locations.ResolveLocationId(
                    samedisClient,
                    locationsResource,
                    string.Empty,
                    sourceRoom.Title,
                    createPropertyHierarchyOnImport,
                    sourceRoom.SourceId,
                    sourceRoom.Title,
                    locationLookup,
                    log,
                    propertyIdForHierarchySync,
                    parentBuildingId,
                    parentFloorId,
                    roomNotes,
                    sourceRoom.SourceId,
                    true
                  );

                  if (string.IsNullOrWhiteSpace(resolvedRoomId))
                  {
                    roomsUnresolved++;
                    continue;
                  }

                  roomIdBySourceId[sourceRoom.SourceId] = resolvedRoomId;
                  roomsResolved++;
                }

                log.Info($"Property mode hierarchy pre-sync finished. Buildings resolved: {buildingsResolved}, unresolved: {buildingsUnresolved}, missing title: {buildingsSkippedNoTitle}.");
                log.Info($"Property mode hierarchy pre-sync finished. Floors resolved: {floorsResolved}, unresolved: {floorsUnresolved}, missing building parent: {floorsMissingBuildingParent}, missing title: {floorsSkippedNoTitle}.");
                log.Info($"Property mode hierarchy pre-sync finished. Rooms resolved: {roomsResolved}, unresolved: {roomsUnresolved}, missing floor parent: {roomsMissingFloorParent}, missing title: {roomsSkippedNoTitle}.");
            }
          }
          else if (sourceBuildings.Count > 0 || sourceFloors.Count > 0 || sourceRooms.Count > 0)
          {
            log.Warn("Tenant location mode is standard. buildings/floors/rooms CSV data was detected but hierarchy pre-sync is skipped.");
          }

          if (File.Exists(departmentsCsvPath))
          {
            DataTable departmentsTable;
            try
            {
              departmentsTable = Csv.Read(departmentsCsvPath, tableName: "DepartmentsUpload", trimFields: true);
            }
            catch (Exception ex)
            {
              log.Warn($"Departments preload failed to read CSV {departmentsCsvPath}: {ex.Message}");
              departmentsTable = new DataTable("DepartmentsUpload");
            }

            if (departmentsTable.Rows.Count > 0)
            {
              var hasDepartmentCsvNotesColumn = departmentsTable.Columns.Contains("notes");
              var hasDepartmentCsvProfitCenterColumn =
                departmentsTable.Columns.Contains("profit_center") ||
                departmentsTable.Columns.Contains("wirtschaftende_einheit");
              log.Info($"Departments preload source rows: {departmentsTable.Rows.Count}");

              foreach (DataRow departmentRow in departmentsTable.Rows)
              {
                var departmentRowId = Rows.Value(departmentRow, "id");
                var departmentApiIdFromCsv = Rows.Value(departmentRow, "department_id");
                var departmentCostCenterFromCsv = Rows.Value(departmentRow, "cost_center_number");

                var departmentTitleFromCsv = Rows.Value(departmentRow, "department");
                if (string.IsNullOrWhiteSpace(departmentTitleFromCsv))
                  departmentTitleFromCsv = Rows.Value(departmentRow, "cost_center_description");
                if (string.IsNullOrWhiteSpace(departmentTitleFromCsv))
                  departmentTitleFromCsv = Rows.Value(departmentRow, "abteilung");

                var departmentNotesFromCsv = string.Empty;
                if (hasDepartmentCsvNotesColumn)
                {
                  departmentNotesFromCsv = Rows.Value(departmentRow, "notes");
                }

                var departmentProfitCenterTitle = string.Empty;
                if (useProfitCenters)
                {
                  if (hasDepartmentCsvProfitCenterColumn)
                    departmentProfitCenterTitle = Rows.Value(departmentRow, "profit_center");
                  if (string.IsNullOrWhiteSpace(departmentProfitCenterTitle) && departmentsTable.Columns.Contains("wirtschaftende_einheit"))
                    departmentProfitCenterTitle = Rows.Value(departmentRow, "wirtschaftende_einheit");
                }
                var departmentProfitCenterId = string.Empty;

                if (!string.IsNullOrWhiteSpace(departmentProfitCenterTitle))
                {
                  departmentProfitCenterId = ProfitCenters.ResolveProfitCenterId(
                    samedisClient,
                    profitCentersResource,
                    departmentProfitCenterTitle,
                    config.Sync.InventoriesUploadCreateDepartmentsOnTheFly,
                    departmentRowId,
                    departmentTitleFromCsv,
                    profitCenterLookup,
                    log
                  ) ?? string.Empty;

                  if (string.IsNullOrWhiteSpace(departmentProfitCenterId))
                  {
                    log.Warn($"Departments preload: profit center '{departmentProfitCenterTitle}' could not be resolved/created (department_title='{departmentTitleFromCsv}', cost_center_number='{departmentCostCenterFromCsv}', source_id='{departmentRowId}'). Department will be synced without profit center.");
                    departmentProfitCenterTitle = string.Empty;
                  }
                }

                if (string.IsNullOrWhiteSpace(departmentTitleFromCsv) && string.IsNullOrWhiteSpace(departmentCostCenterFromCsv))
                  continue;

                var preloadedDepartmentId = Departments.ResolveDepartmentId(
                  samedisClient,
                  departmentsResource,
                  departmentApiIdFromCsv,
                  departmentCostCenterFromCsv,
                  departmentTitleFromCsv,
                  departmentNotesFromCsv,
                  config.Sync.InventoriesUploadCreateDepartmentsOnTheFly,
                  departmentRowId,
                  departmentTitleFromCsv,
                  departmentLookup,
                  syncedDepartmentProfitCenters,
                  log,
                  departmentProfitCenterTitle
                );

                if (string.IsNullOrWhiteSpace(preloadedDepartmentId))
                {
                  log.Warn($"Departments preload: could not resolve/create department (title='{departmentTitleFromCsv}', cost_center_number='{departmentCostCenterFromCsv}', source_id='{departmentRowId}').");
                }
                else if (!string.IsNullOrWhiteSpace(departmentProfitCenterId))
                {
                  ProfitCenters.EnsureDepartmentAssigned(
                    samedisClient,
                    profitCentersResource,
                    departmentProfitCenterId,
                    preloadedDepartmentId,
                    linkedProfitCenterDepartments,
                    log
                  );
                }
              }
            }
          }

          log.Info($"Inventories Upload source rows: {uploadTable.Rows.Count}");
          log.Info($"Inventories Upload location mode: {(useExtendedDeviceLocations ? "property (building/floor/room)" : "standard (room only)")}");

          var createdCount = 0;
          var updatedCount = 0;
          var skippedCount = 0;
          var errorCount = 0;
          var recommissionedCount = 0;
          var retiredCount = 0;

          foreach (DataRow row in uploadTable.Rows)
          {
            var rowId = Rows.Value(row, "id");
            var inventoryTitle = Rows.Value(row, "title");
            if (string.IsNullOrWhiteSpace(inventoryTitle))
              inventoryTitle = Rows.Value(row, "device_model_title");

            var inventoryNumber = Rows.Value(row, "inventory_number");
            var inventoryExternalId = Rows.Value(row, "external_id");
            var departmentCostCenterNumber = Rows.Value(row, "cost_center_number");
            var departmentTitle = Rows.Value(row, "department");
            if (string.IsNullOrWhiteSpace(departmentTitle))
              departmentTitle = Rows.Value(row, "cost_center_description");
            var departmentNotes = hasDepartmentNotesColumn ? Rows.Value(row, "notes") : string.Empty;
            var departmentProfitCenterTitle = string.Empty;
            if (useProfitCenters && hasDepartmentProfitCenterColumn)
            {
              departmentProfitCenterTitle = Rows.Value(row, "profit_center");
              if (string.IsNullOrWhiteSpace(departmentProfitCenterTitle))
                departmentProfitCenterTitle = Rows.Value(row, "wirtschaftende_einheit");
            }
            var departmentProfitCenterId = string.Empty;

            var locationTitle = Rows.Value(row, "location");

            var operationStatus = Rows.Value(row, "operation_status");
            var sourceBuildingTitle = string.Empty;
            var sourceFloorTitle = string.Empty;
            var sourceRoomTitle = string.Empty;
            var sourceLocationId = Rows.Value(row, "source_location_id");
            var sourceLocationType = Rows.Value(row, "source_location_type");
            var normalizedSourceLocationType = sourceLocationType.Trim().ToLowerInvariant();
            Buildings.SourceBuilding? resolvedSourceBuilding = null;
            Floors.SourceFloor? resolvedSourceFloor = null;
            Locations.SourceRoom? resolvedSourceRoom = null;
            var sourceLocationResolved = false;
            var catalogId = Rows.Value(row, "catalog_id");
            var lookupTitle = inventoryTitle;
            if (string.IsNullOrWhiteSpace(lookupTitle))
              lookupTitle = Rows.Value(row, "device_model_title");

            var lookupManufacturer = Rows.Value(row, "manufacturer");
            if (string.IsNullOrWhiteSpace(lookupManufacturer))
              lookupManufacturer = Rows.Value(row, "responsible_manufacturer");
            if (string.IsNullOrWhiteSpace(lookupManufacturer))
              lookupManufacturer = Rows.Value(row, "company");

            var lookupDeviceTypeTitle = Rows.Value(row, "device_type_title");
            var isPlaceholderDeviceModel = Inventories.IsPlaceholderDeviceModel(row);

            if (!string.IsNullOrWhiteSpace(sourceLocationId))
            {
              Buildings.SourceBuilding? resolvedBuilding;
              Floors.SourceFloor? resolvedFloor;
              Locations.SourceRoom? resolvedRoom;

              if (normalizedSourceLocationType.Contains("raum") && sourceRooms.TryGetValue(sourceLocationId, out resolvedRoom))
              {
                resolvedSourceRoom = resolvedRoom;
                sourceLocationResolved = true;
                if (!string.IsNullOrWhiteSpace(resolvedSourceRoom.SourceFloorId))
                {
                  if (sourceFloors.TryGetValue(resolvedSourceRoom.SourceFloorId, out resolvedFloor))
                    resolvedSourceFloor = resolvedFloor;
                  if (resolvedSourceFloor != null && !string.IsNullOrWhiteSpace(resolvedSourceFloor.SourceBuildingId) && sourceBuildings.TryGetValue(resolvedSourceFloor.SourceBuildingId, out resolvedBuilding))
                    resolvedSourceBuilding = resolvedBuilding;
                }
              }
              else if (normalizedSourceLocationType.Contains("ebene") && sourceFloors.TryGetValue(sourceLocationId, out resolvedFloor))
              {
                resolvedSourceFloor = resolvedFloor;
                sourceLocationResolved = true;
                if (!string.IsNullOrWhiteSpace(resolvedSourceFloor.SourceBuildingId) && sourceBuildings.TryGetValue(resolvedSourceFloor.SourceBuildingId, out resolvedBuilding))
                  resolvedSourceBuilding = resolvedBuilding;
              }
              else if (normalizedSourceLocationType.Contains("geb") && sourceBuildings.TryGetValue(sourceLocationId, out resolvedBuilding))
              {
                resolvedSourceBuilding = resolvedBuilding;
                sourceLocationResolved = true;
              }
              else
              {
                if (sourceRooms.TryGetValue(sourceLocationId, out resolvedRoom))
                {
                  resolvedSourceRoom = resolvedRoom;
                  sourceLocationResolved = true;
                  if (!string.IsNullOrWhiteSpace(resolvedSourceRoom.SourceFloorId) && sourceFloors.TryGetValue(resolvedSourceRoom.SourceFloorId, out resolvedFloor))
                  {
                    resolvedSourceFloor = resolvedFloor;
                    if (!string.IsNullOrWhiteSpace(resolvedSourceFloor.SourceBuildingId) && sourceBuildings.TryGetValue(resolvedSourceFloor.SourceBuildingId, out resolvedBuilding))
                      resolvedSourceBuilding = resolvedBuilding;
                  }
                }
                else if (sourceFloors.TryGetValue(sourceLocationId, out resolvedFloor))
                {
                  resolvedSourceFloor = resolvedFloor;
                  sourceLocationResolved = true;
                  if (!string.IsNullOrWhiteSpace(resolvedSourceFloor.SourceBuildingId) && sourceBuildings.TryGetValue(resolvedSourceFloor.SourceBuildingId, out resolvedBuilding))
                    resolvedSourceBuilding = resolvedBuilding;
                }
                else if (sourceBuildings.TryGetValue(sourceLocationId, out resolvedBuilding))
                {
                  resolvedSourceBuilding = resolvedBuilding;
                  sourceLocationResolved = true;
                }
              }

              if (string.IsNullOrWhiteSpace(sourceBuildingTitle))
                sourceBuildingTitle = resolvedSourceBuilding?.Title ?? string.Empty;
              if (string.IsNullOrWhiteSpace(sourceFloorTitle))
                sourceFloorTitle = resolvedSourceFloor?.Title ?? string.Empty;
              if (string.IsNullOrWhiteSpace(sourceRoomTitle))
                sourceRoomTitle = resolvedSourceRoom?.Title ?? string.Empty;
            }

            var isRetiredRow = Inventories.IsRetiredOperationStatus(operationStatus);
            var targetInventoryId = Inventories.ResolveExistingInventoryId(
              inventoryLookup,
              rowId,
              inventoryExternalId,
              inventoryNumber,
              config.Sync.InventoriesUploadFallbackByDeviceNumber
            );
            var isCreateOperation = string.IsNullOrWhiteSpace(targetInventoryId);

            if (isRetiredRow && !string.IsNullOrWhiteSpace(targetInventoryId))
            {
              // Existing device that is retired ("Ausgemustert") in the source CSV.
              // Retirement must go through a device_retired ISSUE -- retirement_date is
              // read-only and a status-only PUT would leave the device non-recommissionable.
              // The issue lets the backend set retirement_date and keeps the device
              // reversible later via recommission_device. We do NOT run the normal
              // update PUT for retired rows (it would be rejected with "Device retired."
              // anyway), so we handle it here and move on to the next row.

              // Idempotency: if the device is already retired in samedis, do nothing --
              // posting another device_retired issue would just accumulate duplicate
              // done issues without changing anything.
              if (Inventories.IsInventoryDeviceRetired(samedisClient, inventoryResource, targetInventoryId))
              {
                skippedCount++;
                log.Debug($"Inventory already retired in samedis, no device_retired issue needed (id='{targetInventoryId}', inventory_number='{inventoryNumber}').");
                continue;
              }

              var retirementDateForIssue = Helper.NormalizeDate(Rows.Value(row, "retirement_date"));
              var retiredResponse = Inventories.PostDeviceRetiredIssue(
                samedisClient,
                issuesResource,
                targetInventoryId,
                inventoryNumber,
                inventoryTitle,
                retirementDateForIssue,
                log
              );

              if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300)
              {
                retiredCount++;
                updatedCount++;
                log.Debug($"Inventory retired via device_retired issue (id='{targetInventoryId}', inventory_number='{inventoryNumber}', title='{inventoryTitle}').");
              }
              else if (Inventories.IsAlreadyRetiredError(retiredResponse))
              {
                // Already retired in samedis -- nothing to do, no error.
                skippedCount++;
                log.Debug($"Inventory already retired in samedis, no device_retired issue needed (id='{targetInventoryId}', inventory_number='{inventoryNumber}').");
              }
              else
              {
                errorCount++;
                log.Error($"Failed to retire inventory via device_retired issue (id='{targetInventoryId}', title='{inventoryTitle}', inventory_number='{inventoryNumber}', status={samedisClient.StatusCode}). Response: {retiredResponse}");
              }

              continue;
            }

            // Retired row whose device does NOT exist in samedis: it is created here
            // like any other device, but ACTIVE (the backend rejects creating a device
            // directly in retired state). After a successful create it is retired
            // properly via a device_retired issue -- see the create-success handling.

            if (string.IsNullOrWhiteSpace(catalogId))
            {
              if (isPlaceholderDeviceModel)
              {
                log.Debug($"Placeholder device model row detected. Skipping catalog/device-model lookup and local model/type/manufacturer creation (inventory_number='{inventoryNumber}', title='{lookupTitle}').");
              }
              else
              {
                catalogId = DeviceModels.ResolveCatalogId(
                  deviceModelLookup,
                  lookupTitle,
                  lookupManufacturer
                ) ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(catalogId))
                {
                  log.Debug($"Resolved catalog_id '{catalogId}' via device model lookup (title='{lookupTitle}', manufacturer='{lookupManufacturer}').");
                }
                else if (createLocalDeviceModelsOnInventoryLookup)
                {
                  catalogId = DeviceModels.ResolveOrCreateTenantCatalogIdForInventory(
                    samedisClient,
                    samedisTenantId,
                    lookupTitle,
                    lookupManufacturer,
                    lookupDeviceTypeTitle,
                    deviceModelLookup,
                    deviceTypeLookup,
                    manufacturerLookup,
                    tenantDeviceModelBySourceKey,
                    log,
                    rowId,
                    inventoryNumber
                  ) ?? string.Empty;

                  if (!string.IsNullOrWhiteSpace(catalogId))
                  {
                    log.Debug($"Resolved catalog_id '{catalogId}' via local tenant device model lookup/create (title='{lookupTitle}', manufacturer='{lookupManufacturer}', device_type_title='{lookupDeviceTypeTitle}', inventory_number='{inventoryNumber}').");
                  }
                  else if (!string.IsNullOrWhiteSpace(lookupTitle))
                  {
                    log.Warn($"No device model match found and local tenant device model creation failed/skipped (title='{lookupTitle}', manufacturer='{lookupManufacturer}', device_type_title='{lookupDeviceTypeTitle}', inventory_number='{inventoryNumber}').");
                  }
                }
                else if (!string.IsNullOrWhiteSpace(lookupTitle))
                {
                  log.Warn($"No device model match found for catalog lookup (title='{lookupTitle}', manufacturer='{lookupManufacturer}', inventory_number='{inventoryNumber}').");
                }
              }
            }

            if (!string.IsNullOrWhiteSpace(departmentProfitCenterTitle))
            {
              departmentProfitCenterId = ProfitCenters.ResolveProfitCenterId(
                samedisClient,
                profitCentersResource,
                departmentProfitCenterTitle,
                config.Sync.InventoriesUploadCreateDepartmentsOnTheFly,
                rowId,
                inventoryTitle,
                profitCenterLookup,
                log
              ) ?? string.Empty;

              if (string.IsNullOrWhiteSpace(departmentProfitCenterId))
              {
                log.Warn($"Profit center '{departmentProfitCenterTitle}' could not be resolved/created for inventory row (id='{rowId}', inventory_number='{inventoryNumber}'). Department will be synced without profit center.");
                departmentProfitCenterTitle = string.Empty;
              }
            }

            var departmentId = Departments.ResolveDepartmentId(
              samedisClient,
              departmentsResource,
              Rows.Value(row, "department_id"),
              departmentCostCenterNumber,
              departmentTitle,
              departmentNotes,
              config.Sync.InventoriesUploadCreateDepartmentsOnTheFly,
              rowId,
              inventoryTitle,
              departmentLookup,
              syncedDepartmentProfitCenters,
              log,
              departmentProfitCenterTitle
            );

            if ((!string.IsNullOrWhiteSpace(departmentTitle) || !string.IsNullOrWhiteSpace(departmentCostCenterNumber)) && string.IsNullOrWhiteSpace(departmentId))
            {
              log.Warn($"Department could not be resolved/created (title='{departmentTitle}', cost_center_number='{departmentCostCenterNumber}', id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without department reference.");
            }
            else if (!string.IsNullOrWhiteSpace(departmentId) && !string.IsNullOrWhiteSpace(departmentProfitCenterId))
            {
              ProfitCenters.EnsureDepartmentAssigned(
                samedisClient,
                profitCentersResource,
                departmentProfitCenterId,
                departmentId,
                linkedProfitCenterDepartments,
                log
              );
            }

            string? locationId = null;
            if (useExtendedDeviceLocations)
            {
              if (string.IsNullOrWhiteSpace(sourceLocationId))
              {
                // A completely empty source_location_id is an expected normal case
                // (~35% of rows have no location at all), not a problem -- log it at
                // debug level instead of flooding the WARN log.
                log.Debug($"Property mode: source_location_id is missing for inventory row (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
              }
              else if (!sourceLocationResolved)
              {
                var resolvedByExternalId = false;

                var roomByExternalId = Locations.ResolveLocationId(
                  samedisClient,
                  locationsResource,
                  string.Empty,
                  string.Empty,
                  false,
                  rowId,
                  inventoryTitle,
                  locationLookup,
                  log,
                  null,
                  null,
                  null,
                  null,
                  sourceLocationId
                );
                if (!string.IsNullOrWhiteSpace(roomByExternalId))
                {
                  locationId = roomByExternalId;
                  resolvedByExternalId = true;
                }

                string? floorByExternalId = null;
                if (!resolvedByExternalId)
                {
                  floorByExternalId = Floors.ResolveFloorId(
                    samedisClient,
                    floorsResource,
                    string.Empty,
                    string.Empty,
                    false,
                    rowId,
                    inventoryTitle,
                    floorLookup,
                    log,
                    sourceLocationId
                  );
                  if (!string.IsNullOrWhiteSpace(floorByExternalId))
                  {
                    // createOnTheFly: when only the floor matches via external_id we still need
                    // a room to attach the inventory to. Create the "Keine Raumzuordnung"
                    // placeholder under that floor on demand (same flag as the hierarchy pre-sync).
                    locationId = Locations.ResolveLocationId(
                      samedisClient,
                      locationsResource,
                      string.Empty,
                      roomPlaceholderTitle,
                      createPropertyHierarchyOnImport,
                      rowId,
                      inventoryTitle,
                      locationLookup,
                      log,
                      propertyIdForHierarchySync,
                      null,
                      floorByExternalId
                    );
                    resolvedByExternalId = !string.IsNullOrWhiteSpace(locationId);
                  }
                }

                if (!resolvedByExternalId)
                {
                  var buildingByExternalId = Buildings.ResolveBuildingId(
                    samedisClient,
                    buildingsResource,
                    propertyIdForHierarchySync ?? string.Empty,
                    string.Empty,
                    false,
                    rowId,
                    inventoryTitle,
                    buildingLookup,
                    log,
                    sourceLocationId
                  );
                  if (!string.IsNullOrWhiteSpace(buildingByExternalId))
                  {
                    // createOnTheFly: when only the building matches via external_id we have
                    // neither a floor nor a room to attach the inventory to. Rooms live below
                    // floors, so create the "Keine Ebenenzuordnung" placeholder floor under that
                    // building first, then the "Keine Raumzuordnung" placeholder room under that
                    // floor on demand and assign the inventory to it.
                    var buildingFloorPlaceholderId = Floors.ResolveFloorId(
                      samedisClient,
                      floorsResource,
                      buildingByExternalId,
                      floorPlaceholderTitle,
                      createPropertyHierarchyOnImport,
                      rowId,
                      inventoryTitle,
                      floorLookup,
                      log
                    );
                    if (!string.IsNullOrWhiteSpace(buildingFloorPlaceholderId))
                    {
                      locationId = Locations.ResolveLocationId(
                        samedisClient,
                        locationsResource,
                        string.Empty,
                        roomPlaceholderTitle,
                        createPropertyHierarchyOnImport,
                        rowId,
                        inventoryTitle,
                        locationLookup,
                        log,
                        propertyIdForHierarchySync,
                        buildingByExternalId,
                        buildingFloorPlaceholderId
                      );
                      resolvedByExternalId = !string.IsNullOrWhiteSpace(locationId);
                    }
                    else
                    {
                      log.Warn($"Property mode: building matched via external_id '{sourceLocationId}' but the placeholder floor '{floorPlaceholderTitle}' could not be created/resolved (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
                    }
                  }
                }

                if (resolvedByExternalId)
                {
                  log.Debug($"Property mode: resolved source_location_id '{sourceLocationId}' via API external_id lookup (id='{rowId}', inventory_number='{inventoryNumber}').");
                  goto SkipPropertyLocationAssignment;
                }

                log.Warn($"Property mode: source_location_id '{sourceLocationId}' could not be mapped from CSV or resolved by API external_id (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
              }
              else
              {
                var roomTitle = string.IsNullOrWhiteSpace(sourceRoomTitle) ? locationTitle : sourceRoomTitle;
                var roomIdFromCsv = string.Empty;
                var roomNotes = string.Empty;
                var isBuildingSourceReference =
                  normalizedSourceLocationType.Contains("geb") ||
                  (!string.IsNullOrWhiteSpace(sourceBuildingTitle) &&
                   string.IsNullOrWhiteSpace(sourceFloorTitle) &&
                   string.IsNullOrWhiteSpace(sourceRoomTitle));
                var isFloorSourceReference =
                  normalizedSourceLocationType.Contains("ebene") ||
                  (!string.IsNullOrWhiteSpace(sourceFloorTitle) &&
                   string.IsNullOrWhiteSpace(sourceRoomTitle));
                var isRoomSourceReference =
                  normalizedSourceLocationType.Contains("raum") ||
                  !string.IsNullOrWhiteSpace(sourceRoomTitle);

                if (isRoomSourceReference && resolvedSourceRoom == null && (resolvedSourceFloor != null || resolvedSourceBuilding != null))
                {
                  var resolvedAs = resolvedSourceFloor != null ? "floor" : "building";
                  log.Warn($"Property mode: source_location_type '{sourceLocationType}' indicates room, but source_location_id '{sourceLocationId}' maps to a {resolvedAs} in CSV hierarchy. Falling back to placeholder room handling.");
                  isRoomSourceReference = false;
                  if (resolvedSourceFloor != null)
                    isFloorSourceReference = true;
                  else
                    isBuildingSourceReference = true;
                }

                if (isRoomSourceReference && !string.IsNullOrWhiteSpace(sourceLocationId))
                  roomIdFromCsv = sourceLocationId;

                // A floor/building source location still needs a room target.
                if (!isRoomSourceReference && (isFloorSourceReference || isBuildingSourceReference))
                {
                  roomTitle = roomPlaceholderTitle;
                  roomIdFromCsv = string.Empty;
                }
                else if (resolvedSourceRoom != null && !string.IsNullOrWhiteSpace(resolvedSourceRoom.PlisCode))
                {
                  roomNotes = $"PLIS Code: {resolvedSourceRoom.PlisCode.Trim()}";
                }

                if (string.IsNullOrWhiteSpace(roomTitle) && string.IsNullOrWhiteSpace(roomIdFromCsv))
                {
                  log.Warn($"Property mode: final room title could not be determined from source_location_id '{sourceLocationId}' (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
                  goto SkipPropertyLocationAssignment;
                }
                else if (string.IsNullOrWhiteSpace(roomTitle) && !string.IsNullOrWhiteSpace(roomIdFromCsv))
                {
                  log.Debug($"Property mode: room title missing for source_location_id '{sourceLocationId}' (id='{rowId}', inventory_number='{inventoryNumber}'). Attempting room resolution by external_id only.");
                }

                if (string.IsNullOrWhiteSpace(propertyIdForHierarchySync))
                {
                  log.Warn($"Property mode: hierarchy property reference is missing (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
                  goto SkipPropertyLocationAssignment;
                }

                string? buildingId = null;
                if (!string.IsNullOrWhiteSpace(sourceBuildingTitle))
                {
                  var sourceBuildingExternalId = resolvedSourceBuilding?.SourceId ?? (isBuildingSourceReference ? sourceLocationId : string.Empty);
                  buildingId = Buildings.ResolveBuildingId(
                    samedisClient,
                    buildingsResource,
                    propertyIdForHierarchySync,
                    sourceBuildingTitle,
                    false,
                    rowId,
                    inventoryTitle,
                    buildingLookup,
                    log,
                    sourceBuildingExternalId
                  );
                  if (string.IsNullOrWhiteSpace(buildingId))
                  {
                    log.Warn($"Property mode: building '{sourceBuildingTitle}' could not be resolved in imported hierarchy (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
                    goto SkipPropertyLocationAssignment;
                  }
                }

                string? floorId = null;
                if (!string.IsNullOrWhiteSpace(sourceFloorTitle))
                {
                  var sourceFloorExternalId = resolvedSourceFloor?.SourceId ?? (isFloorSourceReference ? sourceLocationId : string.Empty);
                  if (string.IsNullOrWhiteSpace(buildingId))
                  {
                    log.Warn($"Property mode: floor '{sourceFloorTitle}' requires a resolved building from source hierarchy (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
                    goto SkipPropertyLocationAssignment;
                  }
                  else
                  {
                    floorId = Floors.ResolveFloorId(
                      samedisClient,
                      floorsResource,
                      buildingId,
                      sourceFloorTitle,
                      false,
                      rowId,
                      inventoryTitle,
                      floorLookup,
                      log,
                      sourceFloorExternalId
                    );
                    if (string.IsNullOrWhiteSpace(floorId))
                    {
                      log.Warn($"Property mode: floor '{sourceFloorTitle}' could not be resolved in imported hierarchy (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
                      goto SkipPropertyLocationAssignment;
                    }
                  }
                }

                // A building reference brings no floor with it, and a room needs one. The
                // same placeholder is created in the external_id path further up; without it
                // here, every inventory whose source location is a building ends up with no
                // location at all.
                if (string.IsNullOrWhiteSpace(floorId) && isBuildingSourceReference
                    && !string.IsNullOrWhiteSpace(buildingId))
                {
                  floorId = Floors.ResolveFloorId(
                    samedisClient,
                    floorsResource,
                    buildingId,
                    floorPlaceholderTitle,
                    createPropertyHierarchyOnImport,
                    rowId,
                    inventoryTitle,
                    floorLookup,
                    log
                  );

                  if (string.IsNullOrWhiteSpace(floorId))
                  {
                    log.Warn($"Property mode: the placeholder floor '{floorPlaceholderTitle}' could not be created below building '{buildingId}' (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
                    goto SkipPropertyLocationAssignment;
                  }

                  log.Debug($"Property mode: created/resolved placeholder floor '{floorPlaceholderTitle}' below building '{buildingId}' (id='{rowId}', inventory_number='{inventoryNumber}').");
                }

                if (!string.IsNullOrWhiteSpace(roomTitle) &&
                    isFloorSourceReference &&
                    string.IsNullOrWhiteSpace(floorId) &&
                    string.IsNullOrWhiteSpace(roomIdFromCsv))
                {
                  log.Warn($"Property mode: room '{roomTitle}' needs a resolved floor from source hierarchy (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
                  goto SkipPropertyLocationAssignment;
                }
                else if (!string.IsNullOrWhiteSpace(roomTitle) &&
                         isBuildingSourceReference &&
                         string.IsNullOrWhiteSpace(buildingId) &&
                         string.IsNullOrWhiteSpace(roomIdFromCsv))
                {
                  log.Warn($"Property mode: room '{roomTitle}' needs a resolved building from source hierarchy (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
                  goto SkipPropertyLocationAssignment;
                }
                else
                {
                  var resolveRoomByExternalOnly = !string.IsNullOrWhiteSpace(roomIdFromCsv);
                  if (resolveRoomByExternalOnly)
                  {
                    if (roomIdBySourceId.TryGetValue(roomIdFromCsv, out var mappedLocationId) && !string.IsNullOrWhiteSpace(mappedLocationId))
                    {
                      locationId = mappedLocationId;
                      log.Debug($"Property mode: resolved room from CSV/pre-sync cache by source_location_id '{roomIdFromCsv}' -> '{locationId}' (id='{rowId}', inventory_number='{inventoryNumber}').");
                    }
                    else
                    {
                      log.Debug($"Property mode: source_location_id '{roomIdFromCsv}' not found in CSV/pre-sync cache. Resolving via API external_id only (type='{sourceLocationType}', id='{rowId}', inventory_number='{inventoryNumber}').");

                      locationId = Locations.ResolveLocationId(
                        samedisClient,
                        locationsResource,
                        string.Empty,
                        string.Empty,
                        false,
                        rowId,
                        inventoryTitle,
                        locationLookup,
                        log,
                        null,
                        null,
                        null,
                        null,
                        roomIdFromCsv
                      );
                    }

                    if (string.IsNullOrWhiteSpace(locationId))
                    {
                      log.Warn($"Property mode: room lookup via source_location_id '{roomIdFromCsv}' failed (source_location_type='{sourceLocationType}', id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
                      goto SkipPropertyLocationAssignment;
                    }
                  }
                  else
                  {
                    // createPropertyHierarchyOnImport, not false: where the source location
                    // is a floor or a building, roomTitle is the placeholder room, and a
                    // lookup that may not create it can never succeed on a first import.
                    // The external_id path above already creates the same placeholder.
                    locationId = Locations.ResolveLocationId(
                      samedisClient,
                      locationsResource,
                      string.Empty,
                      roomTitle,
                      createPropertyHierarchyOnImport,
                      rowId,
                      inventoryTitle,
                      locationLookup,
                      log,
                      propertyIdForHierarchySync,
                      buildingId,
                      floorId,
                      roomNotes,
                      roomIdFromCsv
                    );
                  }

                  if (!string.IsNullOrWhiteSpace(roomTitle) && string.IsNullOrWhiteSpace(locationId))
                  {
                    log.Warn($"Property mode: room '{roomTitle}' could not be resolved in imported hierarchy (source_location_id='{sourceLocationId}', source_location_type='{sourceLocationType}', building_id='{buildingId}', floor_id='{floorId}', id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.");
                    goto SkipPropertyLocationAssignment;
                  }
                }
              }

            SkipPropertyLocationAssignment:
              ;
            }
            else
            {
              var standardLocationId = Rows.Value(row, "location_id");

              locationId = Locations.ResolveLocationId(
                samedisClient,
                locationsResource,
                standardLocationId,
                locationTitle,
                createStandardLocationsOnTheFly,
                rowId,
                inventoryTitle,
                locationLookup,
                log
              );

              if (!string.IsNullOrWhiteSpace(locationTitle) && string.IsNullOrWhiteSpace(locationId))
              {
                skippedCount++;
                log.Warn($"Skipped inventory row because location '{locationTitle}' could not be resolved/created (id='{rowId}', inventory_number='{inventoryNumber}').");
                continue;
              }
            }

            var attributes = Inventories.BuildInventoryAttributes(row, departmentId, locationId, numberFormat, catalogId, isCreateOperation);
            if (!isCreateOperation)
              attributes.Remove("comments_field");

            if (resolveServicePartnerCompany)
            {
              var servicePartnerName = Rows.Value(row, "service_partner");
              if (!string.IsNullOrWhiteSpace(servicePartnerName))
              {
                var servicePartnerCompanyId = Contacts.ResolveCompanyContactId(
                  samedisClient,
                  contactsResource,
                  servicePartnerName,
                  false,
                  manufacturerLookup,
                  log,
                  rowId,
                  inventoryTitle
                );

                if (!string.IsNullOrWhiteSpace(servicePartnerCompanyId))
                {
                  var serviceCompanyIds = attributes.TryGetValue("service_company_ids", out var existingIds) && existingIds is List<string> ids
                    ? ids
                    : new List<string>();
                  if (!serviceCompanyIds.Contains(servicePartnerCompanyId))
                    serviceCompanyIds.Add(servicePartnerCompanyId);
                  attributes["service_company_ids"] = serviceCompanyIds;
                }
                else
                {
                  log.Warn($"service_partner company '{servicePartnerName}' konnte nicht aufgelöst werden (id='{rowId}', inventory_number='{inventoryNumber}') – wird ohne Service-Company hochgeladen.");
                }
              }
            }

            if (attributes.Count == 0)
            {
              skippedCount++;
              log.Warn($"Skipped inventory row because no writable fields were provided (id='{rowId}', inventory_number='{inventoryNumber}').");
              continue;
            }

            if (isCreateOperation && string.IsNullOrWhiteSpace(catalogId) && !isPlaceholderDeviceModel)
            {
              skippedCount++;
              log.Warn($"Skipped inventory row because no existing inventory was found and catalog_id is missing (id='{rowId}', inventory_number='{inventoryNumber}').");
              continue;
            }

            string? response;
            var operation = isCreateOperation ? "create" : "update";
            if (operation == "create")
              attributes["status"] = "created";

            // A retired device cannot be created directly in retired state (backend
            // rejects it). Create it ACTIVE; it is retired afterwards via a
            // device_retired issue in the create-success handler below.
            // (retirement_date is never part of the payload -- see BuildInventoryAttributes.)
            if (operation == "create" && isRetiredRow)
              attributes["operation_status"] = "active";

            var requestPayload = JsonConvert.SerializeObject(new
            {
              data = attributes
            });

            if (string.IsNullOrWhiteSpace(targetInventoryId))
            {
              response = samedisClient.Post(inventoryWriteResource, requestPayload);
            }
            else
            {
              response = samedisClient.Put(inventoryWriteResource, targetInventoryId, requestPayload);
            }

            void HandleInventorySuccess(string? successResponse)
            {
              var resultingId = JsonApi.ExtractDataId(successResponse) ?? targetInventoryId ?? rowId;
              if (!string.IsNullOrWhiteSpace(resultingId))
              {
                // Seed every key this row was identified by, so a later row referring to the
                // same device is answered from memory instead of asking for what this run
                // just wrote.
                inventoryLookup.RememberId(resultingId);
                inventoryLookup.RememberId(rowId, resultingId);
                inventoryLookup.RememberUniqueField("external_id", inventoryExternalId, resultingId);
                inventoryLookup.RememberField("device_number", inventoryNumber, resultingId);
              }

              if (string.IsNullOrWhiteSpace(targetInventoryId))
              {
                createdCount++;
                log.Debug($"Inventory created (inventory_number='{inventoryNumber}', id='{resultingId}').");

                // Source row is retired: the device was just created ACTIVE -- retire
                // it now via a device_retired issue so it ends up consistently retired.
                if (isRetiredRow && !string.IsNullOrWhiteSpace(resultingId))
                {
                  var retDate = Helper.NormalizeDate(Rows.Value(row, "retirement_date"));
                  var retResp = Inventories.PostDeviceRetiredIssue(
                    samedisClient, issuesResource, resultingId, inventoryNumber, inventoryTitle, retDate, log);
                  if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300)
                  {
                    retiredCount++;
                    log.Debug($"Newly created inventory retired via device_retired issue (inventory_number='{inventoryNumber}', id='{resultingId}').");
                  }
                  else
                  {
                    log.Warn($"Inventory created but device_retired issue failed (inventory_number='{inventoryNumber}', id='{resultingId}', status={samedisClient.StatusCode}). Response: {retResp}");
                  }
                }
              }
              else
              {
                updatedCount++;
                log.Debug($"Inventory updated (inventory_number='{inventoryNumber}', id='{targetInventoryId}').");
              }
            }

            if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300)
            {
              HandleInventorySuccess(response);
            }
            else
            {
              // Special handling: device is retired in samedis but the source CSV
              // delivers it as no longer retired. The samedis API rejects the update
              // with HTTP 400 "Device retired." (Inventory#check_retired, which looks at
              // the persisted device_retired flag only). In that case we create a closed
              // "recommission_device" issue to flip the device back to active, then retry
              // the update once.
              var recommissionRetrySucceeded = false;
              // Status/body of the request that ultimately failed. Set explicitly whenever
              // a later diagnostic request (state re-read) would otherwise overwrite the
              // client's StatusCode before it is logged.
              var failedStatus = samedisClient.StatusCode;
              var failedResponse = response;

              // A create cannot be recommissioned: we have no id the row maps to, yet the
              // backend matched an existing retired record. Make that visible instead of
              // logging it as a generic failure -- it means the identity mapping
              // (id -> external_id -> device_number) missed a device that does exist.
              if (isCreateOperation
                  && failedStatus == 400
                  && Inventories.IsDeviceRetiredError(failedResponse))
              {
                log.Warn($"Create was rejected with \"Device retired.\" -- an existing retired device was not resolvable by id/external_id/device_number "
                  + $"(external_id='{inventoryExternalId}', inventory_number='{inventoryNumber}', title='{inventoryTitle}'). Check the identity mapping; no recommission attempted.");
              }

              if (!isCreateOperation
                  && !isRetiredRow
                  && !string.IsNullOrWhiteSpace(targetInventoryId)
                  && samedisClient.StatusCode == 400
                  && Inventories.IsDeviceRetiredError(response))
              {
                // Resolve the canonical samedis inventory id via the full lookup
                // priority (samedis id -> external_id -> inventory_number) before
                // recommissioning. The source CSV may deliver a CHANGED inventory
                // number for the same physical device, so external_id is the stable
                // anchor; we must recommission and retry-update the exact device this
                // row maps to, not whatever a (possibly changed) number points at.
                var recommissionInventoryId = Inventories.ResolveExistingInventoryId(
                  inventoryLookup,
                  rowId,
                  inventoryExternalId,
                  inventoryNumber,
                  config.Sync.InventoriesUploadFallbackByDeviceNumber
                );
                if (string.IsNullOrWhiteSpace(recommissionInventoryId))
                  recommissionInventoryId = targetInventoryId;

                // A recommission_device issue only takes effect when the device's
                // operation_status is 'retired'. Issue#propagate_recommission_device just
                // assigns inventory_operation_status='active', and the write in
                // Issue#update_operation_status! starts with
                // `return unless inventory_operation_status_changed?`. So on a device with
                // device_retired=true but operation_status='active'/'decommissioned' the
                // recommission is a SILENT no-op: the issue is created with 2xx, the device
                // stays retired, the retry below fails again, and the next run adds another
                // useless issue. That inconsistent state is the one behind
                // samedis-care-issues#2380.
                //
                // Normalize it first with a device_retired issue (WITHOUT deleting the
                // device's open tasks -- we are about to reactivate it): that sets
                // device_retired=true AND operation_status='retired', which makes the
                // recommission below a real state change.
                var retirementStateKnown = Inventories.TryGetRetirementState(
                  samedisClient,
                  inventoryResource,
                  recommissionInventoryId,
                  out var persistedDeviceRetired,
                  out var persistedOperationStatus
                );

                var skipRecommission = false;

                if (retirementStateKnown && !persistedDeviceRetired)
                {
                  // "Device retired." is raised by Inventory#check_retired, which only ever
                  // looks at device_retired -- so this combination should be impossible.
                  // Don't post a recommission issue: the backend would reject it with
                  // "The inventory is not retired and therefore cannot be recommissioned."
                  skipRecommission = true;
                  log.Warn($"Update was rejected with \"Device retired.\" although the device reports device_retired=false "
                    + $"(operation_status='{persistedOperationStatus}', id='{recommissionInventoryId}', inventory_number='{inventoryNumber}'). "
                    + "Skipping recommission -- needs backend investigation.");
                }
                else if (retirementStateKnown
                         && !Inventories.IsRetiredOperationStatus(persistedOperationStatus))
                {
                  log.Warn($"Inconsistent retirement state (device_retired=true but operation_status='{persistedOperationStatus}'): a recommission alone would be a silent no-op. "
                    + $"Creating a device_retired issue to normalize first (id='{recommissionInventoryId}', inventory_number='{inventoryNumber}').");
                  var normalizeResponse = Inventories.PostDeviceRetiredIssue(
                    samedisClient,
                    issuesResource,
                    recommissionInventoryId,
                    inventoryNumber,
                    inventoryTitle,
                    null,
                    log,
                    deleteOpenTasks: false
                  );
                  if (samedisClient.StatusCode < 200 || samedisClient.StatusCode >= 300)
                  {
                    log.Warn($"Normalizing device_retired issue was rejected (id='{recommissionInventoryId}', inventory_number='{inventoryNumber}', status={samedisClient.StatusCode}). "
                      + $"The recommission below will most likely stay without effect. Response: {normalizeResponse}");
                  }
                }

                // Stay silent at log level 1 when the recommission+retry succeeds --
                // the resulting inventory update will already be counted via
                // HandleInventorySuccess (which logs at level 2). Only escalate to
                // WARN/ERROR if the recommission or the retry itself fails.
                log.Debug($"Inventory is retired in samedis but CSV row is not (id='{recommissionInventoryId}', external_id='{inventoryExternalId}', inventory_number='{inventoryNumber}'). Attempting recommission and update retry.");

                var recommissionResponse = skipRecommission
                  ? null
                  : Inventories.PostRecommissionIssue(
                    samedisClient,
                    issuesResource,
                    recommissionInventoryId,
                    inventoryNumber,
                    inventoryTitle,
                    log
                  );

                if (recommissionResponse != null)
                {
                  response = samedisClient.Put(inventoryWriteResource, recommissionInventoryId, requestPayload);
                  if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300)
                  {
                    log.Debug($"Inventory recommissioned and updated after retry (inventory_number='{inventoryNumber}', id='{recommissionInventoryId}').");
                    HandleInventorySuccess(response);
                    recommissionedCount++;
                    recommissionRetrySucceeded = true;
                  }
                  else
                  {
                    // Level 1 on purpose: without this line a failed retry is
                    // indistinguishable in the default log from the recommission
                    // path never having engaged (all other recommission messages
                    // are level 2).
                    // Keep the retry's outcome: the state re-read below issues another
                    // request and would otherwise overwrite StatusCode for this message
                    // and for the ERROR line at the end of this block.
                    failedStatus = samedisClient.StatusCode;
                    failedResponse = response;

                    // Re-read the state so the log carries the evidence instead of a guess:
                    // device_retired=true here means the recommission_device issue was
                    // created (2xx) without clearing the retirement.
                    var stateAfterKnown = Inventories.TryGetRetirementState(
                      samedisClient,
                      inventoryResource,
                      recommissionInventoryId,
                      out var deviceRetiredAfter,
                      out var operationStatusAfter
                    );
                    var stateAfter = stateAfterKnown
                      ? $"device_retired={deviceRetiredAfter.ToString().ToLowerInvariant()}, operation_status='{operationStatusAfter}'"
                      : "state could not be re-read";

                    log.Warn($"Recommission issue was created but the update retry was still rejected (id='{recommissionInventoryId}', inventory_number='{inventoryNumber}', status={failedStatus}, {stateAfter}). "
                      + "The recommission_device issue did not clear the retirement -- needs backend investigation (samedis-care-issues#2380).");
                  }
                }
              }

              if (!recommissionRetrySucceeded)
              {
                errorCount++;
                var failedInventoryId = string.IsNullOrWhiteSpace(targetInventoryId) ? rowId : targetInventoryId;
                log.Error($"Failed to {operation} inventory (id='{failedInventoryId}', title='{inventoryTitle}', inventory_number='{inventoryNumber}', status={failedStatus}). Response: {failedResponse}");
              }
            }
          }

          log.Info($"Inventories Upload finished. Created: {createdCount}, Updated: {updatedCount} (incl. {recommissionedCount} recommissioned, {retiredCount} retired), Skipped: {skippedCount}, Errors: {errorCount}");
        }
      }
    }
    #endregion

    #region Tasks Upload
    if (!config.Sync.TasksUpload)
    {
      log.Info("Tasks Upload sync disabled in config.yml");
    }
    else
    {
      log.Info("Tasks Upload sync starting.");

      var tasksResource = scope.Resource("issues");
      var tasksWriteResource = tasksResource + "?locale=en";
      var inventoryResource = scope.Resource("inventories");
      var tasksCsvPath = Path.Combine(uploadRoot, "tasks.csv");
      var setInventoryOperationStatusOnFailedMaintenance = config.Sync.TasksUploadSetInventoryOperationStatusOnFailedMaintenance;

      RequireAccess(log, samedisClient, tasksResource);
      RequireAccess(log, samedisClient, inventoryResource);

      if (!File.Exists(tasksCsvPath))
      {
        log.Warn($"Tasks Upload skipped. CSV not found: {tasksCsvPath}");
      }
      else
      {
        DataTable uploadTable;
        try
        {
          uploadTable = Csv.Read(tasksCsvPath, tableName: "TasksUpload", trimFields: true);
        }
        catch (Exception ex)
        {
          log.Error($"Tasks Upload failed to read CSV {tasksCsvPath}: {ex.Message}");
          uploadTable = new DataTable("TasksUpload");
        }

        if (uploadTable.Rows.Count == 0)
        {
          log.Warn("Tasks Upload skipped because CSV contains no rows.");
        }
        else if (!Csv.HasColumns(uploadTable, Tasks.UploadRequiredColumns))
        {
          log.Error($"Tasks Upload skipped. CSV missing one or more required columns: {string.Join(", ", Tasks.UploadRequiredColumns)}");
        }
        else
        {
          var issueLookup = new ResourceLookup(samedisClient, tasksResource, scope.KeyLookup);
          var taskInventoryLookup = new ResourceLookup(samedisClient, inventoryResource, scope.KeyLookup);
          var createdCount = 0;
          var updatedCount = 0;
          var skippedCount = 0;
          var errorCount = 0;
          var documentsUploadedCount = 0;
          var documentsSkippedCount = 0;
          var documentsErrorCount = 0;

          var rowNumber = 0;
          foreach (DataRow row in uploadTable.Rows)
          {
            rowNumber++;
            var issueNumber = Rows.Value(row, "issue_number");
            var inventoryDeviceNumber = Rows.Value(row, "inventory_device_number");
            var documentFileName = Tasks.GetTaskDocumentFileName(row);
            if (string.IsNullOrWhiteSpace(documentFileName))
            {
              skippedCount++;
              documentsSkippedCount++;
              log.Warn($"Skipped task row {rowNumber} because document filename is empty (issue_number='{issueNumber}', inventory_device_number='{inventoryDeviceNumber}').");
              continue;
            }

            var documentPath = Tasks.ResolveTaskDocumentPath(uploadRoot, documentFileName);
            if (string.IsNullOrWhiteSpace(documentPath))
            {
              skippedCount++;
              documentsSkippedCount++;
              log.Warn($"Skipped task row {rowNumber} because document file '{documentFileName}' was not found (issue_number='{issueNumber}', inventory_device_number='{inventoryDeviceNumber}').");
              continue;
            }

            if (string.IsNullOrWhiteSpace(inventoryDeviceNumber))
            {
              skippedCount++;
              log.Warn($"Skipped task row {rowNumber} because inventory_device_number is empty.");
              continue;
            }

            var inventoryId = Inventories.ResolveInventoryIdByDeviceNumber(
              taskInventoryLookup,
              inventoryDeviceNumber
            );
            if (string.IsNullOrWhiteSpace(inventoryId))
            {
              skippedCount++;
              log.Warn($"Skipped task row {rowNumber} because inventory_device_number '{inventoryDeviceNumber}' could not be resolved to an inventory id.");
              continue;
            }

            var targetIssueId = Rows.Value(row, "id");
            if (string.IsNullOrWhiteSpace(targetIssueId) && !string.IsNullOrWhiteSpace(issueNumber))
            {
              // external_id first, because that is the key this sync writes: the task is created
              // with external_id set to the source's issue_number, and the unique index is on
              // (tenant_id, external_id).
              //
              // issue_number is the SERVER's own running number, assigned on create and unrelated
              // to the source's. Looking up by it alone therefore never found a task this sync had
              // created, so every re-run tried to create it again and was rejected with a
              // duplicate-key error -- which also meant the document could never be re-attached.
              targetIssueId = issueLookup.First(
                () => issueLookup.ByUniqueField("external_id", issueNumber),
                () => Tasks.ResolveIssueIdByIssueNumber(issueLookup, issueNumber) is { Length: > 0 } byNumber
                        ? byNumber
                        : null);
            }

            var taskAttributes = Tasks.BuildTaskAttributes(
              row,
              inventoryId,
              setInventoryOperationStatusOnFailedMaintenance,
              out var buildError,
              out var buildWarning
            );
            if (taskAttributes == null)
            {
              errorCount++;
              log.Error($"Failed to process task row {rowNumber} (issue_number='{issueNumber}', inventory_device_number='{inventoryDeviceNumber}'): {buildError}");
              continue;
            }

            if (!string.IsNullOrWhiteSpace(buildWarning))
            {
              log.Warn($"Task row {rowNumber} warning (issue_number='{issueNumber}', inventory_device_number='{inventoryDeviceNumber}'): {buildWarning}");
            }

            var requestPayload = JsonConvert.SerializeObject(new
            {
              data = taskAttributes
            });

            var existingIssueId = string.IsNullOrWhiteSpace(targetIssueId) ? null : targetIssueId;
            var isCreateOperation = existingIssueId is null;
            var response = existingIssueId is null
              ? samedisClient.Post(tasksWriteResource, requestPayload)
              : samedisClient.Put(tasksWriteResource, existingIssueId, requestPayload);

            if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300)
            {
              var resultingIssueId = JsonApi.ExtractDataId(response) ?? targetIssueId ?? string.Empty;
              if (!string.IsNullOrWhiteSpace(issueNumber))
                issueLookup.RememberField("issue_number", issueNumber, resultingIssueId);

              if (isCreateOperation)
              {
                createdCount++;
                log.Debug($"Task created (issue_number='{issueNumber}', inventory_device_number='{inventoryDeviceNumber}', id='{resultingIssueId}').");
              }
              else
              {
                updatedCount++;
                log.Debug($"Task updated (issue_number='{issueNumber}', inventory_device_number='{inventoryDeviceNumber}', id='{targetIssueId}').");
              }

              if (!File.Exists(documentPath))
              {
                documentsErrorCount++;
                log.Error($"Task document upload failed because file vanished before upload (issue_number='{issueNumber}', task_id='{resultingIssueId}', file='{documentFileName}').");
              }
              else if (string.IsNullOrWhiteSpace(resultingIssueId))
              {
                documentsErrorCount++;
                log.Error($"Task document upload failed because issue id is empty (issue_number='{issueNumber}', file='{documentFileName}').");
              }
              else if (Tasks.IsDocumentAlreadyAttached(samedisClient, tasksResource, resultingIssueId,
                                                       Path.GetFileName(documentPath), log))
              {
                documentsSkippedCount++;
                log.Debug($"Task document already attached, not uploading again (issue_number='{issueNumber}', task_id='{resultingIssueId}', file='{Path.GetFileName(documentPath)}').");
              }
              else
              {
                var uploadResource = $"{tasksResource}/{resultingIssueId}/uploads";
                var uploadResponse = samedisClient.PostDocument(uploadResource, documentPath, Path.GetFileName(documentPath));
                if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300)
                {
                  documentsUploadedCount++;
                  log.Debug($"Task document uploaded (issue_number='{issueNumber}', task_id='{resultingIssueId}', file='{Path.GetFileName(documentPath)}').");
                }
                else
                {
                  documentsErrorCount++;
                  log.Error($"Failed to upload task document (issue_number='{issueNumber}', task_id='{resultingIssueId}', file='{Path.GetFileName(documentPath)}', status={samedisClient.StatusCode}). Response: {uploadResponse}");
                }
              }
            }
            else
            {
              errorCount++;
              var failedIssueId = string.IsNullOrWhiteSpace(targetIssueId) ? issueNumber : targetIssueId;
              var operation = isCreateOperation ? "create" : "update";
              log.Error($"Failed to {operation} task (id='{failedIssueId}', issue_number='{issueNumber}', inventory_device_number='{inventoryDeviceNumber}', status={samedisClient.StatusCode}). Response: {response}");
            }
          }

          log.Info($"Tasks Upload finished. Created: {createdCount}, Updated: {updatedCount}, Skipped: {skippedCount}, Errors: {errorCount}, Documents Uploaded: {documentsUploadedCount}, Documents Skipped: {documentsSkippedCount}, Document Errors: {documentsErrorCount}");
        }
      }
    }
    #endregion

    #region Requests Upload
    if (!config.Sync.RequestsUpload)
    {
      log.Info("Requests Upload sync disabled in config.yml");
    }
    else
    {
      log.Info("Requests Upload sync starting.");

      var requestsResource = scope.Resource("incidents");
      var requestsWriteResource = requestsResource + "?locale=en";
      var requestsCsvPath = Path.Combine(uploadRoot, "requests.csv");
      var requestMessagesCsvPath = Path.Combine(uploadRoot, "request-messages.csv");

      RequireAccess(log, samedisClient, requestsResource);

      var inventoriesResource = scope.Resource("inventories");

      var incidentLookup = new ResourceLookup(samedisClient, requestsResource, scope.KeyLookup);
      var requestInventoryLookup = new ResourceLookup(samedisClient, inventoriesResource, scope.KeyLookup);
      var supporterByInventoryAndEmail = new Dictionary<string, Helper.ResponsibleSupporter?>(StringComparer.OrdinalIgnoreCase);

      // -- requests.csv: status / responsible / etc. updates on existing requests --
      if (!File.Exists(requestsCsvPath))
      {
        log.Warn($"Requests Upload: status CSV not found, skipping: {requestsCsvPath}");
      }
      else
      {
        DataTable uploadTable;
        try
        {
          uploadTable = Csv.Read(requestsCsvPath, tableName: "RequestsUpload", trimFields: true);
        }
        catch (Exception ex)
        {
          log.Error($"Requests Upload failed to read CSV {requestsCsvPath}: {ex.Message}");
          uploadTable = new DataTable("RequestsUpload");
        }

        if (uploadTable.Rows.Count == 0)
        {
          log.Warn("Requests Upload skipped because requests.csv contains no rows.");
        }
        else if (!Csv.HasColumns(uploadTable, Requests.UploadRequiredColumns))
        {
          log.Error($"Requests Upload skipped. requests.csv missing one or more required columns: {string.Join(", ", Requests.UploadRequiredColumns)}");
        }
        else
        {
          var updatedCount = 0;
          var skippedCount = 0;
          var errorCount = 0;
          var rowNumber = 0;

          foreach (DataRow row in uploadTable.Rows)
          {
            rowNumber++;
            var rowId = Rows.Value(row, "id");
            var incidentNumber = Rows.Value(row, "incident_number");

            var targetIncidentId = rowId;
            if (string.IsNullOrWhiteSpace(targetIncidentId) && !string.IsNullOrWhiteSpace(incidentNumber))
            {
              targetIncidentId = Requests.ResolveIncidentIdByIncidentNumber(
                incidentLookup,
                incidentNumber
              );
            }

            if (string.IsNullOrWhiteSpace(targetIncidentId))
            {
              skippedCount++;
              log.Warn($"Skipped request row {rowNumber} because no existing request could be resolved (id='{rowId}', incident_number='{incidentNumber}').");
              continue;
            }

            // Resolve inventory first (prefer samedis inventory_id, otherwise look up by device_number).
            // The responsible lookup below depends on a resolved inventory.
            var csvInventoryDeviceNumber = Rows.Value(row, "inventory_device_number");
            if (string.IsNullOrWhiteSpace(csvInventoryDeviceNumber))
              csvInventoryDeviceNumber = Rows.Value(row, "inventory_number");

            var resolvedInventoryId = Inventories.ResolveInventoryIdByIdOrDeviceNumber(
              requestInventoryLookup,
              Rows.Value(row, "inventory_id"),
              csvInventoryDeviceNumber
            );
            if (string.IsNullOrWhiteSpace(resolvedInventoryId) && !string.IsNullOrWhiteSpace(csvInventoryDeviceNumber))
            {
              log.Warn($"Request row {rowNumber} (id='{targetIncidentId}', incident_number='{incidentNumber}'): inventory_device_number '{csvInventoryDeviceNumber}' could not be resolved to an inventory.");
            }

            // Resolve "verantwortlich" by email against the inventory's incident supporters
            // (internal contact, staff member, or external enterprise contact).
            var responsible = Helper.ResolveResponsibleByEmail(
              samedisClient,
              scope,
              resolvedInventoryId,
              Rows.Value(row, "responsible_email"),
              supporterByInventoryAndEmail,
              log
            );
            var resolvedResponsibleId = responsible?.Id ?? string.Empty;
            var resolvedResponsibleType = responsible?.Type ?? string.Empty;

            var attributes = Requests.BuildRequestUpdateAttributes(
              row,
              out var buildError,
              out var buildWarning,
              resolvedResponsibleId,
              resolvedResponsibleType,
              resolvedInventoryId
            );
            if (attributes == null)
            {
              errorCount++;
              log.Error($"Failed to process request row {rowNumber} (id='{targetIncidentId}', incident_number='{incidentNumber}'): {buildError}");
              continue;
            }

            if (attributes.Count == 0)
            {
              skippedCount++;
              log.Debug($"Skipped request row {rowNumber} (id='{targetIncidentId}', incident_number='{incidentNumber}'): {buildWarning}");
              continue;
            }

            if (!string.IsNullOrWhiteSpace(buildWarning))
            {
              log.Warn($"Request row {rowNumber} warning (id='{targetIncidentId}', incident_number='{incidentNumber}'): {buildWarning}");
            }

            var requestPayload = JsonConvert.SerializeObject(new
            {
              data = attributes
            });

            var response = samedisClient.Put(requestsWriteResource, targetIncidentId, requestPayload);

            if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300)
            {
              updatedCount++;
              log.Debug($"Request updated (id='{targetIncidentId}', incident_number='{incidentNumber}', fields=[{string.Join(",", attributes.Keys)}]).");
            }
            else
            {
              errorCount++;
              log.Error($"Failed to update request (id='{targetIncidentId}', incident_number='{incidentNumber}', status={samedisClient.StatusCode}). Response: {response}");
            }
          }

          log.Info($"Requests Upload finished (status updates). Updated: {updatedCount}, Skipped: {skippedCount}, Errors: {errorCount}");
        }
      }

      // -- request-messages.csv: create new messages (rows with empty id) plus optional asset --
      if (!File.Exists(requestMessagesCsvPath))
      {
        log.Warn($"Requests Upload: messages CSV not found, skipping: {requestMessagesCsvPath}");
      }
      else
      {
        DataTable messagesTable;
        try
        {
          messagesTable = Csv.Read(requestMessagesCsvPath, tableName: "RequestMessagesUpload", trimFields: true);
        }
        catch (Exception ex)
        {
          log.Error($"Requests Upload failed to read CSV {requestMessagesCsvPath}: {ex.Message}");
          messagesTable = new DataTable("RequestMessagesUpload");
        }

        if (messagesTable.Rows.Count == 0)
        {
          log.Warn("Requests Upload skipped messages because request-messages.csv contains no rows.");
        }
        else if (!Csv.HasColumns(messagesTable, Requests.MessageUploadRequiredColumns))
        {
          log.Error($"Requests Upload skipped messages. request-messages.csv missing one or more required columns: {string.Join(", ", Requests.MessageUploadRequiredColumns)}");
        }
        else
        {
          var hasFilenameColumn = messagesTable.Columns.Contains("filename");
          var createdCount = 0;
          var skippedCount = 0;
          var errorCount = 0;
          var documentsUploadedCount = 0;
          var documentsSkippedCount = 0;
          var documentsErrorCount = 0;
          var rowNumber = 0;

          foreach (DataRow row in messagesTable.Rows)
          {
            rowNumber++;
            var existingMessageId = Rows.Value(row, "id");
            if (!string.IsNullOrWhiteSpace(existingMessageId))
            {
              skippedCount++;
              log.Debug($"Skipped message row {rowNumber} because id is non-empty (only new messages with empty id are uploaded). id='{existingMessageId}'.");
              continue;
            }

            var incidentIdRaw = Rows.Value(row, "incident_id");
            var incidentNumberRaw = Rows.Value(row, "incident_number");

            var targetIncidentId = incidentIdRaw;
            if (string.IsNullOrWhiteSpace(targetIncidentId) && !string.IsNullOrWhiteSpace(incidentNumberRaw))
            {
              targetIncidentId = Requests.ResolveIncidentIdByIncidentNumber(
                incidentLookup,
                incidentNumberRaw
              );
            }

            if (string.IsNullOrWhiteSpace(targetIncidentId))
            {
              skippedCount++;
              log.Warn($"Skipped message row {rowNumber} because parent request could not be resolved (incident_id='{incidentIdRaw}', incident_number='{incidentNumberRaw}').");
              continue;
            }

            var messageAttributes = Requests.BuildMessageCreateAttributes(row, out var buildError);
            if (messageAttributes == null)
            {
              errorCount++;
              log.Error($"Failed to process message row {rowNumber} (incident_id='{targetIncidentId}', incident_number='{incidentNumberRaw}'): {buildError}");
              continue;
            }

            var messagesWriteResource = $"{requestsResource}/{targetIncidentId}/messages?locale=en";
            var requestPayload = JsonConvert.SerializeObject(new
            {
              data = messageAttributes
            });

            var response = samedisClient.Post(messagesWriteResource, requestPayload);

            if (samedisClient.StatusCode < 200 || samedisClient.StatusCode >= 300)
            {
              errorCount++;
              log.Error($"Failed to create message (incident_id='{targetIncidentId}', incident_number='{incidentNumberRaw}', status={samedisClient.StatusCode}). Response: {response}");
              continue;
            }

            createdCount++;
            var resultingMessageId = JsonApi.ExtractDataId(response) ?? string.Empty;
            log.Debug($"Message created (incident_id='{targetIncidentId}', incident_number='{incidentNumberRaw}', id='{resultingMessageId}').");

            // Optional asset attached to this message.
            if (!hasFilenameColumn)
              continue;

            var filename = Rows.Value(row, "filename");
            if (string.IsNullOrWhiteSpace(filename))
              continue;

            var documentPath = Requests.ResolveRequestDocumentPath(uploadRoot, filename);
            if (string.IsNullOrWhiteSpace(documentPath))
            {
              documentsSkippedCount++;
              log.Warn($"Skipped asset for message row {rowNumber} because file '{filename}' was not found under {uploadRoot}/request_documents/.");
              continue;
            }

            if (string.IsNullOrWhiteSpace(resultingMessageId))
            {
              documentsErrorCount++;
              log.Error($"Asset upload failed because resulting message id is empty (incident_id='{targetIncidentId}', file='{filename}').");
              continue;
            }

            var assetResource = $"{requestsResource}/{targetIncidentId}/messages/{resultingMessageId}/uploads";
            var assetResponse = samedisClient.PostDocument(assetResource, documentPath, Path.GetFileName(documentPath));
            if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300)
            {
              documentsUploadedCount++;
              log.Debug($"Message asset uploaded (incident_id='{targetIncidentId}', message_id='{resultingMessageId}', file='{Path.GetFileName(documentPath)}').");
            }
            else
            {
              documentsErrorCount++;
              log.Error($"Failed to upload message asset (incident_id='{targetIncidentId}', message_id='{resultingMessageId}', file='{Path.GetFileName(documentPath)}', status={samedisClient.StatusCode}). Response: {assetResponse}");
            }
          }

          log.Info($"Requests Upload finished (messages). Created: {createdCount}, Skipped: {skippedCount}, Errors: {errorCount}, Assets Uploaded: {documentsUploadedCount}, Assets Skipped: {documentsSkippedCount}, Asset Errors: {documentsErrorCount}");
        }
      }
    }
    #endregion

    #region DeviceTypes
    if (!config.Sync.DeviceTypes)
    {
      log.Info("Device Types sync disabled in config.yml");
    }
    else
    {
      var urlResource = scope.Resource("device_types");
      RequireAccess(log, samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var typelist = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<DeviceTypes.Root>(response);
      var totalRecords = typelist?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      LogListStatus(log, samedisClient, requestResource, totalRecords, pages);

      // get data
      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
        response = samedisClient.Get(requestResource);
        log.Debug($"Page {page}");
        log.Debug($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}");
        DeviceTypes.Root? root = null;
        if (!string.IsNullOrEmpty(response))
        {
          root = JsonConvert.DeserializeObject<DeviceTypes.Root>(response);
        }
        if (root == null) continue;
        Helper.ToCsv<DeviceTypes.Root, DeviceTypes.Attributes>(
          root,
          Path.Combine(downloadRoot, "devicetypes.csv"),
          r => (r.Data ?? Enumerable.Empty<DeviceTypes.Data>()).Select(d => d.Attributes!).Where(attr => attr != null)
        );
      }
    }
    #endregion

    #region DeviceModels
    if (!config.Sync.DeviceModels)
    {
      log.Info("Device Models sync disabled in config.yml");
    }
    else
    {
      var urlResource = scope.Resource("device_models");
      RequireAccess(log, samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();

      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var modellist = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<DeviceModels.Root>(response);
      var totalRecords = modellist?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      LogListStatus(log, samedisClient, requestResource, totalRecords, pages);

      // get data
      for (var page = 1; page <= pages; page++)
      {
        filterBuilder.Clear();
        //filterBuilder.Add("linked_image_id", FilterBuilder.FilterType.NotEmpty, FilterBuilder.Type.Text);
        filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
        requestResource += $"&sort=[{{\"property\":\"device_model_combo_search\",\"direction\":\"ASC\"}}]";
        response = samedisClient.Get(requestResource);
        log.Debug($"Page {page}");
        log.Debug($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}");

        if (string.IsNullOrEmpty(response)) continue;
        modellist = JsonConvert.DeserializeObject<DeviceModels.Root>(response);
        //Helper.ToCsv<DeviceModels.Root, DeviceModels.Attributes>(modellist, Path.Combine(downloadRoot, "devicemodels_dump.csv"), r => r.Data.Select(d => d.Attributes));

        if (modellist?.Data != null && modellist.Data.Count > 0)
        {
          var dsDm = DeviceModels.CreateDeviceDataSet();
          var dsC = Contacts.CreateContactDataSet();
          foreach (var item in modellist.Data)
          {
            var attributes = item.Attributes;
            if (attributes == null) continue;
            if (attributes.Id == "63e399b904f218000e738670") continue; // ignore "No device model"

            log.Info($"Id: {attributes.Id} ** Title: {attributes.Title} ** Device Type Id: {attributes.DeviceTypeId}");

            // detail to get service intervals and regulatories
            var detailResponse = samedisClient.Get(urlResource + "/" + attributes.Id);
            if (!string.IsNullOrEmpty(detailResponse))
              DeviceModels.FillDeviceDataSet(dsDm, detailResponse);

            var urlManufacturerResource = scope.Resource("contacts");
            RequireAccess(log, samedisClient, urlManufacturerResource);
            var manufacturerResponse = samedisClient.Get(urlManufacturerResource + "/" + attributes.ManufacturerCompanyContactId);
            if (!string.IsNullOrEmpty(manufacturerResponse))
              Contacts.FillContactDataSet(dsC, manufacturerResponse);

          }
          Csv.Append(Path.Combine(downloadRoot, "devicemodels.csv"), dsDm.Tables["Devices"]!);
          Csv.Append(Path.Combine(downloadRoot, "devicemanufacturers.csv"), dsC.Tables["Contacts"]!);
        }
      }
    }
    #endregion

    #region Departments Download
    if (!config.Sync.DepartmentsDownload)
    {
      log.Info("Departments Download sync disabled in config.yml");
    }
    else
    {
      var urlResource = scope.Resource("departments");
      RequireAccess(log, samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var departmentList = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<Departments.Root>(response);
      var totalRecords = departmentList?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      LogListStatus(log, samedisClient, requestResource, totalRecords, pages);

      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&quickfilter=&gridfilter={filterBuilder.Get()}";
        response = samedisClient.Get(requestResource);
        log.Debug($"Page {page}");
        log.Debug($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}");

        if (string.IsNullOrEmpty(response)) continue;
        var dDs = Departments.CreateDepartmentDataSet();
        Departments.FillDepartmentDataSet(dDs, response);
        Csv.Append(Path.Combine(downloadRoot, "departments.csv"), dDs.Tables["Departments"]!);
      }
    }
    #endregion

    #region Locations Download
    if (!config.Sync.LocationsDownload)
    {
      log.Info("Locations Download sync disabled in config.yml");
    }
    else
    {
      var urlResource = scope.Resource("device_locations");
      RequireAccess(log, samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var locationList = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<Locations.Root>(response);
      var totalRecords = locationList?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      LogListStatus(log, samedisClient, requestResource, totalRecords, pages);

      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&quickfilter=&gridfilter={filterBuilder.Get()}";
        response = samedisClient.Get(requestResource);
        log.Debug($"Page {page}");
        log.Debug($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}");

        if (string.IsNullOrEmpty(response)) continue;
        var lDs = Locations.CreateLocationDataSet();
        Locations.FillLocationDataSet(lDs, response);
        Csv.Append(Path.Combine(downloadRoot, "locations.csv"), lDs.Tables["Locations"]!);
      }
    }
    #endregion

    #region Inventories Download
    if (!config.Sync.InventoriesDownload)
    {
      log.Info("Inventories Download sync disabled in config.yml");
    }
    else
    {
      var urlResource = scope.Resource("inventories");
      var locationsResource = scope.Resource("device_locations");
      var floorsResource = scope.Resource("floors");

      log.Info($"Using resource: {urlResource}");
      RequireAccess(log, samedisClient, urlResource);
      if (useExtendedDeviceLocations)
      {
        RequireAccess(log, samedisClient, locationsResource);
        RequireAccess(log, samedisClient, floorsResource);
      }

      var filterBuilder = new FilterBuilder();
      var includeSourceLocationDetails = useExtendedDeviceLocations;
      var sourceLocationByLocationId = new Dictionary<string, Inventories.SourceLocationExportInfo>(StringComparer.OrdinalIgnoreCase);
      var floorExternalIdByFloorId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      Inventories.SourceLocationExportInfo ResolveSourceLocationForExport(Inventories.Attributes inventoryAttributes)
      {
        var emptyResult = new Inventories.SourceLocationExportInfo();
        var inventoryId = inventoryAttributes.Id ?? string.Empty;
        var deviceLocationId = inventoryAttributes.DeviceLocationId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(deviceLocationId))
          return emptyResult;

        if (sourceLocationByLocationId.TryGetValue(deviceLocationId, out var cachedSourceLocation))
          return cachedSourceLocation;

        var locationResponse = samedisClient.Get(locationsResource + "/" + Uri.EscapeDataString(deviceLocationId));
        if (samedisClient.StatusCode != 200 || string.IsNullOrWhiteSpace(locationResponse))
        {
          log.Warn($"Property mode export: failed room lookup for source_location_id fallback (inventory_id='{inventoryId}', location_id='{deviceLocationId}', status={samedisClient.StatusCode} {samedisClient.Status}).");
          sourceLocationByLocationId[deviceLocationId] = emptyResult;
          return emptyResult;
        }

        var locationRoot = JsonConvert.DeserializeObject<Locations.Root>(locationResponse);
        var locationAttributes = locationRoot?.Data?.FirstOrDefault()?.Attributes;
        var roomExternalId = locationAttributes?.ExternalId?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(roomExternalId))
        {
          var roomResult = new Inventories.SourceLocationExportInfo
          {
            SourceLocationId = roomExternalId,
            SourceLocationType = "room"
          };
          sourceLocationByLocationId[deviceLocationId] = roomResult;
          return roomResult;
        }

        var floorId = locationAttributes?.FloorId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(floorId))
        {
          sourceLocationByLocationId[deviceLocationId] = emptyResult;
          return emptyResult;
        }

        if (!floorExternalIdByFloorId.TryGetValue(floorId, out var floorExternalId))
        {
          floorExternalId = string.Empty;
          var floorResponse = samedisClient.Get(floorsResource + "/" + Uri.EscapeDataString(floorId));
          if (samedisClient.StatusCode == 200 && !string.IsNullOrWhiteSpace(floorResponse))
          {
            var floorRoot = JsonConvert.DeserializeObject<Floors.Root>(floorResponse);
            floorExternalId = floorRoot?.Data?.FirstOrDefault()?.Attributes?.ExternalId?.Trim() ?? string.Empty;
          }
          else
          {
            log.Warn($"Property mode export: failed floor lookup for source_location_id fallback (inventory_id='{inventoryId}', floor_id='{floorId}', status={samedisClient.StatusCode} {samedisClient.Status}).");
          }

          floorExternalIdByFloorId[floorId] = floorExternalId;
        }

        var floorResult = new Inventories.SourceLocationExportInfo
        {
          SourceLocationId = floorExternalId,
          SourceLocationType = string.IsNullOrWhiteSpace(floorExternalId) ? string.Empty : "floor"
        };
        sourceLocationByLocationId[deviceLocationId] = floorResult;
        return floorResult;
      }

      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&variant=regular&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var inventoryList = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<Inventories.Root>(response);
      var totalRecords = inventoryList?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      LogListStatus(log, samedisClient, requestResource, totalRecords, pages);

      // get data
      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
        requestResource += $"&sort=[{{\"property\":\"device_model_combo_search\",\"direction\":\"ASC\"}}]";
        response = samedisClient.Get(requestResource);
        log.Debug($"Page {page}");
        log.Debug($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}");

        if (string.IsNullOrEmpty(response)) continue;
        inventoryList = JsonConvert.DeserializeObject<Inventories.Root>(response);
        // Helper.ToCsv<Inventories.Root, Inventories.Attributes>(inventoryList, Path.Combine(downloadRoot, "inventories_dump.csv"), r => r.Data.Select(d => d.Attributes));

        if (inventoryList?.Data != null && inventoryList.Data.Count > 0)
        {
          var iDs = Inventories.CreateInventoryDataSet();
          foreach (var item in inventoryList.Data)
          {
            var attributes = item.Attributes;
            if (attributes == null) continue;
            log.Info($"Id: {attributes.Id} ** Inventory Nr: {attributes.DeviceNumber} ** Device Model: {attributes.DeviceModelTitle}");

            // detail to get service intervals and regulatories
            var detailResponse = samedisClient.Get(urlResource + "/" + attributes.Id);
            if (!string.IsNullOrEmpty(detailResponse))
              Inventories.FillInventoryDataSet(
                iDs,
                detailResponse,
                numberFormat,
                includeSourceLocationDetails ? ResolveSourceLocationForExport : null
              );

          }
          Csv.Append(Path.Combine(downloadRoot, "inventories.csv"), iDs.Tables["Inventories"]!);
        }
      }
    }
    #endregion

    #region Tasks Download
    // Document downloads retry up to 5 times on HTTP 202 (document generation pending)
    if (!config.Sync.TasksDownload)
    {
      log.Info("Tasks Download sync disabled in config.yml");
    }
    else
    {
      log.Info("Tasks Download sync starting.");
      var urlResource = scope.Resource("issues");
      RequireAccess(log, samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

      var taskDownloadTypes = config.Sync.TaskDownloadTypes ?? string.Empty;
      var taskDownloadStatus = config.Sync.TaskDownloadStatus ?? string.Empty;
      var taskTypeFilter = $"&filter[issue_type]={taskDownloadTypes}";
      var archiveFilter = $"&filter[archive]={config.Sync.TaskArchiveFilter.ToString().ToLower()}";
      var statusFilter = $"&filter[status]={taskDownloadStatus}";

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}{archiveFilter}{taskTypeFilter}{statusFilter}";
      var response = samedisClient.Get(requestResource);
      var taskList = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<Tasks.Root>(response);
      var totalRecords = taskList?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      LogListStatus(log, samedisClient, requestResource, totalRecords, pages);

      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&quickfilter=&gridfilter={filterBuilder.Get()}{archiveFilter}{taskTypeFilter}{statusFilter}";
        response = samedisClient.Get(requestResource);
        log.Debug($"Page {page}");
        log.Debug($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}");

        if (string.IsNullOrEmpty(response)) continue;
        var taskRoot = JsonConvert.DeserializeObject<Tasks.Root>(response);
        var tDs = Tasks.CreateTaskDataSet();
        Tasks.FillTaskDataSet(tDs, response);
        Csv.Append(Path.Combine(downloadRoot, "tasks.csv"), tDs.Tables["Tasks"]!);

        if (taskRoot?.Data == null || taskRoot.Data.Count == 0)
          continue;

        var documentsRoot = Path.Combine(downloadRoot, "task_documents");
        Directory.CreateDirectory(documentsRoot);

        foreach (var task in taskRoot.Data)
        {
          var attr = task.Attributes;
          var taskId = attr?.Id ?? task.Id;
          if (string.IsNullOrEmpty(taskId)) continue;

          var inventoryDeviceNumber = attr?.InventoryDeviceNumber ?? "unknown";
          var issueNumber = attr?.IssueNumber?.ToString() ?? taskId;
          var dateIso = Dates.ToIsoDate(attr?.Date, attr?.DoneAt, attr?.UpdatedAt, attr?.CreatedAt) ?? DateTime.Now.ToString("yyyy-MM-dd");

          var docRequest = $"{urlResource}/{taskId}/uploads?page[number]=1&page[limit]={pageSize}&quickfilter=&gridfilter={{}}";
          var docResponse = samedisClient.Get(docRequest);
          Tasks.TaskDocuments.Root? docRoot = string.IsNullOrEmpty(docResponse) ? null : JsonConvert.DeserializeObject<Tasks.TaskDocuments.Root>(docResponse);
          var docTotal = docRoot?.Meta?.Total ?? 0;
          var docPages = docTotal % pageSize != 0 ? docTotal / pageSize + 1 : docTotal / pageSize;

          for (var docPage = 1; docPage <= Math.Max(1, docPages); docPage++)
          {
            if (docPage > 1)
            {
              docRequest = $"{urlResource}/{taskId}/uploads?page[number]={docPage}&page[limit]={pageSize}&quickfilter=&gridfilter={{}}";
              docResponse = samedisClient.Get(docRequest);
              docRoot = string.IsNullOrEmpty(docResponse) ? null : JsonConvert.DeserializeObject<Tasks.TaskDocuments.Root>(docResponse);
            }

            if (docRoot?.Data == null || docRoot.Data.Count == 0)
              continue;

            var multipleDocs = docRoot.Data.Count > 1 || docTotal > 1;

            foreach (var doc in docRoot.Data)
            {
              var docUrl = doc.Links?.Document;
              if (string.IsNullOrEmpty(docUrl)) continue;

              var ext = Files.Extension(doc.Attributes?.Name, doc.Attributes?.MimeType, docUrl, ".pdf");
              var safeTaskId = Strings.SanitizeFileName(issueNumber);
              var safeInventoryId = Strings.SanitizeFileName(inventoryDeviceNumber);
              var fileName = $"task_{safeTaskId}_inventory_{safeInventoryId}_{dateIso}";
              if (multipleDocs && !string.IsNullOrEmpty(doc.Id))
                fileName += $"_doc_{Strings.SanitizeFileName(doc.Id)}";
              fileName += ext;

              var outputPath = Path.Combine(documentsRoot, fileName);
              if (File.Exists(outputPath)) continue;

              try
              {
                var downloaded = samedisClient.DownloadAsync(docUrl, outputPath).GetAwaiter().GetResult();
                if (downloaded)
                  log.Debug($"Downloaded task document: {fileName}");
                else
                  log.Warn($"Task document not ready after retries for task {taskId}: {fileName}");
              }
              catch (Exception ex)
              {
                log.Error($"Failed to download task document for task {taskId}: {ex.Message}");
              }
            }
          }

          var detailResponse = samedisClient.Get(urlResource + "/" + taskId);
          if (string.IsNullOrEmpty(detailResponse))
            continue;

          var detailRoot = JsonConvert.DeserializeObject<Tasks.Root>(detailResponse);
          var detailAttr = detailRoot?.Data?.FirstOrDefault()?.Attributes;
          var protocolUrl = detailAttr?.TestProtocolUrl;

          if (!string.IsNullOrEmpty(protocolUrl))
          {
            var protocolExt = Files.Extension(null, "application/pdf", protocolUrl, ".pdf");
            var safeTaskId = Strings.SanitizeFileName(issueNumber);
            var safeInventoryId = Strings.SanitizeFileName(inventoryDeviceNumber);
            var protocolFileName = $"task_{safeTaskId}_inventory_{safeInventoryId}_{dateIso}_protocol{protocolExt}";
            var protocolPath = Path.Combine(documentsRoot, protocolFileName);

            if (!File.Exists(protocolPath))
            {
              try
              {
                var downloaded = samedisClient.DownloadAsync(protocolUrl, protocolPath).GetAwaiter().GetResult();
                if (downloaded)
                  log.Debug($"Downloaded task protocol: {protocolFileName}");
                else
                  log.Warn($"Task protocol not ready after retries for task {taskId}: {protocolFileName}");
              }
              catch (Exception ex)
              {
                log.Error($"Failed to download task protocol for task {taskId}: {ex.Message}");
              }
            }
          }
        }
      }
    }
    #endregion

    #region Requests Download
    if (!config.Sync.RequestsDownload)
    {
      log.Info("Requests Download sync disabled in config.yml");
    }
    else
    {
      log.Info("Requests Download sync starting.");
      var urlResource = scope.Resource("incidents");
      RequireAccess(log, samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var requestList = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<Requests.Root>(response);
      var totalRecords = requestList?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      LogListStatus(log, samedisClient, requestResource, totalRecords, pages);

      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&quickfilter=&gridfilter={filterBuilder.Get()}";
        response = samedisClient.Get(requestResource);
        log.Debug($"Page {page}");
        log.Debug($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}");
        if (samedisClient.StatusCode >= 400)
          log.Error($"Request URI: {requestResource}");


        if (string.IsNullOrEmpty(response)) continue;
        var requestRoot = JsonConvert.DeserializeObject<Requests.Root>(response);
        var rDs = Requests.CreateRequestDataSet();
        Requests.FillRequestDataSet(rDs, response, samedisTenantId, config.Samedis.WebUri);
        Csv.Append(Path.Combine(downloadRoot, "requests.csv"), rDs.Tables["Requests"]!);

        if (requestRoot?.Data == null || requestRoot.Data.Count == 0)
          continue;

        var requestDocumentsRoot = Path.Combine(downloadRoot, "request_documents");
        Directory.CreateDirectory(requestDocumentsRoot);

        foreach (var request in requestRoot.Data)
        {
          var rAttr = request.Attributes;
          var requestId = rAttr?.Id ?? request.Id;
          if (string.IsNullOrEmpty(requestId)) continue;

          var incidentNumber = rAttr?.IncidentNumber?.ToString() ?? requestId;
          var safeIncident = Strings.SanitizeFileName(incidentNumber);
          var dateIso = Dates.ToIsoDate(rAttr?.UpdatedAt, rAttr?.CreatedAt) ?? DateTime.Now.ToString("yyyy-MM-dd");

          // Messages
          var msgRequest = $"{urlResource}/{requestId}/messages?page[number]=1&page[limit]={pageSize}&quickfilter=&gridfilter={{}}";
          var msgResponse = samedisClient.Get(msgRequest);
          var msgRoot = string.IsNullOrEmpty(msgResponse) ? null : JsonConvert.DeserializeObject<Requests.RequestMessages.Root>(msgResponse);
          var msgTotal = msgRoot?.Meta?.Total ?? 0;
          var msgPages = msgTotal % pageSize != 0 ? msgTotal / pageSize + 1 : msgTotal / pageSize;

          for (var msgPage = 1; msgPage <= Math.Max(1, msgPages); msgPage++)
          {
            if (msgPage > 1)
            {
              msgRequest = $"{urlResource}/{requestId}/messages?page[number]={msgPage}&page[limit]={pageSize}&quickfilter=&gridfilter={{}}";
              msgResponse = samedisClient.Get(msgRequest);
            }
            if (string.IsNullOrEmpty(msgResponse)) continue;
            var rmDs = Requests.CreateRequestMessageDataSet();
            Requests.FillRequestMessageDataSet(rmDs, msgResponse, requestId, incidentNumber);
            Csv.Append(Path.Combine(downloadRoot, "request-messages.csv"), rmDs.Tables["RequestMessages"]!);
          }

          // Uploads / assets attached to the request (and its messages)
          var docRequest = $"{urlResource}/{requestId}/uploads?page[number]=1&page[limit]={pageSize}&quickfilter=&gridfilter={{}}";
          var docResponse = samedisClient.Get(docRequest);
          Requests.RequestUploads.Root? docRoot = string.IsNullOrEmpty(docResponse) ? null : JsonConvert.DeserializeObject<Requests.RequestUploads.Root>(docResponse);
          var docTotal = docRoot?.Meta?.Total ?? 0;
          var docPages = docTotal % pageSize != 0 ? docTotal / pageSize + 1 : docTotal / pageSize;

          for (var docPage = 1; docPage <= Math.Max(1, docPages); docPage++)
          {
            if (docPage > 1)
            {
              docRequest = $"{urlResource}/{requestId}/uploads?page[number]={docPage}&page[limit]={pageSize}&quickfilter=&gridfilter={{}}";
              docResponse = samedisClient.Get(docRequest);
              docRoot = string.IsNullOrEmpty(docResponse) ? null : JsonConvert.DeserializeObject<Requests.RequestUploads.Root>(docResponse);
            }

            if (docRoot?.Data == null || docRoot.Data.Count == 0)
              continue;

            foreach (var doc in docRoot.Data)
            {
              var docUrl = doc.Links?.Document;
              if (string.IsNullOrEmpty(docUrl)) continue;

              var ext = Files.Extension(doc.Attributes?.Name, doc.Attributes?.MimeType, docUrl, ".pdf");
              var fileName = $"request_{safeIncident}_{dateIso}";
              var messageId = doc.Attributes?.MessageId;
              if (!string.IsNullOrEmpty(messageId))
                fileName += $"_msg_{Strings.SanitizeFileName(messageId)}";
              if (!string.IsNullOrEmpty(doc.Id))
                fileName += $"_doc_{Strings.SanitizeFileName(doc.Id)}";
              fileName += ext;

              var outputPath = Path.Combine(requestDocumentsRoot, fileName);
              if (File.Exists(outputPath)) continue;

              try
              {
                var downloaded = samedisClient.DownloadAsync(docUrl, outputPath).GetAwaiter().GetResult();
                if (downloaded)
                  log.Debug($"Downloaded request document: {fileName}");
                else
                  log.Warn($"Request document not ready after retries for request {requestId}: {fileName}");
              }
              catch (Exception ex)
              {
                log.Error($"Failed to download request document for request {requestId}: {ex.Message}");
              }
            }
          }
        }
      }
    }
    #endregion

    if (config.Sync.ArchiveToSamedisCsvFiles)
    {
      Helper.ArchiveUploadCsvFiles(log, uploadRoot, config.Sync.InventoriesUpload);
    }

    File.WriteAllText(lastRunFilePath, syncStartTime.ToString(lastRunFormat, CultureInfo.InvariantCulture));
    log.Info("Sync finised.");


  }

    /// <summary>Logs the message as an error and stops the run.</summary>
    private static void Abort(ISyncLog log, string message)
    {
      log.Error(message);
      Environment.Exit(1);
    }

    /// <summary>
    /// Stops the run unless the authenticated user may read the resource.
    /// </summary>
    /// <remarks>
    /// Replaces Helper.CanDo, which deserialized an unrelated resource model just to reach
    /// meta.msg.message and called Environment.Exit from inside a helper class.
    /// </remarks>
    private static void RequireAccess(ISyncLog log, RequestData client, string resource)
    {
      var result = Capability.Probe(client, resource);
      if (!result.Allowed)
        Abort(log, $"Sync stopped. {result.StatusCode} {result.Message} for {resource}");
    }

    /// <summary>Logs the outcome of a list request: status, record count and page count.</summary>
    private static void LogListStatus(ISyncLog log, RequestData client, string requestResource,
                                     int totalRecords, int pages)
    {
      log.Debug($"Status Code: {client.StatusCode} {client.Status}");
      if (client.StatusCode >= 400)
        log.Error($"Request URI: {requestResource}");
      log.Debug($"Total: {totalRecords} Pages: {pages}");
    }
}
