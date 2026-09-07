using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record TargetTemperature(
    [property: JsonPropertyName("value")] double Value,
    [property: JsonPropertyName("unit")] string Unit)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}