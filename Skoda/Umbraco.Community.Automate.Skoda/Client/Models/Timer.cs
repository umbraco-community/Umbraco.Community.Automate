using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record Timer(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("time")] string Time,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("oneOffDay")] string? OneOffDay,
    [property: JsonPropertyName("recurringOn")] IReadOnlyCollection<string> RecurringOn)
{
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}