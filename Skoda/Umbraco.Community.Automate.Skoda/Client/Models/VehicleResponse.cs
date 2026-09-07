using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record VehicleResponse(
    [property: JsonPropertyName("vehicle")] Vehicle Vehicle,
    [property: JsonPropertyName("errors")] IReadOnlyCollection<VehicleError> Errors)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}