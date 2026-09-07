using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record VehicleStatusDetail(
    [property: JsonPropertyName("sunroof")] string Sunroof,
    [property: JsonPropertyName("trunk")] string Trunk,
    [property: JsonPropertyName("bonnet")] string Bonnet)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
