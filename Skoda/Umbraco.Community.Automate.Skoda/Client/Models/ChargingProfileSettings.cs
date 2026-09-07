using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record ChargingProfileSettings(
    [property: JsonPropertyName("maxChargingCurrent")] string MaxChargingCurrent,
    [property: JsonPropertyName("minBatteryStateOfCharge")] MinBatteryStateOfCharge MinBatteryStateOfCharge,
    [property: JsonPropertyName("targetStateOfChargeInPercent")] int TargetStateOfChargeInPercent,
    [property: JsonPropertyName("autoUnlockPlugWhenCharged")] string AutoUnlockPlugWhenCharged)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}