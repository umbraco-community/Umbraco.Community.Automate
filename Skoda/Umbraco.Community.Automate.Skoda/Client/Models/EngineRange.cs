using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record EngineRange(
    [property: JsonPropertyName("engineType")] string EngineType,
    [property: JsonPropertyName("currentSoCInPercent")] double CurrentSoCInPercent,
    [property: JsonPropertyName("currentFuelLevelInPercent")] double CurrentFuelLevelInPercent,
    [property: JsonPropertyName("remainingRangeInKm")] double RemainingRangeInKm)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}