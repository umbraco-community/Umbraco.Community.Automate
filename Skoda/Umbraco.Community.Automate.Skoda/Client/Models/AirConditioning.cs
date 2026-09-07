using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record AirConditioning(
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("targetTemperature")] TargetTemperature TargetTemperature,
    [property: JsonPropertyName("estimatedReachOfTargetTemperatureAt")] DateTimeOffset EstimatedReachOfTargetTemperatureAt,
    [property: JsonPropertyName("airConditioningWithoutExternalPower")] bool AirConditioningWithoutExternalPower,
    [property: JsonPropertyName("airConditioningAtUnlock")] bool AirConditioningAtUnlock,
    [property: JsonPropertyName("windowHeating")] WindowHeating WindowHeating,
    [property: JsonPropertyName("carCapturedTimestamp")] DateTimeOffset CarCapturedTimestamp)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
