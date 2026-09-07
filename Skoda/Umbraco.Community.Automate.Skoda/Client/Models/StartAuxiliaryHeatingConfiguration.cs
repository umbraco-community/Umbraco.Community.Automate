using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record StartAuxiliaryHeatingConfiguration(
    [property: JsonPropertyName("targetTemperature")] TargetTemperature TargetTemperature,
    [property: JsonPropertyName("spin")] string Spin,
    [property: JsonPropertyName("durationInSeconds")] int DurationInSeconds,
    [property: JsonPropertyName("startMode")] string StartMode)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}