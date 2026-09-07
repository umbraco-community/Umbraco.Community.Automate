using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record AuxiliaryHeating(
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("startMode")] string StartMode,
    [property: JsonPropertyName("durationInSeconds")] int DurationInSeconds,
    [property: JsonPropertyName("targetTemperature")] TargetTemperature TargetTemperature,
    [property: JsonPropertyName("estimatedReachOfTargetTemperatureAt")] DateTimeOffset EstimatedReachOfTargetTemperatureAt,
    [property: JsonPropertyName("carCapturedTimestamp")] DateTimeOffset CarCapturedTimestamp)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}