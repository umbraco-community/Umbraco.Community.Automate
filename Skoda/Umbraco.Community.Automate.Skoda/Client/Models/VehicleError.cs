using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record VehicleError(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("description")] string Description)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}