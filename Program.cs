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
    if (config.Auth.Uri.Length == 0 || config.Auth.ClientId.Length == 0 || config.Auth.ClientSecret.Length == 0)
    {
      helper.Message($"Authentication configuration invalid, please check config.yml.", 1, "ERROR");
      return;
    }

    var httpSettings = new HttpSettings()
    {
      Proxy = config.Http.Proxy,
      ProxyUsername = config.Http.ProxyUsername,
      ProxyPassword = config.Http.ProxyPassword,
      ValidateCertificate = config.Http.ValidCertificate,
    };

    var samedisAuth = new Authenticate(config.Auth.Uri, config.Auth.ClientId, config.Auth.ClientSecret, httpSettings, helper);
    helper.Message($"Credential checkup Status: {samedisAuth.StatusCode} {samedisAuth.Status} User: {samedisAuth.User}", 1);
    var bearerToken = samedisAuth.BearerToken;

    //define resource
    var samedisClient = new RequestData(config.Samedis.Uri, bearerToken, httpSettings);

    // list settings
    var pageSize = 250; // max 250

    // clean up
    if (Directory.Exists("data"))
      Directory.Delete("data", true);
    Directory.CreateDirectory("data");
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
      var urlResource = $"/api/{config.Samedis.ApiVersion}/tenants/{config.Samedis.TenantId}/issues";
      helper.CanDo(samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      // filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

      var taskTypeFilter = config.Sync.TaskDownloadTypes != null && config.Sync.TaskDownloadTypes.Count > 0
        ? $"&filter[issue_type]={string.Join(",", config.Sync.TaskDownloadTypes)}"
        : "";
      var archiveFilter = $"&filter[archive]={config.Sync.TaskArchiveFilter.ToString().ToLower()}";
      var statusValues = (config.Sync.TaskDownloadStatus != null && config.Sync.TaskDownloadStatus.Count > 0)
        ? config.Sync.TaskDownloadStatus
        : ["done"];
      var statusFilter = statusValues.Count > 0
        ? $"&filter[status]={string.Join(",", statusValues)}"
        : "";

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}{archiveFilter}{taskTypeFilter}{statusFilter}";
      var response = samedisClient.Get(requestResource);
      var taskList = JsonConvert.DeserializeObject<Tasks.Root>(response);
      var totalRecords = taskList == null ? 0 : taskList.Meta.Total;
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
        Helper.ExportDataSetToCsv(tDs, "data/tasks.csv", "Tasks");

        if (taskRoot?.Data == null || taskRoot.Data.Count == 0)
          continue;

        var documentsRoot = Path.Combine("data", "task_documents");
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
          var docRoot = JsonConvert.DeserializeObject<Tasks.TaskDocuments.Root>(docResponse);
          var docTotal = docRoot?.Meta?.Total ?? 0;
          var docPages = docTotal % pageSize != 0 ? docTotal / pageSize + 1 : docTotal / pageSize;

          for (var docPage = 1; docPage <= Math.Max(1, docPages); docPage++)
          {
            if (docPage > 1)
            {
              docRequest = $"{urlResource}/{taskId}/uploads?page[number]={docPage}&page[limit]={pageSize}&quickfilter=&gridfilter={{}}";
              docResponse = samedisClient.Get(docRequest);
              docRoot = JsonConvert.DeserializeObject<Tasks.TaskDocuments.Root>(docResponse);
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

    //helper.MessageAndExit("we stop here");

    #region DeviceTypes
    if (!config.Sync.DeviceTypes)
    {
      helper.Message("Device Types sync disabled in config.yml", 1);
    }
    else
    {
      var urlResource = $"/api/{config.Samedis.ApiVersion}/tenants/{config.Samedis.TenantId}/device_types";
      helper.CanDo(samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var typelist = JsonConvert.DeserializeObject<DeviceTypes.Root>(response);
      var totalRecords = typelist == null ? 0 : typelist.Meta.Total;
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
          "data/devicetypes.csv",
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
      var urlResource = $"/api/{config.Samedis.ApiVersion}/tenants/{config.Samedis.TenantId}/departments";
      helper.CanDo(samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var departmentList = JsonConvert.DeserializeObject<Departments.Root>(response);
      var totalRecords = departmentList == null ? 0 : departmentList.Meta.Total;
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
        Helper.ExportDataSetToCsv(dDs, "data/departments.csv", "Departments");
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
      var urlResource = $"/api/{config.Samedis.ApiVersion}/tenants/{config.Samedis.TenantId}/device_locations";
      helper.CanDo(samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var locationList = JsonConvert.DeserializeObject<Locations.Root>(response);
      var totalRecords = locationList == null ? 0 : locationList.Meta.Total;
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
        Helper.ExportDataSetToCsv(lDs, "data/locations.csv", "Locations");
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
      var urlResource = $"/api/{config.Samedis.ApiVersion}/tenants/{config.Samedis.TenantId}/device_models";
      helper.CanDo(samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();

      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var modellist = JsonConvert.DeserializeObject<DeviceModels.Root>(response);
      var totalRecords = modellist == null ? 0 : modellist.Meta.Total;
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

        modellist = JsonConvert.DeserializeObject<DeviceModels.Root>(response);
        //Helper.ToCsv<DeviceModels.Root, DeviceModels.Attributes>(modellist, "data/devicemodels_dump.csv", r => r.Data.Select(d => d.Attributes));

        if (modellist != null)
        {
          var dsDm = DeviceModels.CreateDeviceDataSet();
          var dsC = Contacts.CreateContactDataSet();
          foreach (var item in modellist.Data)
          {
            if (item.Attributes.Id == "63e399b904f218000e738670") continue; // ignore "No device model"

            helper.Message($"Id: {item.Attributes.Id} ** Title: {item.Attributes.Title} ** Device Type Id: {item.Attributes.DeviceTypeId}");

            // detail to get service intervals and regulatories
            var detailResponse = samedisClient.Get(urlResource + "/" + item.Attributes.Id);

            DeviceModels.FillDeviceDataSet(dsDm, detailResponse);

            var urlManufacturerResource = $"/api/{config.Samedis.ApiVersion}/tenants/{config.Samedis.TenantId}/contacts";
            helper.CanDo(samedisClient, urlManufacturerResource);
            var manufacturerResponse = samedisClient.Get(urlManufacturerResource + "/" + item.Attributes.ManufacturerCompanyContactId);
            var manufacturer = JsonConvert.DeserializeObject<Contacts.Root>(manufacturerResponse);

            Contacts.FillContactDataSet(dsC, manufacturerResponse);

          }
          Helper.ExportDataSetToCsv(dsDm, "data/devicemodels.csv", "Devices");
          Helper.ExportDataSetToCsv(dsC, "data/devicemanufacturers.csv", "Contacts");
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
      var urlResource = $"/api/{config.Samedis.ApiVersion}/tenants/{config.Samedis.TenantId}/inventories";
      //urlResource = $"/api/{config.Samedis.ApiVersion}/enterprise/tenants/{config.Samedis.TenantId}/inventories";

      helper.Message($"Using resource: {urlResource}", 1);
      helper.CanDo(samedisClient, urlResource);

      var filterBuilder = new FilterBuilder();

      filterBuilder.Clear();
      filterBuilder.Add("updated_at", FilterBuilder.FilterType.GreaterThan, FilterBuilder.Type.Date, lastRun);

      var requestResource = urlResource + $"?page[number]=1&page[limit]=0&variant=regular&quickfilter=&gridfilter={filterBuilder.Get()}";
      var response = samedisClient.Get(requestResource);
      var inventoryList = JsonConvert.DeserializeObject<Inventories.Root>(response);
      var totalRecords = inventoryList == null ? 0 : inventoryList.Meta.Total;
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

        inventoryList = JsonConvert.DeserializeObject<Inventories.Root>(response);
        // Helper.ToCsv<Inventories.Root, Inventories.Attributes>(inventoryList, "data/inventories_dump.csv", r => r.Data.Select(d => d.Attributes));

        if (inventoryList != null)
        {
          var iDs = Inventories.CreateInventoryDataSet();
          foreach (var item in inventoryList.Data)
          {
            helper.Message($"Id: {item.Attributes.Id} ** Inventory Nr: {item.Attributes.DeviceNumber} ** Device Model: {item.Attributes.DeviceModelTitle}");

            // detail to get service intervals and regulatories
            var detailResponse = samedisClient.Get(urlResource + "/" + item.Attributes.Id);

            Inventories.FillInventoryDataSet(iDs, detailResponse);

          }
          Helper.ExportDataSetToCsv(iDs, "data/inventories.csv", "Inventories");
        }
      }
    }
    #endregion

    helper.Message("Sync finised.", 1);
  }
}
