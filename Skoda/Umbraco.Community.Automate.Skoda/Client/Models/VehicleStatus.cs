using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record VehicleStatus(
    [property: JsonPropertyName("overall")] OverallVehicleStatus Overall,
    [property: JsonPropertyName("detail")] VehicleStatusDetail Detail,
    [property: JsonPropertyName("carCapturedTimestamp")] DateTimeOffset CarCapturedTimestamp)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}