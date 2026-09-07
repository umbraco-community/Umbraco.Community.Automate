using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record MinBatteryStateOfCharge(
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("minimumBatteryStateOfChargeInPercent")] int MinimumBatteryStateOfChargeInPercent)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}