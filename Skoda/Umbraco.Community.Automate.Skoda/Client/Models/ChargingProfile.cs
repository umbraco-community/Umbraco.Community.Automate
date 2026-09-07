using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record ChargingProfile(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("settings")] ChargingProfileSettings Settings,
    [property: JsonPropertyName("preferredChargingTimes")] IReadOnlyCollection<ChargingTime> PreferredChargingTimes,
    [property: JsonPropertyName("timers")] IReadOnlyCollection<Timer> Timers)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}