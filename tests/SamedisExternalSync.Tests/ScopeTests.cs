using FluentAssertions;
using SamedisCare.Api.Http;
using SamedisCare.Api.Lookup;
using SamedisCare.Helper.Logging;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// Master data the facility references but does not own -- device types, manufacturers,
/// device models -- lives partly in a shared catalog. A lookup that forgets the scope sees
/// only the facility's own records and creates a local copy of something that is already
/// there.
/// <para>
/// Which endpoint needs the parameter is not uniform, which is exactly why it is asserted
/// here rather than left to the server: the contacts controller passes
/// <c>scope: :public_and_tenant</c> into its filters and therefore defaults to it, while the
/// device-type and device-model controllers pass no default and fall back to the facility's
/// own records.
/// </para>
/// </summary>
public class PublicScopeTests
{
    private static readonly ISyncLog Silent = new NullSyncLog();

    [Fact]
    public void A_device_type_lookup_searches_the_public_catalog()
    {
        var api = FakeApi.NotFound();

        DeviceTypes.ResolveDeviceTypeId(api, "device_types", "Beatmungsgeraet",
                                        createOnTheFly: false,
                                        new ResourceLookup(api, "device_types"), Silent);

        api.Requests.Should().ContainSingle()
           .Which.Should().Contain("filter[scope]=public_and_tenant");
    }

    [Fact]
    public void A_manufacturer_lookup_searches_the_shared_contacts()
    {
        var api = FakeApi.NotFound();

        Contacts.ResolveCompanyContactId(api, "contacts", "Nordlicht Medizintechnik GmbH",
                                         createOnTheFly: false,
                                         new ResourceLookup(api, "contacts"), Silent);

        api.Requests.Should().ContainSingle()
           .Which.Should().Contain("filter[scope]=public_and_tenant");
    }

    // A company contact carries its name in last_name, so without the type a person of the
    // same surname would match.
    [Fact]
    public void A_manufacturer_lookup_is_narrowed_to_companies()
    {
        var api = FakeApi.NotFound();

        Contacts.ResolveCompanyContactId(api, "contacts", "Nordlicht Medizintechnik GmbH",
                                         createOnTheFly: false,
                                         new ResourceLookup(api, "contacts"), Silent);

        var url = Uri.UnescapeDataString(api.Requests.Single());
        url.Should().Contain("\"last_name\"").And.Contain("\"contact_type\"");
    }

    [Fact]
    public void A_device_model_lookup_searches_the_public_catalog()
    {
        var api = FakeApi.NotFound();

        DeviceModels.ResolveCatalogId(new ResourceLookup(api, "device_models"), "Seca 954", "seca");

        api.Requests.Should().OnlyContain(r => r.Contains("filter[scope]=public_and_tenant"));
    }

    // The facility's own root device type is the one place that must NOT search the shared
    // catalog: a new type can only be created below the facility's own node.
    [Fact]
    public void The_facilitys_root_device_type_is_looked_up_in_its_own_scope_only()
    {
        var api = new FakeApiWithWrites(
            get: url => url.Contains("filter[scope]=tenant")
                ? (200, "{\"data\":[{\"id\":\"root-1\"}],\"meta\":{\"total\":1}}")
                : (404, "{\"meta\":{\"msg\":{\"success\":false,\"error\":\"record_not_found_error\"}}}"),
            post: _ => (201, "{\"data\":{\"id\":\"new-type\"}}"));

        DeviceTypes.ResolveDeviceTypeId(api, "device_types", "Beatmungsgeraet",
                                        createOnTheFly: true,
                                        new ResourceLookup(api, "device_types"), Silent,
                                        tenantId: "507f1f77bcf86cd799439001")
                   .Should().Be("new-type");

        // The public catalog is searched first, the facility's own root separately.
        api.Gets.Should().Contain(r => r.Contains("filter[scope]=public_and_tenant"));
        api.Gets.Should().Contain(r => r.Contains("filter[scope]=tenant")
                                    && !r.Contains("public_and_tenant"));

        // A new type hangs below the facility's own root, never below a public one.
        api.Posts.Single().Content.Should().Contain("\"parent_id\":\"root-1\"");
    }

    // The counterpart, and the case that cost a whole import run: the facility's root node
    // did not answer, the create was abandoned, and with it every device model, every
    // inventory, every task and every training -- all reported as "Skipped, Errors: 0".
    //
    // The server never needed the parent. Its create action calls
    // Tenant#ensure_type_catalog_tenant_node itself, so the node exists by the time the
    // record is built, and a parent_id it cannot resolve is rescued onto that same node.
    [Fact]
    public void A_root_that_does_not_answer_does_not_stop_the_type_from_being_created()
    {
        var api = new FakeApiWithWrites(
            get: _ => (404, "{\"meta\":{\"msg\":{\"success\":false,\"error\":\"record_not_found_error\"}}}"),
            post: _ => (201, "{\"data\":{\"id\":\"new-type\"}}"));

        DeviceTypes.ResolveDeviceTypeId(api, "device_types", "Beatmungsgeraet",
                                        createOnTheFly: true,
                                        new ResourceLookup(api, "device_types"), Silent,
                                        tenantId: "507f1f77bcf86cd799439001")
                   .Should().Be("new-type");

        var posted = api.Posts.Single().Content;
        posted.Should().Contain("Beatmungsgeraet");
        posted.Should().NotContain("parent_id",
            "an unresolved parent must be left out, not sent as null or empty");
    }

    // A lookup that finds an existing record must not post anything.
    [Fact]
    public void An_existing_manufacturer_is_not_created_again()
    {
        var api = FakeApi.Answering(("filter[scope]=public_and_tenant", "contact-1"));

        Contacts.ResolveCompanyContactId(api, "contacts", "Nordlicht Medizintechnik GmbH",
                                         createOnTheFly: true,
                                         new ResourceLookup(api, "contacts"), Silent)
                .Should().Be("contact-1");
    }

    [Fact]
    public void An_existing_device_type_is_not_created_again()
    {
        var api = FakeApi.Answering(("filter[scope]=public_and_tenant", "type-1"));

        DeviceTypes.ResolveDeviceTypeId(api, "device_types", "Beatmungsgeraet",
                                        createOnTheFly: true,
                                        new ResourceLookup(api, "device_types"), Silent)
                   .Should().Be("type-1");
    }
}
