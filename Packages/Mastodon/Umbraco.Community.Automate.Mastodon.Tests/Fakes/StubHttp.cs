using System.Net;
using System.Text;

namespace Umbraco.Community.Automate.Mastodon.Tests.Fakes;

/// <summary>
/// Returns canned responses in order and records every request sent. Hand-written instead of a
/// mocking library: it's a few lines, and every contributor can read it.
/// </summary>
public sealed class StubHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new(responses);

    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>Each request's body, read as it's sent: callers dispose requests straight after.</summary>
    public List<string?> Bodies { get; } = [];

    public static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
        return _responses.Dequeue();
    }
}

/// <summary>Hands out clients that use the stub handler.</summary>
public sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
