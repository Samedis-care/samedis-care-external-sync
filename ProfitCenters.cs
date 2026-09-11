using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class ProfitCenters
  {
    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("tenant_id")]
      public string? TenantId { get; set; }

      [JsonProperty("title")]
      public string? Title { get; set; }

      [JsonProperty("department_ids")]
      public List<string>? DepartmentIds { get; set; }
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
    /// Links a department to a profit centre, once per pair and per run.
    /// </summary>
    /// <param name="linkedDepartments">
    /// Which pairs were already handled this run. Not an API lookup cache: it records the
    /// outcome of a write, keyed by the pair, so the same link is not attempted twice.
    /// </param>
    public static bool EnsureDepartmentAssigned(
      RequestData client,
      string resource,
      string profitCenterId,
      string departmentId,
      IDictionary<string, string> linkedDepartments,
      ISyncLog log)
    {
      if (string.IsNullOrWhiteSpace(profitCenterId) || string.IsNullOrWhiteSpace(departmentId))
        return false;

      var linkKey = "link:" + profitCenterId + ":" + departmentId;
      if (linkedDepartments.TryGetValue(linkKey, out var checkedValue))
        return !string.IsNullOrWhiteSpace(checkedValue);

      var detailResponse = client.Get(resource + "/" + Uri.EscapeDataString(profitCenterId));
      if (client.StatusCode < 200 || client.StatusCode >= 300 || string.IsNullOrWhiteSpace(detailResponse))
      {
        linkedDepartments[linkKey] = string.Empty;
        log.Warn($"Profit center link check failed (profit_center_id='{profitCenterId}', department_id='{departmentId}', status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}').");
        return false;
      }

      var detailRoot = JsonConvert.DeserializeObject<ProfitCenters.Root>(detailResponse);
      var detailData = detailRoot?.Data?.FirstOrDefault();
      var attributes = detailData?.Attributes;
      var currentDepartmentIds = attributes?.DepartmentIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList() ?? new List<string>();

      if (currentDepartmentIds.Any(id => string.Equals(id, departmentId, StringComparison.OrdinalIgnoreCase)))
      {
        linkedDepartments[linkKey] = departmentId;
        return true;
      }

      currentDepartmentIds.Add(departmentId);
      var payloadData = new Dictionary<string, object?>
      {
        ["department_ids"] = currentDepartmentIds
      };
      if (!string.IsNullOrWhiteSpace(attributes?.Title))
        payloadData["title"] = attributes.Title;

      var updatePayload = JsonConvert.SerializeObject(new
      {
        data = payloadData
      });

      var updateResponse = client.Put(resource, profitCenterId, updatePayload);
      if (client.StatusCode >= 200 && client.StatusCode < 300)
      {
        linkedDepartments[linkKey] = departmentId;
        log.Debug($"Profit center linked to department (profit_center_id='{profitCenterId}', department_id='{departmentId}').");
        return true;
      }

      linkedDepartments[linkKey] = string.Empty;
      log.Warn($"Failed to link profit center to department (profit_center_id='{profitCenterId}', department_id='{departmentId}', status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}'). Response: {updateResponse}");
      return false;
    }
    /// <summary>
    /// Resolves a profit centre by title, creating it when asked to.
    /// </summary>
    public static string? ResolveProfitCenterId(
      IApiClient client,
      string resource,
      string profitCenterTitle,
      bool createOnTheFly,
      string contextId,
      string contextTitle,
      ResourceLookup lookup,
      ISyncLog log)
    {
      if (string.IsNullOrWhiteSpace(profitCenterTitle))
        return null;

      var normalizedTitle = profitCenterTitle.Trim();

      return Records.FindOrCreate(
        client, resource,
        find: () => lookup.ByField("title", normalizedTitle),
        attributes: new Dictionary<string, object?> { ["title"] = normalizedTitle },
        log, $"profit center '{normalizedTitle}' (context_id='{contextId}', context_title='{contextTitle}')",
        create: createOnTheFly,
        remember: id => lookup.RememberField("title", normalizedTitle, id));
    }


  }
}
