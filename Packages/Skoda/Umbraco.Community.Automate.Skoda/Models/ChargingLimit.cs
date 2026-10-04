using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Models;

public sealed record ChargingLimit(
    [property: JsonPropertyName("targetStateOfChargeInPercent")] int TargetStateOfChargeInPercent)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
