using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record ParkingPosition(
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("gpsCoordinates")] GpsCoordinates? GpsCoordinates,
    [property: JsonPropertyName("formattedAddress")] string? FormattedAddress)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}