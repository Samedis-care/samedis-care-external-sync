using FluentAssertions;
using SamedisCare.Api.Http;
using SamedisCare.Api.Lookup;
using SamedisCare.Helper.Logging;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// Answers lookups from a rule set and records what was posted, so a test can drive the
/// resolve-or-create path end to end without a server.
/// </summary>
internal sealed class FakeApiWithWrites : IApiClient
{
    public string LastError => string.Empty;
    public bool TestMode => false;

    private readonly Func<string, (int, string)> _get;
    private readonly Func<string, (int, string)> _post;

    public FakeApiWithWrites(Func<string, (int, string)> get, Func<string, (int, string)> post)
    {
        _get = get;
        _post = post;
    }

    public List<string> Gets { get; } = new();
    public List<(string Resource, string Content)> Posts { get; } = new();

    public int StatusCode { get; private set; }
    public string LastContent { get; private set; } = string.Empty;

    public string Get(string resource)
    {
        Gets.Add(resource);
        (StatusCode, LastContent) = _get(resource);
        return LastContent;
    }

    public string Post(string resource, string content)
    {
        Posts.Add((resource, content));
        (StatusCode, LastContent) = _post(resource);
        return LastContent;
    }

    public string Put(string r, string i, string c) => throw new NotSupportedException();
    public string PostDocument(string r, string f, string n) => throw new NotSupportedException();
}

public class TenantCatalogResolutionTests
{
    private static readonly ISyncLog Silent = new NullSyncLog();
    private const string TenantId = "507f1f77bcf86cd799439001";

    private static (int, string) Found(string id) => (200, $"{{\"data\":[{{\"id\":\"{id}\"}}],\"meta\":{{\"total\":1}}}}");
    private static (int, string) NotFound() => (404, "{\"meta\":{\"msg\":{\"success\":false,\"error\":\"record_not_found_error\"}}}");

    private static string? Resolve(FakeApiWithWrites api, IDictionary<string, string> memo,
                                   string title = "Seca 954", string maker = "seca",
                                   string type = "Waage")
        => DeviceModels.ResolveOrCreateTenantCatalogIdForInventory(
             api, TenantId, title, maker, type,
             new ResourceLookup(api, "device_models"),
             new ResourceLookup(api, "device_types"),
             new ResourceLookup(api, "contacts"),
             memo, Silent);

    // A model that already exists must not be created a second time.
    [Fact]
    public void An_existing_model_is_returned_without_a_create()
    {
        var api = new FakeApiWithWrites(
            get: url => url.StartsWith("device_models") ? Found("model-1")
               : url.StartsWith("device_types") ? Found("type-1")
               : Found("maker-1"),
            post: _ => (201, "{\"data\":{\"id\":\"should-not-happen\"}}"));

        Resolve(api, new Dictionary<string, string>()).Should().Be("model-1");
        api.Posts.Should().BeEmpty();
    }

    [Fact]
    public void A_missing_model_is_created()
    {
        var api = new FakeApiWithWrites(
            get: url => url.StartsWith("device_models") ? NotFound()
               : url.StartsWith("device_types") ? Found("type-1")
               : Found("maker-1"),
            post: _ => (201, "{\"data\":{\"id\":\"new-model\"}}"));

        Resolve(api, new Dictionary<string, string>()).Should().Be("new-model");
        api.Posts.Should().ContainSingle().Which.Resource.Should().Be("device_models");
    }

    // Samedis rejects a create when an identical model already exists and names it in
    // meta.msg.error_details. Failing there would leave the inventory without a model.
    [Fact]
    public void A_create_rejected_as_a_duplicate_reuses_the_named_model()
    {
        var api = new FakeApiWithWrites(
            get: url => url.StartsWith("device_models") ? NotFound()
               : url.StartsWith("device_types") ? Found("type-1")
               : Found("maker-1"),
            post: _ => (422, "{\"meta\":{\"msg\":{\"error_details\":{\"duplicate_of\":\"existing-1\"}}}}"));

        Resolve(api, new Dictionary<string, string>()).Should().Be("existing-1");
    }

    [Fact]
    public void A_public_duplicate_is_reused_as_well()
    {
        var api = new FakeApiWithWrites(
            get: url => url.StartsWith("device_models") ? NotFound()
               : url.StartsWith("device_types") ? Found("type-1")
               : Found("maker-1"),
            post: _ => (422, "{\"meta\":{\"msg\":{\"error_details\":{\"public_duplicate_of\":\"public-1\"}}}}"));

        Resolve(api, new Dictionary<string, string>()).Should().Be("public-1");
    }

    // A model cannot be created without these, and neither can be invented.
    [Theory]
    [InlineData("", "seca", "Waage")]
    [InlineData("Seca 954", "", "Waage")]
    [InlineData("Seca 954", "seca", "")]
    public void An_incomplete_row_resolves_nothing(string title, string maker, string type)
    {
        var api = new FakeApiWithWrites(_ => Found("x"), _ => (201, "{\"data\":{\"id\":\"x\"}}"));

        Resolve(api, new Dictionary<string, string>(), title, maker, type).Should().BeNull();
        api.Posts.Should().BeEmpty();
    }

    // The memo exists so an unresolvable row does not repeat the device-type and manufacturer
    // round trips -- and the warning -- for every further row like it.
    [Fact]
    public void An_unresolvable_row_is_not_retried()
    {
        var api = new FakeApiWithWrites(_ => Found("x"), _ => (201, "{}"));
        var memo = new Dictionary<string, string>();

        Resolve(api, memo, maker: "").Should().BeNull();
        var afterFirst = api.Gets.Count;

        Resolve(api, memo, maker: "").Should().BeNull();
        api.Gets.Should().HaveCount(afterFirst);
    }

    [Fact]
    public void A_resolved_row_is_answered_from_the_memo_the_second_time()
    {
        var api = new FakeApiWithWrites(
            get: url => url.StartsWith("device_models") ? Found("model-1")
               : url.StartsWith("device_types") ? Found("type-1")
               : Found("maker-1"),
            post: _ => (201, "{}"));
        var memo = new Dictionary<string, string>();

        Resolve(api, memo).Should().Be("model-1");
        var afterFirst = api.Gets.Count;

        Resolve(api, memo).Should().Be("model-1");
        api.Gets.Should().HaveCount(afterFirst);
    }

    // The created model has to carry both manufacturer fields and both contact ids, or the
    // record comes out half-populated.
    [Fact]
    public void The_created_model_carries_the_manufacturer_on_every_field_that_needs_it()
    {
        var api = new FakeApiWithWrites(
            get: url => url.StartsWith("device_models") ? NotFound()
               : url.StartsWith("device_types") ? Found("type-1")
               : Found("maker-1"),
            post: _ => (201, "{\"data\":{\"id\":\"new-model\"}}"));

        Resolve(api, new Dictionary<string, string>());

        var body = api.Posts.Single().Content;
        body.Should().Contain("\"manufacturer_according_to_type_plate\":\"seca\"")
            .And.Contain("\"current_responsible_manufacturer\":\"seca\"")
            .And.Contain("\"manufacturer_company_contact_id\":\"maker-1\"")
            .And.Contain("\"responsible_company_contact_id\":\"maker-1\"")
            .And.Contain("\"device_type_id\":\"type-1\"")
            .And.Contain("\"is_public\":false");
    }

    // The facility's own model is identified by all three together: the same title from a
    // different manufacturer is a different device.
    [Fact]
    public void The_lookup_is_narrowed_by_title_device_type_and_manufacturer()
    {
        var api = new FakeApiWithWrites(
            get: url => url.StartsWith("device_models") ? NotFound()
               : url.StartsWith("device_types") ? Found("type-1")
               : Found("maker-1"),
            post: _ => (201, "{\"data\":{\"id\":\"new-model\"}}"));

        Resolve(api, new Dictionary<string, string>());

        var modelLookup = Uri.UnescapeDataString(api.Gets.Last(g => g.StartsWith("device_models")));
        modelLookup.Should().Contain("\"title\"")
                   .And.Contain("\"device_type_id\"")
                   .And.Contain("\"manufacturer_according_to_type_plate\"")
                   .And.Contain("filter[scope]=public_and_tenant");
    }
}
