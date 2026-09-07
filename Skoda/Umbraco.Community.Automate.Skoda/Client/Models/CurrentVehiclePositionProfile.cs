using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record CurrentVehiclePositionProfile(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("targetStateOfChargeInPercent")] int TargetStateOfChargeInPercent,
    [property: JsonPropertyName("nextChargingTime")] string NextChargingTime)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}