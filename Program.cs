using Newtonsoft.Json;
using System.Data;
using System.Globalization;

namespace SamedisExternalSync;

internal class Program
{
  static void Main(string[] args)
  {
    #region init
    // set log
    var helper = new Helper
    {
      LogFile = "Logfile_" + DateTime.Now.ToShortDateString() + ".log",
    };

    // read config
    var ymlFilePath = "config.yml";
    if (!File.Exists(ymlFilePath))
      helper.MessageAndExit($"The file {ymlFilePath} does not exists. Stopping Import.");

    AppConfig config = AppConfig.LoadFromYaml(ymlFilePath);

    helper.LogLevel = config.Logging.Level;
    helper.LogMode = config.Logging.Mode;

    helper.Message("Sync started.", 1);

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
    helper.Message($"Last run: {lastRun}", 2);
    File.WriteAllText(lastRunFilePath, DateTimeOffset.Now.ToString(lastRunFormat, CultureInfo.InvariantCulture));

    // init authentication
    var authUri = config.Auth.Uri;
    var authClientId = config.Auth.ClientId;
    var authClientSecret = config.Auth.ClientSecret;
    if (string.IsNullOrWhiteSpace(authUri) || string.IsNullOrWhiteSpace(authClientId) || string.IsNullOrWhiteSpace(authClientSecret))
    {
      helper.Message($"Authentication configuration invalid, please check config.yml.", 1, "ERROR");
      return;
    }

    var samedisUri = config.Samedis.Uri;
    var samedisApiVersion = config.Samedis.ApiVersion;
    var samedisTenantId = config.Samedis.TenantId;
    if (string.IsNullOrWhiteSpace(samedisUri) || string.IsNullOrWhiteSpace(samedisApiVersion) || string.IsNullOrWhiteSpace(samedisTenantId))
    {
      helper.Message($"Samedis configuration invalid, please check config.yml.", 1, "ERROR");
      return;
    }

    var httpSettings = new HttpSettings()
    {
      Proxy = config.Http.Proxy,
      ProxyUsername = config.Http.ProxyUsername,
      ProxyPassword = config.Http.ProxyPassword,
      ValidateCertificate = config.Http.ValidCertificate,
    };

    var samedisAuth = new Authenticate(authUri, authClientId, authClientSecret, httpSettings, helper);
    helper.Message($"Credential checkup Status: {samedisAuth.StatusCode} {samedisAuth.Status} User: {samedisAuth.User}", 1);
    var bearerToken = samedisAuth.BearerToken;

    //define resource
    var samedisClient = new RequestData(samedisUri, bearerToken, httpSettings);

    // tenant-level settings
    var tenantSettings = Tenant.GetSettings(samedisClient, samedisApiVersion, samedisTenantId, helper);
    var useExtendedDeviceLocations = tenantSettings.UseExtendedDeviceLocations;
    var useProfitCenters = tenantSettings.UseProfitCenters;
    var locationMode = useExtendedDeviceLocations ? "property" : "standard";
    helper.Message(
      $"Tenant settings loaded. TenantId: {tenantSettings.TenantId} Name: {tenantSettings.Name} LocationMode: {locationMode} use_profit_centers: {useProfitCenters}",
      1
    );

    // list settings
    var pageSize = 250; // max 250

    var defaultDownloadRoot = Path.Combine("data", "from_samedis");
    var defaultUploadRoot = Path.Combine("data", "to_samedis");
    var downloadRoot = string.IsNullOrWhiteSpace(config.Paths?.FromSamedis) ? defaultDownloadRoot : config.Paths.FromSamedis.Trim();
    var uploadRoot = string.IsNullOrWhiteSpace(config.Paths?.ToSamedis) ? defaultUploadRoot : config.Paths.ToSamedis.Trim();
    helper.Message($"Data paths: from_samedis='{downloadRoot}', to_samedis='{uploadRoot}'", 2);

    // clean up download folder only, keep upload folder for import procedures
    if (Directory.Exists(downloadRoot))
      Directory.Delete(downloadRoot, true);
    Directory.CreateDirectory(downloadRoot);
    Directory.CreateDirectory(uploadRoot);
    #endregion

    #region Tasks Upload
    if (!config.Sync.TasksUpload)
    {
      helper.Message("Tasks Upload sync disabled in config.yml", 1);
    }
    else
    {
      helper.Message("Tasks Upload sync enabled but not implemented.", 1, "WARN");
    }
    #endregion

    #region Tasks Download
    if (!config.Sync.TasksDownload)
    {
      helper.Message("Tasks Download sync disabled in config.yml", 1);
    }
    else
    {
      helper.Message("Tasks Download sync starting.");
      var urlResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/issues";
      helper.CanDo(samedisClient, urlResource);

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

      helper.LogListStatus(samedisClient, requestResource, totalRecords, pages);

      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&quickfilter=&gridfilter={filterBuilder.Get()}{archiveFilter}{taskTypeFilter}{statusFilter}";
        response = samedisClient.Get(requestResource);
        helper.Message($"Page {page}", 2);
        helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);

        if (string.IsNullOrEmpty(response)) continue;
        var taskRoot = JsonConvert.DeserializeObject<Tasks.Root>(response);
        var tDs = Tasks.CreateTaskDataSet();
        Tasks.FillTaskDataSet(tDs, response);
        Helper.ExportDataSetToCsv(tDs, Path.Combine(downloadRoot, "tasks.csv"), "Tasks");

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
          var dateIso = Helper.ToIsoDate(attr?.Date, attr?.DoneAt, attr?.UpdatedAt, attr?.CreatedAt) ?? DateTime.Now.ToString("yyyy-MM-dd");

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

              var ext = Helper.GetExtension(doc.Attributes?.Name, doc.Attributes?.MimeType, docUrl);
              var safeTaskId = Helper.SanitizeFileName(issueNumber);
              var safeInventoryId = Helper.SanitizeFileName(inventoryDeviceNumber);
              var fileName = $"task_{safeTaskId}_inventory_{safeInventoryId}_{dateIso}";
              if (multipleDocs && !string.IsNullOrEmpty(doc.Id))
                fileName += $"_doc_{Helper.SanitizeFileName(doc.Id)}";
              fileName += ext;

              var outputPath = Path.Combine(documentsRoot, fileName);
              if (File.Exists(outputPath)) continue;

              try
              {
                samedisClient.DownloadAsync(docUrl, outputPath).GetAwaiter().GetResult();
                helper.Message($"Downloaded task document: {fileName}", 2);
              }
              catch (Exception ex)
              {
                helper.Message($"Failed to download task document for task {taskId}: {ex.Message}", 1, "ERROR");
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
            var protocolExt = Helper.GetExtension(null, "application/pdf", protocolUrl);
            var safeTaskId = Helper.SanitizeFileName(issueNumber);
            var safeInventoryId = Helper.SanitizeFileName(inventoryDeviceNumber);
            var protocolFileName = $"task_{safeTaskId}_inventory_{safeInventoryId}_{dateIso}_protocol{protocolExt}";
            var protocolPath = Path.Combine(documentsRoot, protocolFileName);

            if (!File.Exists(protocolPath))
            {
              try
              {
                samedisClient.DownloadAsync(protocolUrl, protocolPath).GetAwaiter().GetResult();
                helper.Message($"Downloaded task protocol: {protocolFileName}", 2);
              }
              catch (Exception ex)
              {
                helper.Message($"Failed to download task protocol for task {taskId}: {ex.Message}", 1, "ERROR");
              }
            }
          }
        }
      }
    }
    #endregion

    #region Requests Upload
    if (!config.Sync.RequestsUpload)
    {
      helper.Message("Requests Upload sync disabled in config.yml", 1);
    }
    else
    {
      helper.Message("Requests Upload sync enabled but not implemented.", 1, "WARN");
    }
    #endregion

    #region Requests Download
    if (!config.Sync.RequestsDownload)
    {
      helper.Message("Requests Download sync disabled in config.yml", 1);
    }
    else
    {
      helper.Message("Requests Download sync starting.");
      var urlResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/incidents";
      helper.CanDo(samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var requestList = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<Requests.Root>(response);
      var totalRecords = requestList?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      helper.LogListStatus(samedisClient, requestResource, totalRecords, pages);

      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&quickfilter=&gridfilter={filterBuilder.Get()}";
        response = samedisClient.Get(requestResource);
        helper.Message($"Page {page}", 2);
        helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);
        if (samedisClient.StatusCode >= 400)
          helper.Message($"Request URI: {requestResource}", 1, "ERROR");


        if (string.IsNullOrEmpty(response)) continue;
        var rDs = Requests.CreateRequestDataSet();
        Requests.FillRequestDataSet(rDs, response);
        Helper.ExportDataSetToCsv(rDs, Path.Combine(downloadRoot, "requests.csv"), "Requests");
      }
    }
    #endregion

    #region DeviceTypes
    if (!config.Sync.DeviceTypes)
    {
      helper.Message("Device Types sync disabled in config.yml", 1);
    }
    else
    {
      var urlResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/device_types";
      helper.CanDo(samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var typelist = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<DeviceTypes.Root>(response);
      var totalRecords = typelist?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      helper.LogListStatus(samedisClient, requestResource, totalRecords, pages);

      // get data
      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
        response = samedisClient.Get(requestResource);
        helper.Message($"Page {page}", 2);
        helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);
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

    #region Departments Download
    if (!config.Sync.DepartmentsDownload)
    {
      helper.Message("Departments Download sync disabled in config.yml", 1);
    }
    else
    {
      var urlResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/departments";
      helper.CanDo(samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var departmentList = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<Departments.Root>(response);
      var totalRecords = departmentList?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      helper.LogListStatus(samedisClient, requestResource, totalRecords, pages);

      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&quickfilter=&gridfilter={filterBuilder.Get()}";
        response = samedisClient.Get(requestResource);
        helper.Message($"Page {page}", 2);
        helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);

        if (string.IsNullOrEmpty(response)) continue;
        var dDs = Departments.CreateDepartmentDataSet();
        Departments.FillDepartmentDataSet(dDs, response);
        Helper.ExportDataSetToCsv(dDs, Path.Combine(downloadRoot, "departments.csv"), "Departments");
      }
    }
    #endregion

    #region Locations Download
    if (!config.Sync.LocationsDownload)
    {
      helper.Message("Locations Download sync disabled in config.yml", 1);
    }
    else
    {
      var urlResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/device_locations";
      helper.CanDo(samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var locationList = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<Locations.Root>(response);
      var totalRecords = locationList?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      helper.LogListStatus(samedisClient, requestResource, totalRecords, pages);

      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&quickfilter=&gridfilter={filterBuilder.Get()}";
        response = samedisClient.Get(requestResource);
        helper.Message($"Page {page}", 2);
        helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);

        if (string.IsNullOrEmpty(response)) continue;
        var lDs = Locations.CreateLocationDataSet();
        Locations.FillLocationDataSet(lDs, response);
        Helper.ExportDataSetToCsv(lDs, Path.Combine(downloadRoot, "locations.csv"), "Locations");
      }
    }
    #endregion

    #region DeviceModels
    if (!config.Sync.DeviceModels)
    {
      helper.Message("Device Models sync disabled in config.yml", 1);
    }
    else
    {
      var urlResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/device_models";
      helper.CanDo(samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();

      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var modellist = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<DeviceModels.Root>(response);
      var totalRecords = modellist?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      helper.LogListStatus(samedisClient, requestResource, totalRecords, pages);

      // get data
      for (var page = 1; page <= pages; page++)
      {
        filterBuilder.Clear();
        //filterBuilder.Add("linked_image_id", FilterBuilder.FilterType.NotEmpty, FilterBuilder.Type.Text);
        filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.DateTime, lastRun);

        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
        requestResource += $"&sort=[{{\"property\":\"device_model_combo_search\",\"direction\":\"ASC\"}}]";
        response = samedisClient.Get(requestResource);
        helper.Message($"Page {page}", 2);
        helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);

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

            helper.Message($"Id: {attributes.Id} ** Title: {attributes.Title} ** Device Type Id: {attributes.DeviceTypeId}");

            // detail to get service intervals and regulatories
            var detailResponse = samedisClient.Get(urlResource + "/" + attributes.Id);
            if (!string.IsNullOrEmpty(detailResponse))
              DeviceModels.FillDeviceDataSet(dsDm, detailResponse);

            var urlManufacturerResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/contacts";
            helper.CanDo(samedisClient, urlManufacturerResource);
            var manufacturerResponse = samedisClient.Get(urlManufacturerResource + "/" + attributes.ManufacturerCompanyContactId);
            if (!string.IsNullOrEmpty(manufacturerResponse))
              Contacts.FillContactDataSet(dsC, manufacturerResponse);

          }
          Helper.ExportDataSetToCsv(dsDm, Path.Combine(downloadRoot, "devicemodels.csv"), "Devices");
          Helper.ExportDataSetToCsv(dsC, Path.Combine(downloadRoot, "devicemanufacturers.csv"), "Contacts");
        }
      }
    }
    #endregion

    #region Inventories Upload
    if (!config.Sync.InventoriesUpload)
    {
      helper.Message("Inventories Upload sync disabled in config.yml", 1);
    }
    else
    {
      helper.Message("Inventories Upload sync starting.", 1);

      var inventoryResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/inventories";
      var inventoryWriteResource = inventoryResource + "?locale=en";
      var departmentsResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/departments";
      var profitCentersResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/profit_centers";
      var propertiesResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/properties";
      var buildingsResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/buildings";
      var floorsResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/floors";
      var locationsResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/device_locations";
      var deviceModelsResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/device_models";
      var deviceTypesResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/device_types";
      var contactsResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/contacts";
      var createLocalDeviceModelsOnInventoryLookup = config.Sync.CreateLocalDeviceModelsOnInventoryLookup;
      var inventoryCsvPath = Path.Combine(uploadRoot, "inventories.csv");
      var departmentsCsvPath = Path.Combine(uploadRoot, "departments.csv");

      helper.CanDo(samedisClient, inventoryResource);
      helper.CanDo(samedisClient, departmentsResource);
      helper.CanDo(samedisClient, locationsResource);
      helper.CanDo(samedisClient, deviceModelsResource);
      if (createLocalDeviceModelsOnInventoryLookup)
      {
        helper.CanDo(samedisClient, deviceTypesResource);
        helper.CanDo(samedisClient, contactsResource);
      }
      if (useExtendedDeviceLocations)
      {
        helper.CanDo(samedisClient, propertiesResource);
        helper.CanDo(samedisClient, buildingsResource);
        helper.CanDo(samedisClient, floorsResource);
      }

      if (!File.Exists(inventoryCsvPath))
      {
        helper.Message($"Inventories Upload skipped. CSV not found: {inventoryCsvPath}", 1, "WARN");
      }
      else
      {
        DataTable uploadTable;
        try
        {
          uploadTable = Helper.ImportCsvToDataTable(inventoryCsvPath, "InventoriesUpload");
        }
        catch (Exception ex)
        {
          helper.Message($"Inventories Upload failed to read CSV {inventoryCsvPath}: {ex.Message}", 1, "ERROR");
          uploadTable = new DataTable("InventoriesUpload");
        }

        var requiredColumns = new[]
        {
          "inventory_number"
        };

        if (uploadTable.Rows.Count == 0)
        {
          helper.Message("Inventories Upload skipped because CSV contains no rows.", 1, "WARN");
        }
        else if (!Helper.CheckColumnsExist(uploadTable, requiredColumns))
        {
          helper.Message($"Inventories Upload skipped. CSV missing one or more required columns: {string.Join(", ", requiredColumns)}", 1, "ERROR");
        }
        else
        {
          var inventoryById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var inventoryByExternalId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var inventoryByDeviceNumber = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var inventoryByModelAndManufacturer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var checkedInventoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
          var checkedInventoryExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
          var checkedInventoryNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
          var checkedInventoryModelAndManufacturer = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
          var departmentsById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var departmentsByCostCenter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var departmentsByTitle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var checkedDepartments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var profitCentersByTitle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var checkedProfitCenters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var locationsById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var locationsByTitle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var checkedLocations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var propertiesByTitle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var checkedProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var buildingsByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var checkedBuildings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var floorsByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var checkedFloors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var deviceModelCatalogLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var deviceTypesByTitle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var checkedDeviceTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var manufacturersByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var checkedManufacturers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var tenantDeviceModelLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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

          var sourceBuildings = Buildings.LoadSourceBuildings(sourceBuildingsCsvPath ?? string.Empty, helper);
          var sourceFloors = Floors.LoadSourceFloors(sourceFloorsCsvPath ?? string.Empty, helper);
          var sourceRooms = Locations.LoadSourceRooms(sourceRoomsCsvPath ?? string.Empty, helper);
          var roomPlaceholderTitle = string.IsNullOrWhiteSpace(config.Sync.LocationsRoomPlaceholder)
            ? "Keine Raumzuordnung"
            : config.Sync.LocationsRoomPlaceholder.Trim();
          var hasDepartmentNotesColumn = uploadTable.Columns.Contains("wirtschaftende_einheit");
          var createStandardLocationsOnTheFly = !useExtendedDeviceLocations && config.Sync.InventoriesUploadCreateLocationsOnTheFly;
          var createPropertyHierarchyOnImport = useExtendedDeviceLocations;
          string? propertyIdForHierarchySync = null;

          if (useExtendedDeviceLocations)
          {
            if (config.Sync.InventoriesUploadCreateLocationsOnTheFly)
            {
              helper.Message(
                "Property mode: sync.inventories_upload_create_locations_on_the_fly is only used in standard mode. Property mode uses hierarchy pre-sync and row-level location assignment resolves references only.",
                1
              );
            }

            if (sourceBuildings.Count == 0 && sourceFloors.Count == 0 && sourceRooms.Count == 0)
            {
              helper.Message("Property mode hierarchy pre-sync skipped because no buildings/floors/rooms CSV data was found.", 1, "WARN");
            }
            else
            {
              var propertyTitle = string.IsNullOrWhiteSpace(tenantSettings.Name) ? "Default Property" : tenantSettings.Name;
              propertyIdForHierarchySync = Properties.ResolvePropertyId(
                samedisClient,
                propertiesResource,
                propertyTitle,
                createPropertyHierarchyOnImport,
                propertiesByTitle,
                checkedProperties,
                helper
              );

              if (string.IsNullOrWhiteSpace(propertyIdForHierarchySync))
              {
                helper.Message(
                  $"Property mode hierarchy pre-sync skipped because property '{propertyTitle}' could not be resolved/created.",
                  1,
                  "WARN"
                );
              }
              else
              {
                helper.Message(
                  $"Property mode hierarchy pre-sync starting. Source buildings: {sourceBuildings.Count}, floors: {sourceFloors.Count}, rooms: {sourceRooms.Count}",
                  1
                );

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
                    buildingsByKey,
                    checkedBuildings,
                    helper,
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
                    floorsByKey,
                    checkedFloors,
                    helper,
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
                    locationsById,
                    locationsByTitle,
                    checkedLocations,
                    helper,
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

                  locationsById[sourceRoom.SourceId] = resolvedRoomId;
                  roomsResolved++;
                }

                helper.Message(
                  $"Property mode hierarchy pre-sync finished. Buildings resolved: {buildingsResolved}, unresolved: {buildingsUnresolved}, missing title: {buildingsSkippedNoTitle}.",
                  1
                );
                helper.Message(
                  $"Property mode hierarchy pre-sync finished. Floors resolved: {floorsResolved}, unresolved: {floorsUnresolved}, missing building parent: {floorsMissingBuildingParent}, missing title: {floorsSkippedNoTitle}.",
                  1
                );
                helper.Message(
                  $"Property mode hierarchy pre-sync finished. Rooms resolved: {roomsResolved}, unresolved: {roomsUnresolved}, missing floor parent: {roomsMissingFloorParent}, missing title: {roomsSkippedNoTitle}.",
                  1
                );
              }
            }
          }
          else if (sourceBuildings.Count > 0 || sourceFloors.Count > 0 || sourceRooms.Count > 0)
          {
            helper.Message(
              "Tenant location mode is standard. buildings/floors/rooms CSV data was detected but hierarchy pre-sync is skipped.",
              1,
              "WARN"
            );
          }

          if (File.Exists(departmentsCsvPath))
          {
            DataTable departmentsTable;
            try
            {
              departmentsTable = Helper.ImportCsvToDataTable(departmentsCsvPath, "DepartmentsUpload");
            }
            catch (Exception ex)
            {
              helper.Message($"Departments preload failed to read CSV {departmentsCsvPath}: {ex.Message}", 1, "WARN");
              departmentsTable = new DataTable("DepartmentsUpload");
            }

            if (departmentsTable.Rows.Count > 0)
            {
              var hasDepartmentCsvNotesColumn = departmentsTable.Columns.Contains("notes") || departmentsTable.Columns.Contains("wirtschaftende_einheit");
              var hasDepartmentCsvProfitCenterColumn = departmentsTable.Columns.Contains("profit_center");
              helper.Message($"Departments preload source rows: {departmentsTable.Rows.Count}", 1);

              foreach (DataRow departmentRow in departmentsTable.Rows)
              {
                var departmentRowId = Helper.GetRowValue(departmentRow, "id");
                var departmentApiIdFromCsv = Helper.GetRowValue(departmentRow, "department_id");
                var departmentCostCenterFromCsv = Helper.GetRowValue(departmentRow, "cost_center_number");

                var departmentTitleFromCsv = Helper.GetRowValue(departmentRow, "department");
                if (string.IsNullOrWhiteSpace(departmentTitleFromCsv))
                  departmentTitleFromCsv = Helper.GetRowValue(departmentRow, "cost_center_description");
                if (string.IsNullOrWhiteSpace(departmentTitleFromCsv))
                  departmentTitleFromCsv = Helper.GetRowValue(departmentRow, "abteilung");

                var departmentNotesFromCsv = string.Empty;
                if (hasDepartmentCsvNotesColumn)
                {
                  departmentNotesFromCsv = Helper.GetRowValue(departmentRow, "notes");
                  if (string.IsNullOrWhiteSpace(departmentNotesFromCsv))
                    departmentNotesFromCsv = Helper.GetRowValue(departmentRow, "wirtschaftende_einheit");
                }

                var departmentProfitCenterTitle = string.Empty;
                if (useProfitCenters)
                {
                  if (hasDepartmentCsvProfitCenterColumn)
                    departmentProfitCenterTitle = Helper.GetRowValue(departmentRow, "profit_center");
                  if (string.IsNullOrWhiteSpace(departmentProfitCenterTitle) && departmentsTable.Columns.Contains("wirtschaftende_einheit"))
                    departmentProfitCenterTitle = Helper.GetRowValue(departmentRow, "wirtschaftende_einheit");
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
                    profitCentersByTitle,
                    checkedProfitCenters,
                    helper
                  ) ?? string.Empty;

                  if (string.IsNullOrWhiteSpace(departmentProfitCenterId))
                  {
                    helper.Message(
                      $"Departments preload: profit center '{departmentProfitCenterTitle}' could not be resolved/created (department_title='{departmentTitleFromCsv}', cost_center_number='{departmentCostCenterFromCsv}', source_id='{departmentRowId}'). Department will be synced without profit center.",
                      1,
                      "WARN"
                    );
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
                  departmentsById,
                  departmentsByCostCenter,
                  departmentsByTitle,
                  checkedDepartments,
                  helper,
                  departmentProfitCenterTitle
                );

                if (string.IsNullOrWhiteSpace(preloadedDepartmentId))
                {
                  helper.Message(
                    $"Departments preload: could not resolve/create department (title='{departmentTitleFromCsv}', cost_center_number='{departmentCostCenterFromCsv}', source_id='{departmentRowId}').",
                    1,
                    "WARN"
                  );
                }
                else if (!string.IsNullOrWhiteSpace(departmentProfitCenterId))
                {
                  ProfitCenters.EnsureDepartmentAssigned(
                    samedisClient,
                    profitCentersResource,
                    departmentProfitCenterId,
                    preloadedDepartmentId,
                    checkedProfitCenters,
                    helper
                  );
                }
              }
            }
          }

          helper.Message($"Inventories Upload source rows: {uploadTable.Rows.Count}", 1);
          helper.Message($"Inventories Upload location mode: {(useExtendedDeviceLocations ? "property (building/floor/room)" : "standard (room only)")}", 1);

          var createdCount = 0;
          var updatedCount = 0;
          var skippedCount = 0;
          var errorCount = 0;

          foreach (DataRow row in uploadTable.Rows)
          {
            var rowId = Helper.GetRowValue(row, "id");
            var inventoryTitle = Helper.GetRowValue(row, "title");
            if (string.IsNullOrWhiteSpace(inventoryTitle))
              inventoryTitle = Helper.GetRowValue(row, "device_model_title");

            var inventoryNumber = Helper.GetRowValue(row, "inventory_number");
            var inventoryExternalId = Helper.GetRowValue(row, "external_id");
            var departmentCostCenterNumber = Helper.GetRowValue(row, "cost_center_number");
            var departmentTitle = Helper.GetRowValue(row, "department");
            if (string.IsNullOrWhiteSpace(departmentTitle))
              departmentTitle = Helper.GetRowValue(row, "cost_center_description");
            var departmentNotes = hasDepartmentNotesColumn ? Helper.GetRowValue(row, "wirtschaftende_einheit") : string.Empty;
            var departmentProfitCenterTitle = useProfitCenters ? departmentNotes : string.Empty;
            var departmentProfitCenterId = string.Empty;

            var locationTitle = Helper.GetRowValue(row, "location");

            var operationStatus = Helper.GetRowValue(row, "operation_status");
            var sourceBuildingTitle = string.Empty;
            var sourceFloorTitle = string.Empty;
            var sourceRoomTitle = string.Empty;
            var sourceLocationId = Helper.GetRowValue(row, "source_location_id");
            var sourceLocationType = Helper.GetRowValue(row, "source_location_type");
            var normalizedSourceLocationType = sourceLocationType.Trim().ToLowerInvariant();
            Buildings.SourceBuilding? resolvedSourceBuilding = null;
            Floors.SourceFloor? resolvedSourceFloor = null;
            Locations.SourceRoom? resolvedSourceRoom = null;
            var sourceLocationResolved = false;
            var catalogId = Helper.GetRowValue(row, "catalog_id");
            var lookupTitle = inventoryTitle;
            if (string.IsNullOrWhiteSpace(lookupTitle))
              lookupTitle = Helper.GetRowValue(row, "device_model_title");

            var lookupManufacturer = Helper.GetRowValue(row, "manufacturer");
            if (string.IsNullOrWhiteSpace(lookupManufacturer))
              lookupManufacturer = Helper.GetRowValue(row, "responsible_manufacturer");
            if (string.IsNullOrWhiteSpace(lookupManufacturer))
              lookupManufacturer = Helper.GetRowValue(row, "company");

            var lookupDeviceTypeTitle = Helper.GetRowValue(row, "device_type_title");
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
              samedisClient,
              inventoryResource,
              rowId,
              inventoryExternalId,
              inventoryNumber,
              lookupTitle,
              lookupManufacturer,
              config.Sync.InventoriesUploadFallbackByDeviceNumber,
              inventoryById,
              inventoryByExternalId,
              inventoryByDeviceNumber,
              inventoryByModelAndManufacturer,
              checkedInventoryIds,
              checkedInventoryExternalIds,
              checkedInventoryNumbers,
              checkedInventoryModelAndManufacturer
            );

            if (isRetiredRow && !string.IsNullOrWhiteSpace(targetInventoryId))
            {
              skippedCount++;
              helper.Message(
                $"Skipped retired inventory row because device already exists and retired devices are not updated (id='{targetInventoryId}', inventory_number='{inventoryNumber}', title='{inventoryTitle}').",
                1,
                "WARN"
              );
              continue;
            }

            if (string.IsNullOrWhiteSpace(catalogId))
            {
              if (isPlaceholderDeviceModel)
              {
                helper.Message(
                  $"Placeholder device model row detected. Skipping catalog/device-model lookup and local model/type/manufacturer creation (inventory_number='{inventoryNumber}', title='{lookupTitle}').",
                  2
                );
              }
              else
              {
                catalogId = DeviceModels.ResolveCatalogId(
                  samedisClient,
                  deviceModelsResource,
                  lookupTitle,
                  lookupManufacturer,
                  deviceModelCatalogLookup
                ) ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(catalogId))
                {
                  helper.Message($"Resolved catalog_id '{catalogId}' via device model lookup (title='{lookupTitle}', manufacturer='{lookupManufacturer}').", 2);
                }
                else if (createLocalDeviceModelsOnInventoryLookup)
                {
                  catalogId = DeviceModels.ResolveOrCreateTenantCatalogIdForInventory(
                    samedisClient,
                    deviceModelsResource,
                    deviceTypesResource,
                    contactsResource,
                    samedisTenantId,
                    lookupTitle,
                    lookupManufacturer,
                    lookupDeviceTypeTitle,
                    deviceTypesByTitle,
                    checkedDeviceTypes,
                    manufacturersByName,
                    checkedManufacturers,
                    tenantDeviceModelLookup,
                    deviceModelCatalogLookup,
                    helper,
                    rowId,
                    inventoryNumber
                  ) ?? string.Empty;

                  if (!string.IsNullOrWhiteSpace(catalogId))
                  {
                    helper.Message(
                      $"Resolved catalog_id '{catalogId}' via local tenant device model lookup/create (title='{lookupTitle}', manufacturer='{lookupManufacturer}', device_type_title='{lookupDeviceTypeTitle}', inventory_number='{inventoryNumber}').",
                      2
                    );
                  }
                  else if (!string.IsNullOrWhiteSpace(lookupTitle))
                  {
                    helper.Message(
                      $"No device model match found and local tenant device model creation failed/skipped (title='{lookupTitle}', manufacturer='{lookupManufacturer}', device_type_title='{lookupDeviceTypeTitle}', inventory_number='{inventoryNumber}').",
                      2,
                      "WARN"
                    );
                  }
                }
                else if (!string.IsNullOrWhiteSpace(lookupTitle))
                {
                  helper.Message(
                    $"No device model match found for catalog lookup (title='{lookupTitle}', manufacturer='{lookupManufacturer}', inventory_number='{inventoryNumber}').",
                    2,
                    "WARN"
                  );
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
                profitCentersByTitle,
                checkedProfitCenters,
                helper
              ) ?? string.Empty;

              if (string.IsNullOrWhiteSpace(departmentProfitCenterId))
              {
                helper.Message(
                  $"Profit center '{departmentProfitCenterTitle}' could not be resolved/created for inventory row (id='{rowId}', inventory_number='{inventoryNumber}'). Department will be synced without profit center.",
                  1,
                  "WARN"
                );
                departmentProfitCenterTitle = string.Empty;
              }
            }

            var departmentId = Departments.ResolveDepartmentId(
              samedisClient,
              departmentsResource,
              Helper.GetRowValue(row, "department_id"),
              departmentCostCenterNumber,
              departmentTitle,
              departmentNotes,
              config.Sync.InventoriesUploadCreateDepartmentsOnTheFly,
              rowId,
              inventoryTitle,
              departmentsById,
              departmentsByCostCenter,
              departmentsByTitle,
              checkedDepartments,
              helper,
              departmentProfitCenterTitle
            );

            if ((!string.IsNullOrWhiteSpace(departmentTitle) || !string.IsNullOrWhiteSpace(departmentCostCenterNumber)) && string.IsNullOrWhiteSpace(departmentId))
            {
              helper.Message(
                $"Department could not be resolved/created (title='{departmentTitle}', cost_center_number='{departmentCostCenterNumber}', id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without department reference.",
                1,
                "WARN"
              );
            }
            else if (!string.IsNullOrWhiteSpace(departmentId) && !string.IsNullOrWhiteSpace(departmentProfitCenterId))
            {
              ProfitCenters.EnsureDepartmentAssigned(
                samedisClient,
                profitCentersResource,
                departmentProfitCenterId,
                departmentId,
                checkedProfitCenters,
                helper
              );
            }

            string? locationId = null;
            if (useExtendedDeviceLocations)
            {
              if (string.IsNullOrWhiteSpace(sourceLocationId))
              {
                helper.Message(
                  $"Property mode: source_location_id is missing for inventory row (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.",
                  1,
                  "WARN"
                );
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
                  locationsById,
                  locationsByTitle,
                  checkedLocations,
                  helper,
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
                    floorsByKey,
                    checkedFloors,
                    helper,
                    sourceLocationId
                  );
                  if (!string.IsNullOrWhiteSpace(floorByExternalId))
                  {
                    locationId = Locations.ResolveLocationId(
                      samedisClient,
                      locationsResource,
                      string.Empty,
                      roomPlaceholderTitle,
                      false,
                      rowId,
                      inventoryTitle,
                      locationsById,
                      locationsByTitle,
                      checkedLocations,
                      helper,
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
                    buildingsByKey,
                    checkedBuildings,
                    helper,
                    sourceLocationId
                  );
                  if (!string.IsNullOrWhiteSpace(buildingByExternalId))
                  {
                    locationId = Locations.ResolveLocationId(
                      samedisClient,
                      locationsResource,
                      string.Empty,
                      roomPlaceholderTitle,
                      false,
                      rowId,
                      inventoryTitle,
                      locationsById,
                      locationsByTitle,
                      checkedLocations,
                      helper,
                      propertyIdForHierarchySync,
                      buildingByExternalId
                    );
                    resolvedByExternalId = !string.IsNullOrWhiteSpace(locationId);
                  }
                }

                if (resolvedByExternalId)
                {
                  helper.Message(
                    $"Property mode: resolved source_location_id '{sourceLocationId}' via API external_id lookup (id='{rowId}', inventory_number='{inventoryNumber}').",
                    2
                  );
                  goto SkipPropertyLocationAssignment;
                }

                helper.Message(
                  $"Property mode: source_location_id '{sourceLocationId}' could not be mapped from CSV or resolved by API external_id (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.",
                  1,
                  "WARN"
                );
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
                  helper.Message(
                    $"Property mode: source_location_type '{sourceLocationType}' indicates room, but source_location_id '{sourceLocationId}' maps to a {resolvedAs} in CSV hierarchy. Falling back to placeholder room handling.",
                    2,
                    "WARN"
                  );
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
                  helper.Message(
                    $"Property mode: final room title could not be determined from source_location_id '{sourceLocationId}' (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.",
                    1,
                    "WARN"
                  );
                  goto SkipPropertyLocationAssignment;
                }
                else if (string.IsNullOrWhiteSpace(roomTitle) && !string.IsNullOrWhiteSpace(roomIdFromCsv))
                {
                  helper.Message(
                    $"Property mode: room title missing for source_location_id '{sourceLocationId}' (id='{rowId}', inventory_number='{inventoryNumber}'). Attempting room resolution by external_id only.",
                    2
                  );
                }

                if (string.IsNullOrWhiteSpace(propertyIdForHierarchySync))
                {
                  helper.Message(
                    $"Property mode: hierarchy property reference is missing (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.",
                    1,
                    "WARN"
                  );
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
                    buildingsByKey,
                    checkedBuildings,
                    helper,
                    sourceBuildingExternalId
                  );
                  if (string.IsNullOrWhiteSpace(buildingId))
                  {
                    helper.Message(
                      $"Property mode: building '{sourceBuildingTitle}' could not be resolved in imported hierarchy (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.",
                      1,
                      "WARN"
                    );
                    goto SkipPropertyLocationAssignment;
                  }
                }

                string? floorId = null;
                if (!string.IsNullOrWhiteSpace(sourceFloorTitle))
                {
                  var sourceFloorExternalId = resolvedSourceFloor?.SourceId ?? (isFloorSourceReference ? sourceLocationId : string.Empty);
                  if (string.IsNullOrWhiteSpace(buildingId))
                  {
                    helper.Message(
                      $"Property mode: floor '{sourceFloorTitle}' requires a resolved building from source hierarchy (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.",
                      1,
                      "WARN"
                    );
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
                      floorsByKey,
                      checkedFloors,
                      helper,
                      sourceFloorExternalId
                    );
                    if (string.IsNullOrWhiteSpace(floorId))
                    {
                      helper.Message(
                        $"Property mode: floor '{sourceFloorTitle}' could not be resolved in imported hierarchy (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.",
                        1,
                        "WARN"
                      );
                      goto SkipPropertyLocationAssignment;
                    }
                  }
                }

                if (!string.IsNullOrWhiteSpace(roomTitle) &&
                    isFloorSourceReference &&
                    string.IsNullOrWhiteSpace(floorId) &&
                    string.IsNullOrWhiteSpace(roomIdFromCsv))
                {
                  helper.Message(
                    $"Property mode: room '{roomTitle}' needs a resolved floor from source hierarchy (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.",
                    1,
                    "WARN"
                  );
                  goto SkipPropertyLocationAssignment;
                }
                else if (!string.IsNullOrWhiteSpace(roomTitle) &&
                         isBuildingSourceReference &&
                         string.IsNullOrWhiteSpace(buildingId) &&
                         string.IsNullOrWhiteSpace(roomIdFromCsv))
                {
                  helper.Message(
                    $"Property mode: room '{roomTitle}' needs a resolved building from source hierarchy (id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.",
                    1,
                    "WARN"
                  );
                  goto SkipPropertyLocationAssignment;
                }
                else
                {
                  var resolveRoomByExternalOnly = !string.IsNullOrWhiteSpace(roomIdFromCsv);
                  if (resolveRoomByExternalOnly)
                  {
                    if (locationsById.TryGetValue(roomIdFromCsv, out var mappedLocationId) && !string.IsNullOrWhiteSpace(mappedLocationId))
                    {
                      locationId = mappedLocationId;
                      helper.Message(
                        $"Property mode: resolved room from CSV/pre-sync cache by source_location_id '{roomIdFromCsv}' -> '{locationId}' (id='{rowId}', inventory_number='{inventoryNumber}').",
                        2
                      );
                    }
                    else
                    {
                      helper.Message(
                        $"Property mode: source_location_id '{roomIdFromCsv}' not found in CSV/pre-sync cache. Resolving via API external_id only (type='{sourceLocationType}', id='{rowId}', inventory_number='{inventoryNumber}').",
                        2
                      );

                      locationId = Locations.ResolveLocationId(
                        samedisClient,
                        locationsResource,
                        string.Empty,
                        string.Empty,
                        false,
                        rowId,
                        inventoryTitle,
                        locationsById,
                        locationsByTitle,
                        checkedLocations,
                        helper,
                        null,
                        null,
                        null,
                        null,
                        roomIdFromCsv
                      );
                    }

                    if (string.IsNullOrWhiteSpace(locationId))
                    {
                      helper.Message(
                        $"Property mode: room lookup via source_location_id '{roomIdFromCsv}' failed (source_location_type='{sourceLocationType}', id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.",
                        1,
                        "WARN"
                      );
                      goto SkipPropertyLocationAssignment;
                    }
                  }
                  else
                  {
                    locationId = Locations.ResolveLocationId(
                      samedisClient,
                      locationsResource,
                      string.Empty,
                      roomTitle,
                      false,
                      rowId,
                      inventoryTitle,
                      locationsById,
                      locationsByTitle,
                      checkedLocations,
                      helper,
                      propertyIdForHierarchySync,
                      buildingId,
                      floorId,
                      roomNotes,
                      roomIdFromCsv
                    );
                  }

                  if (!string.IsNullOrWhiteSpace(roomTitle) && string.IsNullOrWhiteSpace(locationId))
                  {
                    helper.Message(
                      $"Property mode: room '{roomTitle}' could not be resolved in imported hierarchy (source_location_id='{sourceLocationId}', source_location_type='{sourceLocationType}', building_id='{buildingId}', floor_id='{floorId}', id='{rowId}', inventory_number='{inventoryNumber}'). Proceeding without location reference.",
                      1,
                      "WARN"
                    );
                    goto SkipPropertyLocationAssignment;
                  }
                }
              }

            SkipPropertyLocationAssignment:
              ;
            }
            else
            {
              var standardLocationId = Helper.GetRowValue(row, "location_id");

              locationId = Locations.ResolveLocationId(
                samedisClient,
                locationsResource,
                standardLocationId,
                locationTitle,
                createStandardLocationsOnTheFly,
                rowId,
                inventoryTitle,
                locationsById,
                locationsByTitle,
                checkedLocations,
                helper
              );

              if (!string.IsNullOrWhiteSpace(locationTitle) && string.IsNullOrWhiteSpace(locationId))
              {
                skippedCount++;
                helper.Message($"Skipped inventory row because location '{locationTitle}' could not be resolved/created (id='{rowId}', inventory_number='{inventoryNumber}').", 1, "WARN");
                continue;
              }
            }

            var isCreateOperation = string.IsNullOrWhiteSpace(targetInventoryId);
            var attributes = Inventories.BuildInventoryAttributes(row, departmentId, locationId, catalogId, isCreateOperation);

            if (attributes.Count == 0)
            {
              skippedCount++;
              helper.Message($"Skipped inventory row because no writable fields were provided (id='{rowId}', inventory_number='{inventoryNumber}').", 2, "WARN");
              continue;
            }

            if (isCreateOperation && string.IsNullOrWhiteSpace(catalogId) && !isPlaceholderDeviceModel)
            {
              skippedCount++;
              helper.Message(
                $"Skipped inventory row because no existing inventory was found and catalog_id is missing (id='{rowId}', inventory_number='{inventoryNumber}').",
                1,
                "WARN"
              );
              continue;
            }

            string? response;
            var operation = isCreateOperation ? "create" : "update";
            if (operation == "create")
              attributes["status"] = "created";

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

            if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300)
            {
              var resultingId = Helper.ExtractDataId(response) ?? targetInventoryId ?? rowId;
              if (!string.IsNullOrWhiteSpace(resultingId))
              {
                inventoryById[resultingId] = resultingId;
                if (!string.IsNullOrWhiteSpace(rowId))
                  inventoryById[rowId] = resultingId;
                if (!string.IsNullOrWhiteSpace(inventoryExternalId))
                  inventoryByExternalId[inventoryExternalId] = resultingId;
                if (!string.IsNullOrWhiteSpace(inventoryNumber))
                  inventoryByDeviceNumber[inventoryNumber] = resultingId;
                if (!string.IsNullOrWhiteSpace(lookupTitle))
                  inventoryByModelAndManufacturer[$"{lookupTitle.Trim()}|{(lookupManufacturer ?? string.Empty).Trim()}"] = resultingId;
              }

              if (string.IsNullOrWhiteSpace(targetInventoryId))
              {
                createdCount++;
                helper.Message($"Inventory created (inventory_number='{inventoryNumber}', id='{resultingId}').", 2);
              }
              else
              {
                updatedCount++;
                helper.Message($"Inventory updated (inventory_number='{inventoryNumber}', id='{targetInventoryId}').", 2);
              }
            }
            else
            {
              errorCount++;
              var failedInventoryId = string.IsNullOrWhiteSpace(targetInventoryId) ? rowId : targetInventoryId;
              helper.Message(
                $"Failed to {operation} inventory (id='{failedInventoryId}', title='{inventoryTitle}', inventory_number='{inventoryNumber}', status={samedisClient.StatusCode}). Response: {response}",
                1,
                "ERROR"
              );
            }
          }

          helper.Message($"Inventories Upload finished. Created: {createdCount}, Updated: {updatedCount}, Skipped: {skippedCount}, Errors: {errorCount}", 1);
        }
      }
    }
    #endregion

    #region Inventories Download
    if (!config.Sync.InventoriesDownload)
    {
      helper.Message("Inventories Download sync disabled in config.yml", 1);
    }
    else
    {
      var urlResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/inventories";
      var locationsResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/device_locations";
      var floorsResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/floors";
      //urlResource = $"/api/{samedisApiVersion}/enterprise/tenants/{samedisTenantId}/inventories";

      helper.Message($"Using resource: {urlResource}", 1);
      helper.CanDo(samedisClient, urlResource);
      if (useExtendedDeviceLocations)
      {
        helper.CanDo(samedisClient, locationsResource);
        helper.CanDo(samedisClient, floorsResource);
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
          helper.Message(
            $"Property mode export: failed room lookup for source_location_id fallback (inventory_id='{inventoryId}', location_id='{deviceLocationId}', status={samedisClient.StatusCode} {samedisClient.Status}).",
            1,
            "WARN"
          );
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
            helper.Message(
              $"Property mode export: failed floor lookup for source_location_id fallback (inventory_id='{inventoryId}', floor_id='{floorId}', status={samedisClient.StatusCode} {samedisClient.Status}).",
              1,
              "WARN"
            );
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

      helper.LogListStatus(samedisClient, requestResource, totalRecords, pages);

      // get data
      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
        requestResource += $"&sort=[{{\"property\":\"device_model_combo_search\",\"direction\":\"ASC\"}}]";
        response = samedisClient.Get(requestResource);
        helper.Message($"Page {page}", 2);
        helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);

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
            helper.Message($"Id: {attributes.Id} ** Inventory Nr: {attributes.DeviceNumber} ** Device Model: {attributes.DeviceModelTitle}");

            // detail to get service intervals and regulatories
            var detailResponse = samedisClient.Get(urlResource + "/" + attributes.Id);
            if (!string.IsNullOrEmpty(detailResponse))
              Inventories.FillInventoryDataSet(
                iDs,
                detailResponse,
                includeSourceLocationDetails ? ResolveSourceLocationForExport : null
              );

          }
          Helper.ExportDataSetToCsv(iDs, Path.Combine(downloadRoot, "inventories.csv"), "Inventories");
        }
      }
    }
    #endregion

    if (config.Sync.ArchiveToSamedisCsvFiles)
    {
      helper.ArchiveUploadCsvFiles(uploadRoot, config.Sync.InventoriesUpload);
    }

    helper.Message("Sync finised.", 1);
  }
}
