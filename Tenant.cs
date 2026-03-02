using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SamedisExternalSync
{
  public class Tenant
  {
    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("tenant_id")]
      public string? TenantId { get; set; }

      [JsonProperty("name")]
      public string? Name { get; set; }

      [JsonProperty("default_locale")]
      public string? DefaultLocale { get; set; }

      [JsonProperty("language")]
      public string? Language { get; set; }

      [JsonProperty("required_inventory_fields")]
      public List<string>? RequiredInventoryFields { get; set; }

      [JsonProperty("use_extended_device_locations", NullValueHandling = NullValueHandling.Ignore)]
      public bool UseExtendedDeviceLocations { get; set; } = false;

      [JsonProperty("use_profit_centers", NullValueHandling = NullValueHandling.Ignore)]
      public bool UseProfitCenters { get; set; } = false;

      // Keep unknown tenant settings available for future use without changing the model each time.
      [JsonExtensionData]
      public IDictionary<string, JToken>? AdditionalSettings { get; set; }
    }

    public class Data
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("type")]
      public string? Type { get; set; }

      [JsonProperty("attributes")]
      public Attributes? Attributes { get; set; }

      [JsonProperty("links")]
      public Dictionary<string, object>? Links { get; set; }
    }

    public class Msg
    {
      [JsonProperty("success")]
      public bool Success { get; set; }

      [JsonProperty("message")]
      public string? Message { get; set; }

      [JsonProperty("error")]
      public string? Error { get; set; }

      [JsonProperty("error_details")]
      public string? ErrorDetails { get; set; }
    }

    public class JsonApiOptions
    {
      [JsonProperty("padding")]
      public int? Padding { get; set; }

      [JsonProperty("cursor")]
      public string? Cursor { get; set; }

      [JsonProperty("include")]
      public List<object>? Include { get; set; }

      [JsonProperty("fields")]
      public Dictionary<string, object>? Fields { get; set; }
    }

    public class Meta
    {
      [JsonProperty("git_version")]
      public string? GitVersion { get; set; }

      [JsonProperty("json_api_options")]
      public JsonApiOptions? JsonApiOptions { get; set; }

      [JsonProperty("locale")]
      public string? Locale { get; set; }

      [JsonProperty("current_user_id")]
      public string? CurrentUserId { get; set; }

      [JsonProperty("msg")]
      public Msg? Msg { get; set; }
    }

    public class Root
    {
      [JsonProperty("data")]
      public Data? Data { get; set; }

      [JsonProperty("meta")]
      public Meta? Meta { get; set; }
    }

    public class Settings
    {
      public string TenantId { get; init; } = string.Empty;
      public string Name { get; init; } = string.Empty;
      public bool UseExtendedDeviceLocations { get; init; } = false;
      public bool UseProfitCenters { get; init; } = false;
      public string LocationMode => UseExtendedDeviceLocations ? "property" : "standard";
    }

    public static Settings GetSettings(RequestData client, string apiVersion, string tenantId, Helper helper)
    {
      var resource = $"/api/{apiVersion}/user/tenants/{tenantId}";
      var response = client.Get(resource);

      if (client.StatusCode < 200 || client.StatusCode >= 300 || string.IsNullOrWhiteSpace(response))
      {
        helper.Message(
          $"Tenant settings request failed ({client.StatusCode}). Fallback to defaults: location_mode=standard, use_profit_centers=false.",
          1,
          "WARN"
        );
        return new Settings { TenantId = tenantId };
      }

      var root = JsonConvert.DeserializeObject<Root>(response);
      var attributes = root?.Data?.Attributes;
      if (attributes == null)
      {
        helper.Message(
          "Tenant settings response had no attributes. Fallback to defaults: location_mode=standard, use_profit_centers=false.",
          1,
          "WARN"
        );
        return new Settings { TenantId = tenantId };
      }

      return new Settings
      {
        TenantId = attributes.TenantId ?? attributes.Id ?? root?.Data?.Id ?? tenantId,
        Name = attributes.Name ?? string.Empty,
        UseExtendedDeviceLocations = attributes.UseExtendedDeviceLocations,
        UseProfitCenters = attributes.UseProfitCenters,
      };
    }
  }
}
