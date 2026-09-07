using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record FuelStatus(
    [property: JsonPropertyName("carType")] string CarType,
    [property: JsonPropertyName("adBlueRange")] double AdBlueRange,
    [property: JsonPropertyName("totalRangeInKm")] double TotalRangeInKm,
    [property: JsonPropertyName("primaryEngineRange")] EngineRange PrimaryEngineRange,
    [property: JsonPropertyName("secondaryEngineRange")] EngineRange? SecondaryEngineRange,
    [property: JsonPropertyName("carCapturedTimestamp")] DateTimeOffset CarCapturedTimestamp)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
