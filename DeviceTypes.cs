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
      [JsonConverter(typeof(JsonApi.SingleOrArrayConverter<Data>))]
      public List<Data>? Data { get; set; }

      [JsonProperty("meta")]
      public Meta? Meta { get; set; }
    }
    private const string PublicAndTenant = "filter[scope]=public_and_tenant";
    private const string TenantOnly = "filter[scope]=tenant";

    /// <summary>
    /// Resolves a device type by title, creating it under the facility's own root when asked
    /// to.
    /// </summary>
    /// <remarks>
    /// The lookup searches the public catalog and the facility's own types; a create can only
    /// go under the facility's root node, which the server maintains and which is found as the
    /// tenant-scoped type without a parent.
    /// </remarks>
    public static string? ResolveDeviceTypeId(
      IApiClient client,
      string resource,
      string deviceTypeTitle,
      bool createOnTheFly,
      ResourceLookup lookup,
      ISyncLog log,
      string tenantId = "",
      string contextId = "",
      string contextTitle = "")
    {
      if (string.IsNullOrWhiteSpace(deviceTypeTitle))
        return null;

      var normalizedTitle = deviceTypeTitle.Trim();
      var where = $"(context_id='{contextId}', context_title='{contextTitle}')";

      var existing = lookup.ByField("title", normalizedTitle,
                                    FilterBuilder.FilterType.Equals, PublicAndTenant);
      if (!string.IsNullOrWhiteSpace(existing))
        return existing;

      if (!createOnTheFly)
        return null;

      // The parent is a hint, not a requirement. The create endpoint calls
      // Tenant#ensure_type_catalog_tenant_node itself and anchors the new type under the
      // facility's root node, creating that node when it is missing; a parent_id it cannot
      // resolve is rescued onto the same node. So a root that does not answer here is worth
      // noting and nothing more.
      //
      // It used to abort instead, and that turned a situation the server heals by itself
      // into a total import failure: no device types, therefore no device models, therefore
      // every inventory skipped, and with them every task and every training -- all reported
      // as "Skipped" with "Errors: 0", because from each row's point of view the data was
      // simply unresolvable.
      var attributes = new Dictionary<string, object?> { ["title"] = normalizedTitle };

      var rootId = ResolveTenantRootDeviceTypeId(lookup, tenantId);
      if (string.IsNullOrWhiteSpace(rootId))
        log.Debug($"Root device type of the facility did not resolve; letting the server anchor '{normalizedTitle}' {where}.");
      else
        attributes["parent_id"] = rootId;

      var created = Records.Create(client, resource, attributes,
                                   log, $"device type '{normalizedTitle}' {where}");

      // The server can accept the create and answer without an id. The record exists at that
      // point, so it is looked up in the facility's own scope rather than reported as lost.
      created ??= lookup.ByFields(TenantConditions(normalizedTitle, tenantId),
                                  FilterBuilder.FilterType.Equals, TenantOnly);

      if (!string.IsNullOrWhiteSpace(created))
        lookup.RememberField("title", normalizedTitle, created,
                             FilterBuilder.FilterType.Equals, PublicAndTenant);

      return created;
    }

    /// <summary>
    /// The facility's own root device type: the tenant-scoped entry with no parent. Everything
    /// this sync creates hangs below it.
    /// </summary>
    private static string? ResolveTenantRootDeviceTypeId(ResourceLookup lookup, string tenantId)
    {
      // A mixed set: the tenant is compared by value, the parent is asserted to be absent.
      var conditions = string.IsNullOrWhiteSpace(tenantId)
        ? new[] { Condition.Empty("parent_id", FilterBuilder.Type.ObjectId) }
        : new[]
          {
            Condition.Id("tenant_id", tenantId),
            Condition.Empty("parent_id", FilterBuilder.Type.ObjectId),
          };

      return lookup.ByConditions(conditions, TenantOnly);
    }

    /// <summary>
    /// Declared once so the lookup and the cache seeding cannot drift apart.
    /// </summary>
    private static (string Field, string? Value)[] TenantConditions(string title, string tenantId)
      => string.IsNullOrWhiteSpace(tenantId)
        ? new (string, string?)[] { ("title", title) }
        : new (string, string?)[] { ("title", title), ("tenant_id", tenantId) };


  }
}
