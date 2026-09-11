using FluentAssertions;
using SamedisCare.Helper.Logging;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// Re-running the upload after fixing something must update what is there, not try to create
/// it again -- and must not attach the same protocol a second time.
/// </summary>
public class TaskDocumentTests
{
    private static readonly ISyncLog Silent = new NullSyncLog();
    private const string TaskId = "507f1f77bcf86cd799439011";

    private static FakeApi WithUploads(params string[] names)
        => FakeApi.Returning("uploads",
             "{\"data\":[" + string.Join(",", names.Select((n, i) =>
                 $"{{\"id\":\"u{i}\",\"attributes\":{{\"name\":\"{n}\"}}}}")) + "]}");

    [Fact]
    public void A_protocol_already_attached_is_not_uploaded_again()
    {
        var api = WithUploads("protokoll_001.pdf");

        Tasks.IsDocumentAlreadyAttached(api, "issues", TaskId, "protokoll_001.pdf", Silent)
             .Should().BeTrue();
    }

    [Fact]
    public void A_protocol_that_is_not_attached_yet_is_uploaded()
    {
        var api = WithUploads("protokoll_002.pdf");

        Tasks.IsDocumentAlreadyAttached(api, "issues", TaskId, "protokoll_001.pdf", Silent)
             .Should().BeFalse();
    }

    // File names are compared the way a file system would, not byte for byte.
    [Fact]
    public void The_comparison_ignores_casing()
        => Tasks.IsDocumentAlreadyAttached(WithUploads("Protokoll_001.PDF"), "issues", TaskId,
                                           "protokoll_001.pdf", Silent)
                .Should().BeTrue();

    [Fact]
    public void A_task_with_no_uploads_yet_takes_the_file()
        => Tasks.IsDocumentAlreadyAttached(WithUploads(), "issues", TaskId, "protokoll_001.pdf", Silent)
                .Should().BeFalse();

    // Attaching a duplicate is a smaller problem than silently not attaching the protocol,
    // so a failure to read the existing uploads lets the upload proceed.
    [Fact]
    public void An_unreadable_upload_list_does_not_block_the_upload()
        => Tasks.IsDocumentAlreadyAttached(FakeApi.AlwaysStatus(500), "issues", TaskId,
                                           "protokoll_001.pdf", Silent)
                .Should().BeFalse();

    [Theory]
    [InlineData("", "protokoll_001.pdf")]
    [InlineData("507f1f77bcf86cd799439011", "")]
    public void Nothing_is_asked_without_a_task_or_a_file(string taskId, string fileName)
    {
        var api = FakeApi.NotFound();

        Tasks.IsDocumentAlreadyAttached(api, "issues", taskId, fileName, Silent).Should().BeFalse();
        api.Requests.Should().BeEmpty();
    }

    [Fact]
    public void The_lookup_asks_the_tasks_own_uploads_collection()
    {
        var api = WithUploads("x.pdf");

        Tasks.IsDocumentAlreadyAttached(api, "issues", TaskId, "protokoll_001.pdf", Silent);

        api.Requests.Single().Should().StartWith($"issues/{TaskId}/uploads");
    }
}

/// <summary>
/// The task is created with external_id set to the source's issue_number, and the unique index
/// is on (tenant_id, external_id). issue_number is the server's own running number, so a
/// lookup by it never finds what this sync wrote.
/// </summary>
public class TaskLookupTests
{
    [Fact]
    public void A_task_is_found_by_the_external_id_this_sync_writes()
    {
        var api = FakeApi.Answering(("via/external_id/11", "issue-11"));
        var lookup = new SamedisCare.Api.Lookup.ResourceLookup(api, "issues");

        lookup.ByVia("external_id", "11").Should().Be("issue-11");
    }

    // Kept as a fallback for sources that reference a task by the number samedis assigned.
    [Fact]
    public void The_issue_number_still_works_as_a_fallback()
    {
        var api = FakeApi.Answering(("gridfilter", "issue-by-number"));
        var lookup = new SamedisCare.Api.Lookup.ResourceLookup(api, "issues");

        Tasks.ResolveIssueIdByIssueNumber(lookup, "11").Should().Be("issue-by-number");
    }

    [Fact]
    public void A_numeric_issue_number_is_filtered_as_a_number()
    {
        var api = FakeApi.NotFound();
        var lookup = new SamedisCare.Api.Lookup.ResourceLookup(api, "issues");

        Tasks.ResolveIssueIdByIssueNumber(lookup, "11");

        Uri.UnescapeDataString(api.Requests.Single()).Should().Contain("\"filterType\":\"number\"");
    }
}
