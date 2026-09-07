using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record Charging(
    [property: JsonPropertyName("isVehicleInSavedLocation")] bool IsVehicleInSavedLocation,
    [property: JsonPropertyName("status")] ChargingStatus Status,
    [property: JsonPropertyName("settings")] ChargingSettings Settings,
    [property: JsonPropertyName("carCapturedTimestamp")] DateTimeOffset CarCapturedTimestamp)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}