using System.Data;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CsvHelper;
using System.Text;
using CsvHelper.Configuration;

namespace SamedisExternalSync
{
  public class Helper
  {
    private static readonly Encoding Utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Encoding Windows1252 = CreateWindows1252Encoding();

    private static Encoding CreateWindows1252Encoding()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      return Encoding.GetEncoding(1252);
    }

    private static Encoding DetectCsvEncoding(string filePath)
    {
      using var stream = File.OpenRead(filePath);
      Span<byte> bom = stackalloc byte[4];
      var bytesRead = stream.Read(bom);

      if (bytesRead >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
        return Utf8;
      if (bytesRead >= 4 && bom[0] == 0xFF && bom[1] == 0xFE && bom[2] == 0x00 && bom[3] == 0x00)
        return Encoding.UTF32;
      if (bytesRead >= 4 && bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF)
        return new UTF32Encoding(bigEndian: true, byteOrderMark: true);
      if (bytesRead >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
        return Encoding.Unicode;
      if (bytesRead >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
        return Encoding.BigEndianUnicode;

      stream.Position = 0;
      try
      {
        using var utf8Reader = new StreamReader(stream, Utf8Strict, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var buffer = new char[4096];
        while (utf8Reader.ReadBlock(buffer, 0, buffer.Length) > 0) { }
        return Utf8;
      }
      catch (DecoderFallbackException)
      {
        return Windows1252;
      }
    }

    public static string SanitizeFileName(string value)
    {
      if (string.IsNullOrEmpty(value)) return string.Empty;
      var invalidChars = Path.GetInvalidFileNameChars();
      var sanitized = new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
      return sanitized.Replace(" ", "_");
    }

    public static string GetExtension(string? name, string? mimeType, string? url)
    {
      var ext = !string.IsNullOrEmpty(name) ? Path.GetExtension(name) : string.Empty;
      if (string.IsNullOrEmpty(ext) && !string.IsNullOrEmpty(url))
      {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
          ext = Path.GetExtension(uri.AbsolutePath);
      }

      if (string.IsNullOrEmpty(ext) && !string.IsNullOrEmpty(mimeType))
      {
        if (mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
          ext = ".pdf";
      }

      return string.IsNullOrEmpty(ext) ? ".pdf" : ext;
    }

    public static string? ToIsoDate(params string?[] values)
    {
      foreach (var value in values)
      {
        if (string.IsNullOrWhiteSpace(value)) continue;
        if (DateTime.TryParse(value, out var dt))
          return dt.ToString("yyyy-MM-dd");
      }
      return null;
    }
    /// <summary>
    /// LogLevel 0: turned off
    /// LogLevel 1: normal output
    /// LogLevel 2: debug output
    /// </summary>
    public int LogLevel = 1;
    /// <summary>
    /// LogMode 0: no output
    /// LogMode 1: Console Output
    /// LogMode 2: LogFile
    /// LofMode 3: Console and Logfile
    /// </summary>
    public int LogMode = 3;
    public string LogFile = "debug.log";

    public void Message(string message, int logLevel = 1, string logType = "INFO")
    {
      if (logLevel > LogLevel) return;
      const string format = "yyyy-MM-dd HH:mm:ss";

      if (LogMode == 1 || LogMode == 3)
      {
        Console.WriteLine(new string('*', 80));
        Console.WriteLine(DateTime.Now.ToString(format) + " " + message);
      }

      if (LogMode < 2) return;
      Directory.CreateDirectory("log");
      var logContent = string.Empty;
      logContent += DateTime.Now.ToString(format) + " ";
      logContent += logType + " ";
      if (!string.IsNullOrEmpty(message))
        logContent += message;
      File.AppendAllText(Path.Combine("log", LogFile), logContent + "\n");
    }

    public void LogListStatus(RequestData client, string requestResource, int totalRecords, int pages)
    {
      Message($"Status Code: {client.StatusCode} {client.Status}", 2);
      if (client.StatusCode >= 400)
        Message($"Request URI: {requestResource}", 1, "ERROR");
      Message($"Total: {totalRecords} Pages: {pages}", 2);
    }

    public void ArchiveUploadCsvFiles(string uploadRoot, bool inventoriesUploadEnabled)
    {
      if (!inventoriesUploadEnabled)
      {
        Message("CSV archive step skipped because inventories upload is disabled.", 2);
        return;
      }

      if (!Directory.Exists(uploadRoot))
      {
        Message($"CSV archive step skipped because upload folder does not exist: {uploadRoot}", 2);
        return;
      }

      var sourceCsvFiles = Directory.GetFiles(uploadRoot, "*.csv", SearchOption.TopDirectoryOnly);
      if (sourceCsvFiles.Length == 0)
      {
        Message($"CSV archive step skipped because no CSV files were found in {uploadRoot}.", 2);
        return;
      }

      var uploadRootFull = Path.GetFullPath(uploadRoot)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      var parentDirectory = Directory.GetParent(uploadRootFull)?.FullName;
      var uploadRootName = Path.GetFileName(uploadRootFull);
      if (string.IsNullOrWhiteSpace(parentDirectory) || string.IsNullOrWhiteSpace(uploadRootName))
      {
        Message($"CSV archive step skipped because archive path could not be determined from upload folder '{uploadRoot}'.", 1, "WARN");
        return;
      }

      var archiveRoot = Path.Combine(parentDirectory, "archive", uploadRootName);
      var archiveRootFull = Path.GetFullPath(archiveRoot)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

      if (string.Equals(uploadRootFull, archiveRootFull, StringComparison.OrdinalIgnoreCase))
      {
        Message("CSV archive step skipped because archive folder resolves to the upload folder.", 1, "WARN");
        return;
      }

      Directory.CreateDirectory(archiveRoot);

      var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
      var archivedCount = 0;
      var archiveErrors = 0;

      foreach (var sourcePath in sourceCsvFiles)
      {
        var originalFileName = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = Path.GetExtension(sourcePath);
        var targetFileName = $"{originalFileName}_{timestamp}{extension}";
        var targetPath = Path.Combine(archiveRoot, targetFileName);

        var collisionIndex = 1;
        while (File.Exists(targetPath))
        {
          targetFileName = $"{originalFileName}_{timestamp}_{collisionIndex}{extension}";
          targetPath = Path.Combine(archiveRoot, targetFileName);
          collisionIndex++;
        }

        try
        {
          File.Move(sourcePath, targetPath);
          archivedCount++;
          Message($"Archived upload CSV: {Path.GetFileName(sourcePath)} -> {targetPath}", 2);
        }
        catch (Exception ex)
        {
          archiveErrors++;
          Message($"Failed to archive upload CSV '{sourcePath}' to '{targetPath}': {ex.Message}", 1, "WARN");
        }
      }

      Message(
        $"CSV archive step finished. Archived: {archivedCount}, Errors: {archiveErrors}, Target: {archiveRoot}",
        1
      );
    }

    public static bool CheckColumnsExist(DataTable dataTable, string[] requiredColumns)
    {
      foreach (var columnName in requiredColumns)
      {
        if (!dataTable.Columns.Contains(columnName))
          return false;
      }
      return true;
    }

    public static DataTable ImportCsvToDataTable(string filePath, string tableName)
    {
      var detectedEncoding = DetectCsvEncoding(filePath);
      using var reader = new StreamReader(filePath, detectedEncoding, detectEncodingFromByteOrderMarks: true);
      using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
      {
        Delimiter = ";",
        TrimOptions = TrimOptions.Trim,
        MissingFieldFound = null,
        HeaderValidated = null,
        BadDataFound = null
      });
      using var csvDataReader = new CsvDataReader(csv);

      var dataTable = new DataTable(tableName);
      dataTable.Load(csvDataReader);
      return dataTable;
    }

    public void CanDo(RequestData client, string resource)
    {
      var requestResource = resource + "?limit=0";
      var check = client.Get(requestResource);
      if (client.StatusCode >= 400)
      {
        var record = string.IsNullOrEmpty(check) ? null : JsonConvert.DeserializeObject<JsonGeneric.Root>(check);
        MessageAndExit($"Sync stopped. {client.StatusCode} {record?.Meta?.Msg?.Message} for {requestResource}");
      }
    }

    public static string? ExternalIdExists(RequestData client, string resource, string id)
    {
      if (string.IsNullOrWhiteSpace(id))
        return "";

      var normalizedId = id.Trim();
      var requestResource = resource + "/via/external_id/" + normalizedId;
      var check = client.Get(requestResource);
      if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(check))
      {
        var extractedId = ExtractDataId(check);
        if (!string.IsNullOrWhiteSpace(extractedId))
          return extractedId;

        try
        {
          var record = JsonConvert.DeserializeObject<JsonGeneric.Root>(check);
          if (record?.Data?.Count > 0)
            return record.Data[0].Id;
        }
        catch
        {
          // Keep resilient behavior for non-standard API payloads.
        }
      }

      return "";
    }

    public static string? ExternalExists(RequestData client, string resource, string filter)
    {
      var requestResource = resource + filter;
      var check = client.Get(requestResource);
      if (client.StatusCode != 200 || string.IsNullOrWhiteSpace(check))
        return "";

      var extractedId = ExtractDataId(check);
      if (!string.IsNullOrWhiteSpace(extractedId))
        return extractedId;

      try
      {
        var record = JsonConvert.DeserializeObject<JsonGeneric.Root>(check);
        if (record?.Data?.Count > 0)
          return record.Data[0].Id;
      }
      catch
      {
        // Keep resilient behavior for non-standard API payloads.
      }

      return "";
    }

    public static string? ExtractDataId(string? json)
    {
      if (string.IsNullOrWhiteSpace(json))
        return null;

      try
      {
        var root = JToken.Parse(json);
        var data = root["data"];
        if (data == null)
          return null;

        if (data.Type == JTokenType.Array)
          return data.First?["id"]?.ToString();

        return data["id"]?.ToString();
      }
      catch
      {
        return null;
      }
    }

    public static string GetRowValue(DataRow row, string columnName)
    {
      if (!row.Table.Columns.Contains(columnName))
        return string.Empty;

      var value = row[columnName];
      if (value == DBNull.Value || value == null)
        return string.Empty;

      var normalized = value.ToString()?.Trim() ?? string.Empty;
      if (string.Equals(normalized, "NULL", StringComparison.OrdinalIgnoreCase))
        return string.Empty;

      return normalized;
    }

    public static bool TryParseInt(string value, out int result)
    {
      return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    public static bool TryParseDecimal(string value, out decimal result)
    {
      if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
        return true;
      return decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    public static bool TryParseLong(string value, out long result)
    {
      return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    public static bool TryParseBool(string value, out bool result)
    {
      if (bool.TryParse(value, out result))
        return true;

      var normalized = value.Trim().ToLowerInvariant();
      switch (normalized)
      {
        case "1":
        case "yes":
        case "y":
        case "ja":
        case "true":
          result = true;
          return true;
        case "0":
        case "no":
        case "n":
        case "nein":
        case "false":
          result = false;
          return true;
        default:
          result = false;
          return false;
      }
    }

    public static string NormalizeDate(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return string.Empty;
      return DateTime.TryParse(value, out var date) ? date.ToString("yyyy-MM-dd") : value.Trim();
    }

    public static void AddStringAttribute(IDictionary<string, object> attributes, string key, string value)
    {
      if (!string.IsNullOrWhiteSpace(value))
        attributes[key] = value;
    }

    public static object? GetDefault(Type type)
    {
      return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    internal string MessageAndExit(string errorMessage)
    {
      Message(errorMessage, 1);
      Environment.Exit(1);
      return null; // Unreachable Code, just for compiler
    }

    public static string OrdinanceMap(string key)
    {
      if (string.IsNullOrEmpty(key)) return "";

      var ordinanceMap = new Dictionary<string, string>
      {
        { "annex_1", "1" },
        { "annex_2", "2" },
        { "annex_1_2", "1+2" },
        { "none", "" }
      };
      return ordinanceMap.ContainsKey(key) ? ordinanceMap[key] : "";
    }

    public static string RiskClassMap(string key)
    {
      if (string.IsNullOrEmpty(key)) return "";

      var riskClassMap = new Dictionary<string, string>
      {
        { "1", "I" },
        { "2", "II" },
        { "2a", "IIa" },
        { "2b", "IIb" },
        { "3", "III" }
      };
      return riskClassMap.ContainsKey(key) ? riskClassMap[key] : "";
    }

    /// <summary>
    /// Ensures JSON can be parsed whether "data" is a single object or array.
    /// </summary>
    public class SingleOrArrayConverter<T> : JsonConverter
    {
      public override bool CanConvert(Type objectType) => objectType == typeof(List<T>);

      public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
      {
        var token = JToken.Load(reader);
        if (token.Type == JTokenType.Array)
          return token.ToObject<List<T>>(serializer) ?? [];

        var obj = token.ToObject<T>(serializer);
        return obj != null ? new List<T> { obj } : [];
      }

      public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
      {
        serializer.Serialize(writer, value);
      }
    }

    /// <summary>
    /// Exports all attributes of a root object (devices, inventories, contacts, ...) to CSV.
    /// </summary>
    public static void ToCsv<TRoot, TAttributes>(TRoot root, string filePath, Func<TRoot, IEnumerable<TAttributes>> selector)
    {
      if (root == null) return;

      var properties = typeof(TAttributes).GetProperties();
      var fileExists = File.Exists(filePath);

      using var writer = new StreamWriter(filePath, append: true);
      using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
      {
        Delimiter = ";",
        Quote = '"'
      });

      if (!fileExists)
      {
        foreach (var prop in properties)
        {
          var isLocalizable = prop.GetCustomAttributes(typeof(DeviceModels.LocalizableContentAttribute), false).Any();
          if (isLocalizable)
          {
            var sample = selector(root).FirstOrDefault();
            var dict = prop.GetValue(sample) as IDictionary<string, string>;
            if (dict != null)
            {
              foreach (var key in dict.Keys)
                csv.WriteField($"{prop.Name}_{key}");
            }
          }
          else
          {
            csv.WriteField(prop.Name);
          }
        }
        csv.NextRecord();
      }

      foreach (var attributes in selector(root))
      {
        foreach (var prop in properties)
        {
          var isLocalizable = prop.GetCustomAttributes(typeof(DeviceModels.LocalizableContentAttribute), false).Any();
          var value = prop.GetValue(attributes);

          if (isLocalizable && value is IDictionary<string, string> dict)
          {
            foreach (var key in dict.Keys)
              csv.WriteField(dict[key]);
          }
          else if (value is IList<string> stringList)
          {
            // statt JSON → einfache Liste als Semikolon-getrenntes Feld
            var joined = string.Join("; ", stringList);
            csv.WriteField(joined);
          }
          else if (value is IDictionary<string, string> generalDict)
          {
            // statt JSON → Key=Value Paare
            var joined = string.Join("; ", generalDict.Select(kv => $"{kv.Key}={kv.Value}"));
            csv.WriteField(joined);
          }
          else if (value is IDictionary<string, object> generalObjDict)
          {
            // für deine service_intervals / issue_statistics
            var joined = string.Join("; ", generalObjDict.Select(kv => $"{kv.Key}={kv.Value}"));
            csv.WriteField(joined);
          }
          else
          {
            csv.WriteField(value);
          }
        }
        csv.NextRecord();
      }
    }

    public static void ExportDataSetToCsv(DataSet ds, string filePath, string tableName)
    {
      var table = ds.Tables[tableName] ?? throw new ArgumentException($"Table '{tableName}' does not exist in the DataSet.", nameof(tableName));
      var fileExists = File.Exists(filePath);

      using var writer = new StreamWriter(filePath, append: true, Encoding.UTF8);
      using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
      {
        Delimiter = ";",
        Quote = '"'
      });
      // Header nur einmal schreiben, wenn Datei neu erstellt wird
      if (!fileExists)
      {
        foreach (DataColumn column in table.Columns)
        {
          csv.WriteField(column.ColumnName);
        }
        csv.NextRecord();
      }

      // Rows schreiben
      foreach (DataRow row in table.Rows)
      {
        foreach (DataColumn column in table.Columns)
        {
          csv.WriteField(row[column]?.ToString());
        }
        csv.NextRecord();
      }
    }

  }

  public class JsonGeneric
  {
    public class JsonApiOptions
    {
      [JsonProperty("limit")]
      public int Limit { get; set; }

      [JsonProperty("page")]
      public int Page { get; set; }
    }

    public class Meta
    {
      [JsonProperty("total")]
      public int Total { get; set; }

      [JsonProperty("json_api_options")]
      public JsonApiOptions? JsonApiOptions { get; set; }

      [JsonProperty("locale")]
      public string? Locale { get; set; }

      [JsonProperty("msg")]
      public Msg? Msg { get; set; }
    }

    public class Msg
    {
      [JsonProperty("success")]
      public bool Success { get; set; }

      [JsonProperty("error")]
      public string? Error { get; set; }
      [JsonProperty("message")]
      public string? Message { get; set; }
    }

    public class Data
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

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

  public enum DatabaseType
  {
    SqlServer,
    MySql,
    SQLite,
    Oracle
  }

}
