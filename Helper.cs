using System.Globalization;
using Newtonsoft.Json.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace SamedisExternalSync
{
  public class Helper
  {


    public static void ArchiveUploadCsvFiles(ISyncLog log, string uploadRoot, bool inventoriesUploadEnabled)
    {
      if (!inventoriesUploadEnabled)
      {
        log.Debug("CSV archive step skipped because inventories upload is disabled.");
        return;
      }

      if (!Directory.Exists(uploadRoot))
      {
        log.Debug($"CSV archive step skipped because upload folder does not exist: {uploadRoot}");
        return;
      }

      var sourceCsvFiles = Directory.GetFiles(uploadRoot, "*.csv", SearchOption.TopDirectoryOnly);
      if (sourceCsvFiles.Length == 0)
      {
        log.Debug($"CSV archive step skipped because no CSV files were found in {uploadRoot}.");
        return;
      }

      var uploadRootFull = Path.GetFullPath(uploadRoot)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      var parentDirectory = Directory.GetParent(uploadRootFull)?.FullName;
      var uploadRootName = Path.GetFileName(uploadRootFull);
      if (string.IsNullOrWhiteSpace(parentDirectory) || string.IsNullOrWhiteSpace(uploadRootName))
      {
        log.Warn($"CSV archive step skipped because archive path could not be determined from upload folder '{uploadRoot}'.");
        return;
      }

      var archiveRoot = Path.Combine(parentDirectory, "archive", uploadRootName);
      var archiveRootFull = Path.GetFullPath(archiveRoot)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

      if (string.Equals(uploadRootFull, archiveRootFull, StringComparison.OrdinalIgnoreCase))
      {
        log.Warn("CSV archive step skipped because archive folder resolves to the upload folder.");
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
          log.Debug($"Archived upload CSV: {Path.GetFileName(sourcePath)} -> {targetPath}");
        }
        catch (Exception ex)
        {
          archiveErrors++;
          log.Warn($"Failed to archive upload CSV '{sourcePath}' to '{targetPath}': {ex.Message}");
        }
      }

      log.Info($"CSV archive step finished. Archived: {archivedCount}, Errors: {archiveErrors}, Target: {archiveRoot}");
    }
    // Resolves the "verantwortlich" (responsible) for a request by matching an email
    // address against the incident supporters allowed for a given inventory.
    //
    // It calls GET <incidentSupporterResource> (e.g.
    //   /api/v4/tenants/{tenant_id}/inventories/{inventory_id}/incident_supporter)
    // and matches by email. A supporter may be an internal contact, a staff member,
    // or an external contact (enterprise tenant); the returned Type carries that
    // distinction so the caller can send responsible_id together with responsible_type.
    //
    // NOTE: the response field mapping below follows the JSON:API convention used
    // across this project (data[].attributes). If the incident_supporter payload uses
    // different attribute names, adjust the candidate keys here.
    public static ResponsibleSupporter? ResolveResponsibleByEmail(
      RequestData samedisClient,
        ITenantScope scope,
      string inventoryId,
      string email,
      IDictionary<string, ResponsibleSupporter?> cache,
      ISyncLog? logger = null)
    {
      if (string.IsNullOrWhiteSpace(email))
        return null;

      if (string.IsNullOrWhiteSpace(inventoryId))
      {
        logger?.Warn($"responsible_email '{email.Trim()}' cannot be resolved without an inventory (provide inventory_id or inventory_device_number).");
        return null;
      }

      var normalizedEmail = email.Trim();
      var cacheKey = inventoryId + "|" + normalizedEmail.ToLowerInvariant();
      if (cache.TryGetValue(cacheKey, out var cached))
        return cached;

      var incidentSupporterResource = scope.Resource($"inventories/{inventoryId}/incident_supporter");
      ResponsibleSupporter? result = null;
      var response = samedisClient.Get(incidentSupporterResource);

      if (samedisClient.StatusCode >= 200 && samedisClient.StatusCode < 300 && !string.IsNullOrWhiteSpace(response))
      {
        try
        {
          var root = JToken.Parse(response);
          var data = root["data"];
          if (data != null)
          {
            IEnumerable<JToken> entries = data.Type == JTokenType.Array ? data.Children() : new[] { data };
            foreach (var entry in entries)
            {
              var attrs = entry["attributes"];
              var entryEmail =
                attrs?["email"]?.ToString() ??
                attrs?["contact_email"]?.ToString() ??
                attrs?["user_email"]?.ToString();

              if (string.IsNullOrWhiteSpace(entryEmail))
                continue;
              if (!string.Equals(entryEmail.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
                continue;

              result = new ResponsibleSupporter
              {
                Id = attrs?["responsible_id"]?.ToString() ?? entry["id"]?.ToString() ?? string.Empty,
                Type = attrs?["responsible_type"]?.ToString() ?? attrs?["type"]?.ToString() ?? entry["type"]?.ToString() ?? string.Empty,
                Email = entryEmail.Trim(),
                Name = attrs?["responsible_name"]?.ToString() ?? attrs?["name"]?.ToString() ?? string.Empty
              };
              break;
            }
          }
        }
        catch
        {
          result = null;
        }
      }

      if (result == null || string.IsNullOrWhiteSpace(result.Id))
        logger?.Warn($"responsible_email '{normalizedEmail}' did not match any incident supporter for inventory '{inventoryId}'.");

      cache[cacheKey] = result;
      return result;
    }

    // Resolved incident supporter used as the request's "verantwortlich" (responsible).
    public class ResponsibleSupporter
    {
      public string Id { get; set; } = string.Empty;
      public string Type { get; set; } = string.Empty;
      public string Email { get; set; } = string.Empty;
      public string Name { get; set; } = string.Empty;
    }




    public static string NormalizeDate(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return string.Empty;
      return DateTime.TryParse(value, out var date) ? date.ToString("yyyy-MM-dd") : value.Trim();
    }


    public static object? GetDefault(Type type)
    {
      return type.IsValueType ? Activator.CreateInstance(type) : null;
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


  }

}
