using System.Net;
using System.Text;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Fakes;

/// <summary>
/// Returns canned responses in order and records every request sent. Hand-written instead of a
/// mocking library: it's a few lines, and every contributor can read it.
/// </summary>
public sealed class StubHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new(responses);

    public List<HttpRequestMessage> Requests { get; } = [];

    public static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(_responses.Dequeue());
    }
}

/// <summary>Hands out clients that use the stub handler.</summary>
public sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
