using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record ChargingSettings(
    [property: JsonPropertyName("targetStateOfChargeInPercent")] int TargetStateOfChargeInPercent,
    [property: JsonPropertyName("batteryCareModeTargetValueInPercent")] int BatteryCareModeTargetValueInPercent,
    [property: JsonPropertyName("preferredChargeMode")] string PreferredChargeMode,
    [property: JsonPropertyName("availableChargeModes")] IReadOnlyCollection<string> AvailableChargeModes,
    [property: JsonPropertyName("chargingCareMode")] string ChargingCareMode,
    [property: JsonPropertyName("autoUnlockPlugWhenCharged")] string AutoUnlockPlugWhenCharged,
    [property: JsonPropertyName("maxChargeCurrentAc")] string MaxChargeCurrentAc,
    [property: JsonPropertyName("maxChargeCurrentAcAmpere")] int MaxChargeCurrentAcAmpere)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}