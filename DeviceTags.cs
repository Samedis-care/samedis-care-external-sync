using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;

namespace SamedisExternalSync
{

  public class DeviceTags
  {
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class LocalizableContentAttribute : Attribute
    {
      public LocalizableContentAttribute() { }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("external_id")]
      public string? ExternalId { get; set; }

      [JsonProperty("created_at")]
      public DateTime? CreatedAt { get; set; } = null;

      [JsonProperty("updated_at")]
      public DateTime? UpdatedAt { get; set; } = null;

      [JsonProperty("created_by_user")]
      public string? CreatedByUser { get; set; }

      [JsonProperty("updated_by_user")]
      public string? UpdatedByUser { get; set; }

      [JsonProperty("type")]
      public string? Type { get; set; }

      [JsonProperty("name")]
      public string? Name { get; set; }

      [JsonProperty("labels")]
      [LocalizableContent]
      public Dictionary<string, string>? Labels { get; set; }

      [JsonProperty("tenant_name")]
      public string? TenantName { get; set; }

      [JsonProperty("trust_level")]
      public string? TrustLevel { get; set; }
    }

    public class Data
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("type")]
      public string? Type { get; set; }

      [JsonProperty("attributes")]
      public Attributes? Attributes { get; set; }
    }

    public class JsonApiOptions
    {
      [JsonProperty("limit")]
      public int Limit { get; set; }

      [JsonProperty("page")]
      public int Page { get; set; }

      [JsonProperty("padding")]
      public int Padding { get; set; }

      [JsonProperty("include")]
      public List<object>? Include { get; set; }

      [JsonProperty("fields")]
      public Fields? Fields { get; set; }
    }

    public class Fields
    {
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

    public class Root
    {
      [JsonProperty("data")]
      public List<Data>? Data { get; set; }

      [JsonProperty("meta")]
      public Meta? Meta { get; set; }

      public static void ToCsv(string json, string filePath)
      {
        var root = JsonConvert.DeserializeObject<Root>(json);
        if (root == null) return;
        if (root.Data == null || root.Data.Count == 0) return;

        var properties = typeof(Attributes).GetProperties();
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
            var isLocalizable = prop.GetCustomAttributes(typeof(LocalizableContentAttribute), false).Length > 0;
            if (isLocalizable)
            {
              // Try to get sample data from the first entry to determine the keys
              IDictionary<string, string>? sampleData = null;
              var firstData = root.Data.FirstOrDefault();
              if (firstData?.Attributes != null)
              {
                sampleData = firstData.Attributes.GetType()
                              .GetProperty(prop.Name)?.GetValue(firstData.Attributes)
                              as IDictionary<string, string>;
              }
              if (sampleData != null)
              {
                foreach (var key in sampleData.Keys)
                {
                  csv.WriteField($"{prop.Name}_{key}");
                }
              }
            }
            else
            {
              csv.WriteField(prop.Name);
            }
          }
          csv.NextRecord();
        }

        // Write data
        foreach (var data in root.Data)
        {
          var attributes = data.Attributes;
          foreach (var prop in properties)
          {
            var isLocalizable = prop.GetCustomAttributes(typeof(LocalizableContentAttribute), false).Length > 0;
            var value = prop.GetValue(attributes);
            if (isLocalizable && value is IDictionary<string, string> dict)
            {
              foreach (var key in dict.Keys)
              {
                csv.WriteField(dict[key]);
              }
            }
            else if (value is IList<string> stringList)
            {
              csv.WriteField(JsonConvert.SerializeObject(stringList));
            }
            else if (value is IDictionary<string, string> generalDict)
            {
              csv.WriteField(JsonConvert.SerializeObject(generalDict));
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
}