using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record ActiveVentilation(
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("durationInSeconds")] int DurationInSeconds,
    [property: JsonPropertyName("carCapturedTimestamp")] DateTimeOffset CarCapturedTimestamp)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}