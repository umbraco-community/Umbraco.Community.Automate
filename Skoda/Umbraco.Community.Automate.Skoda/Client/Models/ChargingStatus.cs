using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record ChargingStatus(
    [property: JsonPropertyName("chargingRateInKilometersPerHour")] double ChargingRateInKilometersPerHour,
    [property: JsonPropertyName("chargePowerInKw")] double ChargePowerInKw,
    [property: JsonPropertyName("remainingTimeToFullyChargedInMinutes")] int RemainingTimeToFullyChargedInMinutes,
    [property: JsonPropertyName("fullyChargedAt")] DateTimeOffset FullyChargedAt,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("chargeType")] string ChargeType,
    [property: JsonPropertyName("battery")] BatteryStatus Battery)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}