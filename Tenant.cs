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
        // GET /user/tenants/{id} is gated on the API user's facility-data access
        // for the tenant; a 401/403 here usually means that permission is missing
        // while all regular /tenants/{id}/... data calls still work.
        helper.Message(
          $"Tenant settings request failed ({client.StatusCode}) for GET {resource}."
          + (client.StatusCode == 401 || client.StatusCode == 403
              ? " The sync user likely lacks facility-data access on this tenant."
              : string.Empty)
          + " Trying get_current_user fallback.",
          1,
          "WARN"
        );
        return GetSettingsViaCurrentUser(client, apiVersion, tenantId, helper);
      }

      var root = JsonConvert.DeserializeObject<Root>(response);
      var attributes = root?.Data?.Attributes;
      if (attributes == null)
      {
        helper.Message(
          "Tenant settings response had no attributes. Trying get_current_user fallback.",
          1,
          "WARN"
        );
        return GetSettingsViaCurrentUser(client, apiVersion, tenantId, helper);
      }

      return new Settings
      {
        TenantId = attributes.TenantId ?? attributes.Id ?? root?.Data?.Id ?? tenantId,
        Name = attributes.Name ?? string.Empty,
        UseExtendedDeviceLocations = attributes.UseExtendedDeviceLocations,
        UseProfitCenters = attributes.UseProfitCenters,
      };
    }

    // Fallback: GET /api/{v}/get_current_user lists the user's tenants without
    // requiring facility-data access. The tenant entries carry
    // use_extended_device_locations but NOT use_profit_centers, so profit
    // centers stay disabled on this path and that limitation is logged loudly.
    private static Settings GetSettingsViaCurrentUser(RequestData client, string apiVersion, string tenantId, Helper helper)
    {
      var resource = $"/api/{apiVersion}/get_current_user";
      var response = client.Get(resource);

      if (client.StatusCode >= 200 && client.StatusCode < 300 && !string.IsNullOrWhiteSpace(response))
      {
        try
        {
          var tenants = JObject.Parse(response).SelectToken("data.current_user.tenants") as JArray;
          var tenant = tenants?.FirstOrDefault(t => string.Equals(t["id"]?.ToString(), tenantId, StringComparison.OrdinalIgnoreCase));
          if (tenant != null)
          {
            var useExtendedDeviceLocations = tenant["use_extended_device_locations"]?.Type == JTokenType.Boolean
              && tenant.Value<bool>("use_extended_device_locations");
            helper.Message(
              $"Tenant settings resolved via get_current_user (location_mode={(useExtendedDeviceLocations ? "property" : "standard")}). "
              + "This endpoint does not expose use_profit_centers -- profit centers are DISABLED for this run; "
              + "grant the sync user facility-data access on the tenant if it uses profit centers.",
              1,
              "WARN"
            );
            return new Settings
            {
              TenantId = tenantId,
              Name = tenant["full_name"]?.ToString() ?? tenant["name"]?.ToString() ?? string.Empty,
              UseExtendedDeviceLocations = useExtendedDeviceLocations,
            };
          }

          helper.Message(
            $"get_current_user did not list tenant {tenantId} for the sync user.",
            1,
            "WARN"
          );
        }
        catch (JsonException)
        {
          // fall through to the defaults warning below
        }
      }

      helper.Message(
        "Tenant settings unavailable. Fallback to defaults: location_mode=standard, use_profit_centers=false -- "
        + "if the tenant uses profit centers or property locations, this run will sync them WRONGLY; fix the sync user's permissions first.",
        1,
        "WARN"
      );
      return new Settings { TenantId = tenantId };
    }
  }
}
