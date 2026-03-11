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

    public static string? ResolveDeviceTypeId(
      RequestData client,
      string resource,
      string deviceTypeTitle,
      bool createOnTheFly,
      IDictionary<string, string> deviceTypesByTitle,
      IDictionary<string, string> checkedDeviceTypes,
      Helper helper,
      string tenantId = "",
      string contextId = "",
      string contextTitle = "")
    {
      if (string.IsNullOrWhiteSpace(deviceTypeTitle))
        return null;

      var normalizedTitle = deviceTypeTitle.Trim();
      var checkedByTitleKey = "title:" + normalizedTitle;

      if (deviceTypesByTitle.TryGetValue(normalizedTitle, out var cachedDeviceTypeId))
      {
        if (!string.IsNullOrWhiteSpace(cachedDeviceTypeId))
          return cachedDeviceTypeId;

        return null;
      }

      if (checkedDeviceTypes.TryGetValue(checkedByTitleKey, out var checkedByTitle))
      {
        if (!string.IsNullOrWhiteSpace(checkedByTitle))
          return checkedByTitle;

        return null;
      }

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("title", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, normalizedTitle);

      var listResponse = client.Get(
        resource +
        $"?page[number]=1&page[limit]=1&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}"
      );

      if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(listResponse))
      {
        var listRoot = JsonConvert.DeserializeObject<Root>(listResponse);
        var foundDeviceType = listRoot?.Data?.FirstOrDefault();
        var resolvedId = foundDeviceType?.Attributes?.Id ?? foundDeviceType?.Id;
        if (!string.IsNullOrWhiteSpace(resolvedId))
        {
          deviceTypesByTitle[normalizedTitle] = resolvedId;
          checkedDeviceTypes[checkedByTitleKey] = resolvedId;
          return resolvedId;
        }
      }
      else if (client.StatusCode != 200)
      {
        helper.Message(
          $"Device type lookup request failed for '{normalizedTitle}' (status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}', context_id='{contextId}', context_title='{contextTitle}').",
          2,
          "WARN"
        );
      }

      checkedDeviceTypes[checkedByTitleKey] = string.Empty;
      deviceTypesByTitle[normalizedTitle] = string.Empty;

      if (!createOnTheFly)
        return null;

      string? ResolveTenantRootDeviceTypeId()
      {
        const string rootCacheKey = "tenant_root_device_type";
        if (checkedDeviceTypes.TryGetValue(rootCacheKey, out var cachedRootId))
          return string.IsNullOrWhiteSpace(cachedRootId) ? null : cachedRootId;

        var rootFilterBuilder = new FilterBuilder();
        rootFilterBuilder.Clear();
        if (!string.IsNullOrWhiteSpace(tenantId))
          rootFilterBuilder.Add("tenant_id", FilterBuilder.FilterType.Equals, FilterBuilder.Type.ObjectId, tenantId);
        rootFilterBuilder.Add("parent_id", FilterBuilder.FilterType.Empty, FilterBuilder.Type.ObjectId);

        var rootResponse = client.Get(
          resource +
          $"?page[number]=1&page[limit]=1&filter[scope]=tenant&quickfilter=&gridfilter={rootFilterBuilder.Get()}"
        );
        if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(rootResponse))
        {
          var rootList = JsonConvert.DeserializeObject<Root>(rootResponse);
          var root = rootList?.Data?.FirstOrDefault();
          var rootId = root?.Attributes?.Id ?? root?.Id;
          if (!string.IsNullOrWhiteSpace(rootId))
          {
            checkedDeviceTypes[rootCacheKey] = rootId;
            return rootId;
          }
        }

        checkedDeviceTypes[rootCacheKey] = string.Empty;
        return null;
      }

      var tenantRootDeviceTypeId = ResolveTenantRootDeviceTypeId();
      if (string.IsNullOrWhiteSpace(tenantRootDeviceTypeId))
      {
        helper.Message(
          $"Failed to create device type '{normalizedTitle}' because tenant root device type could not be resolved (context_id='{contextId}', context_title='{contextTitle}').",
          1,
          "WARN"
        );
        return null;
      }

      string? ResolveTenantByTitle()
      {
        var tenantFilterBuilder = new FilterBuilder();
        tenantFilterBuilder.Clear();
        tenantFilterBuilder.Add("title", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, normalizedTitle);
        if (!string.IsNullOrWhiteSpace(tenantId))
          tenantFilterBuilder.Add("tenant_id", FilterBuilder.FilterType.Equals, FilterBuilder.Type.ObjectId, tenantId);

        var tenantListResponse = client.Get(
          resource +
          $"?page[number]=1&page[limit]=1&filter[scope]=tenant&quickfilter=&gridfilter={tenantFilterBuilder.Get()}"
        );
        if (client.StatusCode != 200 || string.IsNullOrWhiteSpace(tenantListResponse))
          return null;

        var tenantListRoot = JsonConvert.DeserializeObject<Root>(tenantListResponse);
        var tenantMatch = tenantListRoot?.Data?.FirstOrDefault();
        var tenantMatchId = tenantMatch?.Attributes?.Id ?? tenantMatch?.Id;
        return string.IsNullOrWhiteSpace(tenantMatchId) ? null : tenantMatchId;
      }

      var createPayloads = new[]
      {
        JsonConvert.SerializeObject(new
        {
          data = new Dictionary<string, object?>
          {
            ["title"] = normalizedTitle,
            ["parent_id"] = tenantRootDeviceTypeId
          }
        }),
        JsonConvert.SerializeObject(new
        {
          data = new Dictionary<string, object?>
          {
            ["attributes"] = new Dictionary<string, object?>
            {
              ["title"] = normalizedTitle,
              ["parent_id"] = tenantRootDeviceTypeId
            }
          }
        }),
        JsonConvert.SerializeObject(new
        {
          data = new Dictionary<string, object?>
          {
            ["title"] = normalizedTitle,
            ["title_labels"] = new Dictionary<string, string>
            {
              ["de"] = normalizedTitle
            },
            ["parent_id"] = tenantRootDeviceTypeId
          }
        })
      };

      string? createResponse = null;
      string? newDeviceTypeId = null;

      foreach (var createPayload in createPayloads)
      {
        createResponse = client.Post(resource, createPayload);
        if (client.StatusCode < 200 || client.StatusCode >= 300)
          continue;

        newDeviceTypeId = Helper.ExtractDataId(createResponse);
        if (!string.IsNullOrWhiteSpace(newDeviceTypeId))
          break;

        newDeviceTypeId = ResolveTenantByTitle();
        if (!string.IsNullOrWhiteSpace(newDeviceTypeId))
          break;
      }

      if (string.IsNullOrWhiteSpace(newDeviceTypeId))
      {
        helper.Message(
          $"Failed to create device type (title='{normalizedTitle}', context_id='{contextId}', context_title='{contextTitle}', status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}'). Response: {createResponse}",
          1,
          "WARN"
        );
        return null;
      }

      deviceTypesByTitle[normalizedTitle] = newDeviceTypeId;
      checkedDeviceTypes[checkedByTitleKey] = newDeviceTypeId;
      helper.Message($"Device type created on the fly: '{normalizedTitle}' -> {newDeviceTypeId}", 2);
      return newDeviceTypeId;
    }

  }
}
