using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record OverallVehicleStatus(
    [property: JsonPropertyName("doorsLocked")] string DoorsLocked,
    [property: JsonPropertyName("locked")] string Locked,
    [property: JsonPropertyName("doors")] string Doors,
    [property: JsonPropertyName("windows")] string Windows,
    [property: JsonPropertyName("lights")] string Lights,
    [property: JsonPropertyName("reliableLockStatus")] string ReliableLockStatus)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}