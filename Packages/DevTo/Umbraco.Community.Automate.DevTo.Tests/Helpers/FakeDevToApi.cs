using System.Net;
using System.Text;
using System.Text.Json;
using Moq;

namespace Umbraco.Community.Automate.DevTo.Tests.Helpers;

/// <summary>
/// An in-memory stand-in for the Forem API: queue a response per call, then inspect the
/// requests that were sent (bodies are captured before the request is disposed).
/// </summary>
internal sealed class FakeDevToApi : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body)> _responses = new();

    public List<RecordedRequest> Requests { get; } = [];

    public FakeDevToApi Respond(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses.Enqueue((status, json));
        return this;
    }

    public FakeDevToApi RespondWithArticles(params object[] articles)
        => Respond(JsonSerializer.Serialize(articles));

    public IHttpClientFactory CreateFactory()
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(this, disposeHandler: false));
        return factory.Object;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(
            request.Method, request.RequestUri!, request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)), body,
            request.Content?.Headers.ContentLength, request.Content?.Headers.ContentType?.MediaType));

        if (!_responses.TryDequeue(out var response))
            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");

        return new HttpResponseMessage(response.Status)
        {
            Content = new StringContent(response.Body, Encoding.UTF8, "application/json"),
        };
    }
}

internal sealed record RecordedRequest(
    HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, string? Body, long? ContentLength, string? ContentType)
{
    /// <summary>The <c>article</c> object of a create/update body.</summary>
    public JsonElement Article => JsonDocument.Parse(Body!).RootElement.GetProperty("article");
}
