using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Models;

/// <summary>The body the Send Message action posts to httpbin.org.</summary>
public sealed class SendMessageRequest
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
