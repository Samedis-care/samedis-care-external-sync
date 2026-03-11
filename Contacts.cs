using System.Data;
using Newtonsoft.Json;

namespace SamedisExternalSync
{
  public class Contacts
  {
    public class Attributes
    {
      [JsonProperty("id")]
      public string? Id { get; set; }

      [JsonProperty("created_at")]
      public DateTime? CreatedAt { get; set; }

      [JsonProperty("updated_at")]
      public DateTime? UpdatedAt { get; set; }

      [JsonProperty("created_by_user")]
      public string? CreatedByUser { get; set; }

      [JsonProperty("updated_by_user")]
      public string? UpdatedByUser { get; set; }

      [JsonProperty("_type")]
      public string? Type { get; set; }

      [JsonProperty("contact_type")]
      public string? ContactType { get; set; }

      [JsonProperty("salutation")]
      public string? Salutation { get; set; }

      [JsonProperty("name")]
      public string? Name { get; set; }

      [JsonProperty("email")]
      public string? Email { get; set; }

      [JsonProperty("phone")]
      public string? Phone { get; set; }

      [JsonProperty("url")]
      public string? Url { get; set; }

      [JsonProperty("fax")]
      public string? Fax { get; set; }

      [JsonProperty("zip")]
      public string? Zip { get; set; }

      [JsonProperty("street")]
      public string? Street { get; set; }

      [JsonProperty("town")]
      public string? Town { get; set; }

      [JsonProperty("first_name")]
      public string? FirstName { get; set; }

      [JsonProperty("last_name")]
      public string? LastName { get; set; }

      [JsonProperty("mobile")]
      public string? Mobile { get; set; }

      [JsonProperty("status")]
      public string? Status { get; set; }

      [JsonProperty("is_public")]
      public bool IsPublic { get; set; }

      [JsonProperty("trust_level")]
      public string? TrustLevel { get; set; }

      [JsonProperty("categories")]
      public List<string>? Categories { get; set; }

      [JsonProperty("country")]
      public string? Country { get; set; }

      [JsonProperty("notes")]
      public string? Notes { get; set; }

      [JsonProperty("company_id")]
      public string? CompanyId { get; set; }

      [JsonProperty("ident_user_id")]
      public string? IdentUserId { get; set; }

      [JsonProperty("avatar")]
      public string? Avatar { get; set; }

      [JsonProperty("user_avatar")]
      public string? UserAvatar { get; set; }

      [JsonProperty("company_name")]
      public string? CompanyName { get; set; }
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

    public class Msg
    {
      [JsonProperty("success")]
      public bool Success { get; set; }

      [JsonProperty("message")]
      public string? Message { get; set; }
    }

    public class Fields { }

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

    public class ListRoot
    {
      [JsonProperty("data")]
      [JsonConverter(typeof(Helper.SingleOrArrayConverter<Data>))]
      public List<Data>? Data { get; set; }

      [JsonProperty("meta")]
      public Meta? Meta { get; set; }
    }

    public static string? ResolveCompanyContactId(
      RequestData client,
      string resource,
      string companyName,
      bool createOnTheFly,
      IDictionary<string, string> contactsByName,
      IDictionary<string, string> checkedContacts,
      Helper helper,
      string contextId = "",
      string contextTitle = "")
    {
      if (string.IsNullOrWhiteSpace(companyName))
        return null;

      var normalizedCompanyName = companyName.Trim();
      var checkedByNameKey = "name:" + normalizedCompanyName;

      if (contactsByName.TryGetValue(normalizedCompanyName, out var cachedContactId))
      {
        if (!string.IsNullOrWhiteSpace(cachedContactId))
          return cachedContactId;

        return null;
      }

      if (checkedContacts.TryGetValue(checkedByNameKey, out var checkedByName))
      {
        if (!string.IsNullOrWhiteSpace(checkedByName))
          return checkedByName;

        return null;
      }

      string? TryFindCompanyByLastName()
      {
        var filterBuilder = new FilterBuilder();
        filterBuilder.Clear();
        filterBuilder.Add("last_name", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, normalizedCompanyName);
        filterBuilder.Add("contact_type", FilterBuilder.FilterType.Equals, FilterBuilder.Type.Text, "company");

        var listResponse = client.Get(
          resource +
          $"?page[number]=1&page[limit]=1&filter[scope]=public_and_tenant&quickfilter=&gridfilter={filterBuilder.Get()}"
        );

        if (client.StatusCode != 200 || string.IsNullOrWhiteSpace(listResponse))
          return null;

        var listRoot = JsonConvert.DeserializeObject<ListRoot>(listResponse);
        var foundContact = listRoot?.Data?.FirstOrDefault();
        var foundId = foundContact?.Attributes?.Id ?? foundContact?.Id;
        return string.IsNullOrWhiteSpace(foundId) ? null : foundId;
      }

      var resolvedContactId = TryFindCompanyByLastName();

      if (!string.IsNullOrWhiteSpace(resolvedContactId))
      {
        contactsByName[normalizedCompanyName] = resolvedContactId;
        checkedContacts[checkedByNameKey] = resolvedContactId;
        return resolvedContactId;
      }

      checkedContacts[checkedByNameKey] = string.Empty;
      contactsByName[normalizedCompanyName] = string.Empty;

      if (!createOnTheFly)
        return null;

      var payload = JsonConvert.SerializeObject(new
      {
        data = new Dictionary<string, object?>
        {
          ["last_name"] = normalizedCompanyName,
          ["contact_type"] = "company",
          ["categories"] = new List<string> { "manufacturer" },
          ["status"] = "tenant"
        }
      });

      var createResponse = client.Post(resource, payload);
      if (client.StatusCode >= 200 && client.StatusCode < 300)
      {
        resolvedContactId = Helper.ExtractDataId(createResponse);
      }

      resolvedContactId ??= TryFindCompanyByLastName();

      if (string.IsNullOrWhiteSpace(resolvedContactId))
      {
        helper.Message(
          $"Failed to resolve/create manufacturer contact (name='{normalizedCompanyName}', context_id='{contextId}', context_title='{contextTitle}', status={client.StatusCode} {client.Status}, response_status='{client.LastResponseStatus}', error='{client.LastError}').",
          1,
          "WARN"
        );
        return null;
      }

      contactsByName[normalizedCompanyName] = resolvedContactId;
      checkedContacts[checkedByNameKey] = resolvedContactId;
      helper.Message($"Manufacturer contact created on the fly: '{normalizedCompanyName}' -> {resolvedContactId}", 2);
      return resolvedContactId;
    }

    public static DataSet CreateContactDataSet()
    {
      var ds = new DataSet("Contacts");
      var dt = new DataTable("Contacts");

      dt.Columns.Add("id", typeof(string));
      dt.Columns.Add("name", typeof(string));
      dt.Columns.Add("type", typeof(string));
      dt.Columns.Add("street", typeof(string));
      dt.Columns.Add("zip", typeof(string));
      dt.Columns.Add("town", typeof(string));
      dt.Columns.Add("country", typeof(string));
      dt.Columns.Add("phone", typeof(string));
      dt.Columns.Add("mobile", typeof(string));
      dt.Columns.Add("email", typeof(string));
      dt.Columns.Add("website", typeof(string));
      dt.Columns.Add("public", typeof(string));
      dt.Columns.Add("trust_level", typeof(string));
      dt.Columns.Add("categories", typeof(string));

      var idColumn = dt.Columns["Id"] ?? throw new InvalidOperationException("The 'Id' column was not found in the DataTable.");
      dt.PrimaryKey = [idColumn];

      ds.Tables.Add(dt);
      return ds;
    }

    public static void FillContactDataSet(DataSet ds, string json)
    {
      var root = JsonConvert.DeserializeObject<Contacts.Root>(json);
      if (root?.Data?.Attributes == null)
        return;

      var table = ds.Tables["Contacts"];
      if (table == null) return;

      var attr = root.Data.Attributes;

      if (table.Rows.Find(attr.Id) != null)
        return;

      var row = table.NewRow();

      row["id"] = attr.Id;
      row["name"] = attr.Name ?? attr.LastName ?? "";
      row["type"] = attr.ContactType ?? "";
      row["street"] = attr.Street ?? "";
      row["zip"] = attr.Zip ?? "";
      row["town"] = attr.Town ?? "";
      row["country"] = attr.Country ?? "";
      row["phone"] = attr.Phone ?? "";
      row["mobile"] = attr.Mobile ?? "";
      row["email"] = attr.Email ?? "";
      row["website"] = attr.Url ?? "";

      row["public"] = attr.IsPublic ? "Ja" : "Nein";
      row["trust_level"] = attr.TrustLevel ?? "";

      row["categories"] = attr.Categories != null
          ? string.Join(", ", attr.Categories)
          : "";

      table.Rows.Add(row);
    }

  }
}
