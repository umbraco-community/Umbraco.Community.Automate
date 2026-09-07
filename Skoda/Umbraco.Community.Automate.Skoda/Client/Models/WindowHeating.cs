using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record WindowHeating(
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("front")] string Front,
    [property: JsonPropertyName("rear")] string Rear)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}