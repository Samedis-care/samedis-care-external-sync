using Newtonsoft.Json;

namespace SamedisExternalSync
{

  public class DeviceTypes
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

      [JsonProperty("title")]
      public string? Title { get; set; }

      [JsonProperty("title_with_path")]
      public string? TitleWithPath { get; set; }

      [JsonProperty("description")]
      public string? Description { get; set; }

      [JsonProperty("trust_level")]
      public string? TrustLevel { get; set; }

      [JsonProperty("title_labels")]
      [LocalizableContent]
      public Dictionary<string, string>? TitleLabels { get; set; }

      [JsonProperty("description_labels")]
      [LocalizableContent]
      public Dictionary<string, string>? DescriptionLabels { get; set; }

      [JsonProperty("has_children")]
      public bool HasChildren { get; set; }

      [JsonProperty("parents")]
      public List<Parent>? Parents { get; set; }

      [JsonProperty("tenant_name")]
      public string? TenantName { get; set; }

      [JsonProperty("parent_id")]
      public string? ParentId { get; set; }

      [JsonProperty("parent_ids")]
      public List<string>? ParentIds { get; set; }

      [JsonProperty("device_tag_ids")]
      public List<string>? DeviceTagIds { get; set; }

      [JsonProperty("embedded_device_tags")]
      public List<EmbeddedDeviceTag>? EmbeddedDeviceTags { get; set; }
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

    public class Parent
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("title")]
      public string? Title { get; set; }

      [JsonProperty("title_labels")]
      public Dictionary<string, string>? TitleLabels { get; set; }

      [JsonProperty("language")]
      public string? Language { get; set; }
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

  }
}