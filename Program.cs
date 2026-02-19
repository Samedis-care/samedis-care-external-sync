using Newtonsoft.Json;
using System.Data;

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

    // last run handler
    var lastRun = File.Exists("lastrun.txt") ? File.ReadAllText("lastrun.txt") : "2022-01-01";
    helper.Message($"Last run: {lastRun}", 2);
    File.WriteAllText("lastrun.txt", DateTime.Now.ToString("yyyy-MM-dd"));

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

    // list settings
    var pageSize = 250; // max 250

    var dataRoot = "data";
    var downloadRoot = Path.Combine(dataRoot, "from_samedis");
    var uploadRoot = Path.Combine(dataRoot, "to_samedis");

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
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

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

      helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);
      helper.Message($"Total: {totalRecords} Pages: {pages}", 2);

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
      //filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var requestList = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<Requests.Root>(response);
      var totalRecords = requestList?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);
      helper.Message($"Total: {totalRecords} Pages: {pages}", 2);

      for (var page = 1; page <= pages; page++)
      {
        requestResource = urlResource + $"?page[number]={page}&page[limit]={pageSize}&quickfilter=&gridfilter={filterBuilder.Get()}";
        response = samedisClient.Get(requestResource);
        helper.Message($"Page {page}", 2);
        helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);

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
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var typelist = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<DeviceTypes.Root>(response);
      var totalRecords = typelist?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);
      helper.Message($"Total: {totalRecords} Pages: {pages}", 2);

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
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var departmentList = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<Departments.Root>(response);
      var totalRecords = departmentList?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);
      helper.Message($"Total: {totalRecords} Pages: {pages}", 2);

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
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var locationList = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<Locations.Root>(response);
      var totalRecords = locationList?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);
      helper.Message($"Total: {totalRecords} Pages: {pages}", 2);

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
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var modellist = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<DeviceModels.Root>(response);
      var totalRecords = modellist?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);
      helper.Message($"Total: {totalRecords} Pages: {pages}", 2);

      // get data
      for (var page = 1; page <= pages; page++)
      {
        filterBuilder.Clear();
        //filterBuilder.Add("linked_image_id", FilterBuilder.FilterType.NotEmpty, FilterBuilder.Type.Text);
        filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

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
      var locationsResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/device_locations";
      var deviceModelsResource = $"/api/{samedisApiVersion}/tenants/{samedisTenantId}/device_models";
      var inventoryCsvPath = Path.Combine(uploadRoot, "inventories.csv");

      helper.CanDo(samedisClient, inventoryResource);
      helper.CanDo(samedisClient, departmentsResource);
      helper.CanDo(samedisClient, locationsResource);
      helper.CanDo(samedisClient, deviceModelsResource);

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
          var inventoryByDeviceNumber = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var checkedInventoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
          var checkedInventoryNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
          var departmentsById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var departmentsByTitle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var checkedDepartments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var locationsById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var locationsByTitle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var checkedLocations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          var deviceModelCatalogLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

          helper.Message($"Inventories Upload source rows: {uploadTable.Rows.Count}", 1);

          var createdCount = 0;
          var updatedCount = 0;
          var skippedCount = 0;
          var errorCount = 0;

          foreach (DataRow row in uploadTable.Rows)
          {
            var rowId = Helper.GetRowValue(row, "id");
            var inventoryTitle = Helper.GetRowValue(row, "title");
            var inventoryNumber = Helper.GetRowValue(row, "inventory_number");
            var departmentTitle = Helper.GetRowValue(row, "department");
            var locationTitle = Helper.GetRowValue(row, "location");
            var catalogId = Helper.GetRowValue(row, "catalog_id");

            if (string.IsNullOrWhiteSpace(catalogId))
            {
              var lookupTitle = inventoryTitle;
              if (string.IsNullOrWhiteSpace(lookupTitle))
                lookupTitle = Helper.GetRowValue(row, "device_model_title");

              var lookupManufacturer = Helper.GetRowValue(row, "manufacturer");
              if (string.IsNullOrWhiteSpace(lookupManufacturer))
                lookupManufacturer = Helper.GetRowValue(row, "responsible_manufacturer");

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
              else if (!string.IsNullOrWhiteSpace(lookupTitle))
              {
                helper.Message($"No device model match found for catalog lookup (title='{lookupTitle}', manufacturer='{lookupManufacturer}', inventory_number='{inventoryNumber}').", 2, "WARN");
              }
            }

            var departmentId = Departments.ResolveDepartmentId(
              samedisClient,
              departmentsResource,
              Helper.GetRowValue(row, "department_id"),
              departmentTitle,
              config.Sync.InventoriesUploadCreateDepartmentsOnTheFly,
              rowId,
              inventoryTitle,
              departmentsById,
              departmentsByTitle,
              checkedDepartments,
              helper
            );

            if (!string.IsNullOrWhiteSpace(departmentTitle) && string.IsNullOrWhiteSpace(departmentId))
            {
              skippedCount++;
              helper.Message($"Skipped inventory row because department '{departmentTitle}' could not be resolved/created (id='{rowId}', inventory_number='{inventoryNumber}').", 1, "WARN");
              continue;
            }

            var locationId = Locations.ResolveLocationId(
              samedisClient,
              locationsResource,
              Helper.GetRowValue(row, "location_id"),
              locationTitle,
              config.Sync.InventoriesUploadCreateLocationsOnTheFly,
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

            var targetInventoryId = Inventories.ResolveExistingInventoryId(
              samedisClient,
              inventoryResource,
              rowId,
              inventoryNumber,
              config.Sync.InventoriesUploadFallbackByDeviceNumber,
              inventoryById,
              inventoryByDeviceNumber,
              checkedInventoryIds,
              checkedInventoryNumbers
            );

            var attributes = Inventories.BuildInventoryAttributes(row, departmentId, locationId, catalogId);

            if (attributes.Count == 0)
            {
              skippedCount++;
              helper.Message($"Skipped inventory row because no writable fields were provided (id='{rowId}', inventory_number='{inventoryNumber}').", 2, "WARN");
              continue;
            }

            var requestPayload = JsonConvert.SerializeObject(new
            {
              data = attributes
            });

            string? response;
            var operation = string.IsNullOrWhiteSpace(targetInventoryId) ? "create" : "update";
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
                if (!string.IsNullOrWhiteSpace(inventoryNumber))
                  inventoryByDeviceNumber[inventoryNumber] = resultingId;
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
      //urlResource = $"/api/{samedisApiVersion}/enterprise/tenants/{samedisTenantId}/inventories";

      helper.Message($"Using resource: {urlResource}", 1);
      helper.CanDo(samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();

      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&variant=regular&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var inventoryList = string.IsNullOrEmpty(response) ? null : JsonConvert.DeserializeObject<Inventories.Root>(response);
      var totalRecords = inventoryList?.Meta?.Total ?? 0;
      var pages = totalRecords % pageSize != 0 ? totalRecords / pageSize + 1 : totalRecords / pageSize;

      helper.Message($"Status Code: {samedisClient.StatusCode} {samedisClient.Status}", 2);
      helper.Message($"Total: {totalRecords} Pages: {pages}", 2);

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
              Inventories.FillInventoryDataSet(iDs, detailResponse);

          }
          Helper.ExportDataSetToCsv(iDs, Path.Combine(downloadRoot, "inventories.csv"), "Inventories");
        }
      }
    }
    #endregion

    helper.Message("Sync finised.", 1);
  }

}
