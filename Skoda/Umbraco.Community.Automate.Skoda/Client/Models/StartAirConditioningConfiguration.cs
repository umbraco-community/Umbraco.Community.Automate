using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record StartAirConditioningConfiguration(
    [property: JsonPropertyName("targetTemperature")] TargetTemperature TargetTemperature,
    [property: JsonPropertyName("airConditioningWithoutExternalPower")] bool AirConditioningWithoutExternalPower)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}