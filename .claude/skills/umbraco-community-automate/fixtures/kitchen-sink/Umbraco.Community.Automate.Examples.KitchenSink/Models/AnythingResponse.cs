using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Models;

/// <summary>
/// httpbin.org's response to <c>/anything</c>: it echoes the request back, which stands in for
/// a real service's "created" response.
/// </summary>
public sealed class AnythingResponse
{
    [JsonPropertyName("json")]
    public SendMessageRequest? Json { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
