using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Properties
  {
    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("tenant_id")]
      public string? TenantId { get; set; }

      [JsonProperty("title")]
      public string? Title { get; set; }

      [JsonProperty("path")]
      public string? Path { get; set; }

      [JsonProperty("notes")]
      public string? Notes { get; set; }
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

    public class Root
    {
      [JsonProperty("data")]
      [JsonConverter(typeof(JsonApi.SingleOrArrayConverter<Data>))]
      public List<Data>? Data { get; set; }
    }
    /// <summary>
    /// Resolves the property to place buildings under, creating it when asked to.
    /// </summary>
    /// <remarks>
    /// The fallback to whichever property already exists is deliberate and specific to this
    /// resource: a facility has one property in the overwhelming majority of installations,
    /// and the source's name for it rarely matches what was set up in Samedis. Matching the
    /// title first still lets a multi-property facility work.
    /// </remarks>
    public static string? ResolvePropertyId(
      IApiClient client,
      string resource,
      string propertyTitle,
      bool createOnTheFly,
      ResourceLookup lookup,
      ISyncLog log)
    {
      if (string.IsNullOrWhiteSpace(propertyTitle))
        return null;

      var normalizedTitle = propertyTitle.Trim();

      return Records.FindOrCreate(
        client, resource,
        find: () => lookup.First(
          () => lookup.ByField("title", normalizedTitle),
          () => FirstExistingProperty(client, resource, lookup, normalizedTitle, log)),
        attributes: new Dictionary<string, object?> { ["title"] = normalizedTitle },
        log, $"property '{normalizedTitle}'",
        create: createOnTheFly,
        remember: id => lookup.RememberField("title", normalizedTitle, id));
    }

    /// <summary>
    /// The first property the facility has, if any. Remembered under the requested title so
    /// the next row asking for the same title does not repeat the round trip.
    /// </summary>
    private static string? FirstExistingProperty(IApiClient client, string resource,
                                                 ResourceLookup lookup, string requestedTitle,
                                                 ISyncLog log)
    {
      var response = client.Get(resource + "?page[number]=1&page[limit]=1");
      if (!JsonApi.IsSuccess(client.StatusCode)) return null;

      var id = JsonApi.FirstDataId(response);
      if (string.IsNullOrWhiteSpace(id)) return null;

      lookup.RememberField("title", requestedTitle, id);
      log.Debug($"Using the facility's existing property for '{requestedTitle}' -> {id}");
      return id;
    }


  }
}
