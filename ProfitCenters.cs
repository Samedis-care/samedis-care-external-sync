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
      [JsonConverter(typeof(Helper.SingleOrArrayConverter<Data>))]
      public List<Data>? Data { get; set; }
    }

    public static string? ResolveProfitCenterId(
      RequestData client,
      string resource,
      string profitCenterTitle,
      bool createOnTheFly,
      string contextId,
      string contextTitle,
      IDictionary<string, string> profitCentersByTitle,
      IDictionary<string, string> checkedProfitCenters,
      Helper helper)
    {
      if (string.IsNullOrWhiteSpace(profitCenterTitle))
        return null;

      var normalizedTitle = profitCenterTitle.Trim();
      var checkedKey = "title:" + normalizedTitle;

      if (profitCentersByTitle.TryGetValue(normalizedTitle, out var cachedProfitCenterId))
      {
        if (!string.IsNullOrWhiteSpace(cachedProfitCenterId))
          return cachedProfitCenterId;

        return null;
      }

      if (checkedProfitCenters.TryGetValue(checkedKey, out var checkedByTitle))
      {
        if (!string.IsNullOrWhiteSpace(checkedByTitle))
          return checkedByTitle;

        return null;
      }

      var filterBuilder = new FilterBuilder();
      filterBuilder.Clear();
      filterBuilder.Add("title", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, normalizedTitle);

      var listResponse = client.Get(resource + $"?page[number]=1&page[limit]=1&gridfilter={filterBuilder.Get()}");
      if (client.StatusCode == 200 && !string.IsNullOrWhiteSpace(listResponse))
      {
        var listRoot = JsonConvert.DeserializeObject<ProfitCenters.Root>(listResponse);
        var foundProfitCenter = listRoot?.Data?.FirstOrDefault();
        var resolvedId = foundProfitCenter?.Attributes?.Id ?? foundProfitCenter?.Id;
        if (!string.IsNullOrWhiteSpace(resolvedId))
        {
          profitCentersByTitle[normalizedTitle] = resolvedId;
          checkedProfitCenters[checkedKey] = resolvedId;
          return resolvedId;
        }
      }
      else if (client.StatusCode != 200)
      {
        helper.Message(
          $"Profit center lookup request failed for '{normalizedTitle}' (status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}', context_id='{contextId}', context_title='{contextTitle}').",
          2,
          "WARN"
        );
      }

      checkedProfitCenters[checkedKey] = string.Empty;
      profitCentersByTitle[normalizedTitle] = string.Empty;

      if (!createOnTheFly)
        return null;

      var payload = JsonConvert.SerializeObject(new
      {
        data = new Dictionary<string, object?>
        {
          ["title"] = normalizedTitle
        }
      });

      var response = client.Post(resource, payload);
      if (client.StatusCode < 200 || client.StatusCode >= 300)
      {
        helper.Message(
          $"Failed to create profit center (title='{normalizedTitle}', context_id='{contextId}', context_title='{contextTitle}', status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}'). Response: {response}",
          1,
          "WARN"
        );
        return null;
      }

      var newProfitCenterId = Helper.ExtractDataId(response);
      if (string.IsNullOrWhiteSpace(newProfitCenterId))
      {
        helper.Message(
          $"Failed to create profit center (title='{normalizedTitle}', context_id='{contextId}', context_title='{contextTitle}'): API returned no profit center id.",
          1,
          "WARN"
        );
        return null;
      }

      profitCentersByTitle[normalizedTitle] = newProfitCenterId;
      checkedProfitCenters[checkedKey] = newProfitCenterId;
      helper.Message($"Profit center created on the fly: '{normalizedTitle}' -> {newProfitCenterId}", 2);
      return newProfitCenterId;
    }

    public static bool EnsureDepartmentAssigned(
      RequestData client,
      string resource,
      string profitCenterId,
      string departmentId,
      IDictionary<string, string> checkedProfitCenters,
      Helper helper)
    {
      if (string.IsNullOrWhiteSpace(profitCenterId) || string.IsNullOrWhiteSpace(departmentId))
        return false;

      var linkKey = "link:" + profitCenterId + ":" + departmentId;
      if (checkedProfitCenters.TryGetValue(linkKey, out var checkedValue))
        return !string.IsNullOrWhiteSpace(checkedValue);

      var detailResponse = client.Get(resource + "/" + Uri.EscapeDataString(profitCenterId));
      if (client.StatusCode < 200 || client.StatusCode >= 300 || string.IsNullOrWhiteSpace(detailResponse))
      {
        checkedProfitCenters[linkKey] = string.Empty;
        helper.Message(
          $"Profit center link check failed (profit_center_id='{profitCenterId}', department_id='{departmentId}', status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}').",
          1,
          "WARN"
        );
        return false;
      }

      var detailRoot = JsonConvert.DeserializeObject<ProfitCenters.Root>(detailResponse);
      var detailData = detailRoot?.Data?.FirstOrDefault();
      var attributes = detailData?.Attributes;
      var currentDepartmentIds = attributes?.DepartmentIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList() ?? new List<string>();

      if (currentDepartmentIds.Any(id => string.Equals(id, departmentId, StringComparison.OrdinalIgnoreCase)))
      {
        checkedProfitCenters[linkKey] = departmentId;
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
        checkedProfitCenters[linkKey] = departmentId;
        helper.Message(
          $"Profit center linked to department (profit_center_id='{profitCenterId}', department_id='{departmentId}').",
          2
        );
        return true;
      }

      checkedProfitCenters[linkKey] = string.Empty;
      helper.Message(
        $"Failed to link profit center to department (profit_center_id='{profitCenterId}', department_id='{departmentId}', status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}'). Response: {updateResponse}",
        1,
        "WARN"
      );
      return false;
    }
  }
}
