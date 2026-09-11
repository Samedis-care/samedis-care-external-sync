using SamedisCare.Api.Http;

namespace SamedisExternalSync.Tests;

/// <summary>
/// Records every URL asked for and answers from a caller-supplied rule set, so a test can
/// assert both the answer and -- just as important for a lookup cascade -- which requests
/// were never made.
/// </summary>
internal sealed class FakeApi : IApiClient
{
    public string LastError => string.Empty;
    public bool TestMode => false;

    private readonly Func<string, (int Status, string Body)> _respond;

    public List<string> Requests { get; } = new();

    private FakeApi(Func<string, (int, string)> respond) => _respond = respond;

    /// <summary>Answers 404 to everything.</summary>
    public static FakeApi NotFound() => Answering();

    /// <summary>
    /// Answers 200 with the given id for the first URL containing a marker, 404 otherwise.
    /// An empty marker is rejected: it would match every URL and quietly invert the test.
    /// </summary>
    public static FakeApi Answering(params (string Marker, string Id)[] hits)
        => hits.Any(h => string.IsNullOrEmpty(h.Marker))
            ? throw new ArgumentException("An empty marker matches every URL.", nameof(hits))
            : new FakeApi(url =>
            {
                foreach (var (marker, id) in hits)
                    if (url.Contains(marker, StringComparison.Ordinal))
                        return (200, $"{{\"data\":[{{\"id\":\"{id}\"}}],\"meta\":{{\"total\":1}}}}");
                return (404, "{\"meta\":{\"msg\":{\"error\":\"record_not_found_error\"}}}");
            });

    /// <summary>
    /// Answers 200 with a verbatim body for URLs containing the marker, 404 otherwise. Use
    /// where the test cares about the response shape rather than just an id.
    /// </summary>
    public static FakeApi Returning(string marker, string body)
        => new(url => url.Contains(marker, StringComparison.Ordinal) ? (200, body) : (404, "{\"meta\":{\"msg\":{\"success\":false,\"error\":\"record_not_found_error\"}}}"));

    /// <summary>Answers with a fixed status for everything.</summary>
    public static FakeApi AlwaysStatus(int status, string body =
        "{\"meta\":{\"msg\":{\"success\":false,\"error\":\"record_not_found_error\"}}}")
        => new(_ => (status, body));

    public int StatusCode { get; private set; }
    public string LastContent { get; private set; } = string.Empty;

    public string Get(string resource)
    {
        Requests.Add(resource);
        (StatusCode, LastContent) = _respond(resource);
        return LastContent;
    }

    public string Post(string r, string c) => throw new NotSupportedException();
    public string Put(string r, string i, string c) => throw new NotSupportedException();
    public string PostDocument(string r, string f, string n) => throw new NotSupportedException();
}
