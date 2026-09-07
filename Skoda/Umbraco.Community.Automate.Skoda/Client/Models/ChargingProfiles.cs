using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record ChargingProfiles(
    [property: JsonPropertyName("profiles")] IReadOnlyCollection<ChargingProfile> Profiles,
    [property: JsonPropertyName("currentVehiclePositionProfile")] CurrentVehiclePositionProfile? CurrentVehiclePositionProfile,
    [property: JsonPropertyName("carCapturedTimestamp")] DateTimeOffset CarCapturedTimestamp)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}