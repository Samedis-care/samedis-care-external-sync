using System.Diagnostics;
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

      // This endpoint only requires authentication and membership in the tenant
      // (`show` is a public cando), so a failure here is a configuration or
      // connectivity problem -- never a missing permission the sync could work
      // around. There is deliberately no fallback: guessing use_profit_centers
      // or use_extended_device_locations sends every inventory down the wrong
      // location/profit-center path and corrupts the tenant's data silently.
      if (client.StatusCode < 200 || client.StatusCode >= 300 || string.IsNullOrWhiteSpace(response))
      {
        var reason = client.StatusCode switch
        {
          401 => " The sync user is not authenticated -- the API rejected the bearer token."
                 + " Re-authenticate and check authentication.client_id and authentication.client_secret in config.yml.",
          403 => " The sync user is authenticated but is not a member of this tenant."
                 + " Check samedis.tenant_id in config.yml and the user's tenant assignment.",
          _ => string.Empty
        };
        helper.MessageAndExit(
          $"Sync stopped. Tenant settings request failed ({client.StatusCode}) for GET {resource}.{reason}"
          + ApiErrorSuffix(response)
        );
        throw new UnreachableException();
      }

      var root = JsonConvert.DeserializeObject<Root>(response);
      var attributes = root?.Data?.Attributes;
      if (attributes == null)
      {
        helper.MessageAndExit(
          $"Sync stopped. Tenant settings response from GET {resource} could not be parsed (no attributes)."
          + " Tenant settings must not be guessed -- fix the API response before syncing."
          + ApiErrorSuffix(response)
        );
        throw new UnreachableException();
      }

      return new Settings
      {
        TenantId = attributes.TenantId ?? attributes.Id ?? root?.Data?.Id ?? tenantId,
        Name = attributes.Name ?? string.Empty,
        UseExtendedDeviceLocations = attributes.UseExtendedDeviceLocations,
        UseProfitCenters = attributes.UseProfitCenters,
      };
    }

    // The API reports the actual cause in meta.msg.message; append it when the
    // body is parseable so the operator does not have to reproduce the request.
    private static string ApiErrorSuffix(string? response)
    {
      if (string.IsNullOrWhiteSpace(response))
        return string.Empty;

      try
      {
        var message = JsonConvert.DeserializeObject<JsonGeneric.Root>(response)?.Meta?.Msg?.Message;
        return string.IsNullOrWhiteSpace(message) ? string.Empty : $" API message: {message}";
      }
      catch (JsonException)
      {
        return string.Empty;
      }
    }
  }
}
