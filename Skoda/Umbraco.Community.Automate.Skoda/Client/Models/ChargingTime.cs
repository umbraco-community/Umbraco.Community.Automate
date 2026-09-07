using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record ChargingTime(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("startTime")] string StartTime,
    [property: JsonPropertyName("endTime")] string EndTime)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}