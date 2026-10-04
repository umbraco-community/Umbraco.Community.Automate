using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Models;

public sealed record BatteryStatus(
    [property: JsonPropertyName("remainingCruisingRangeInMeters")] int RemainingCruisingRangeInMeters,
    [property: JsonPropertyName("stateOfChargeInPercent")] int StateOfChargeInPercent)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}