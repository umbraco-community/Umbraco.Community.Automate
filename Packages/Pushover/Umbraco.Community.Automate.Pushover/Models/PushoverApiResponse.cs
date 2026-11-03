using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Pushover.Models;

/// <summary>Pushover's response to every call: a status, a request ID, and errors when it failed.</summary>
public sealed class PushoverApiResponse
{
    /// <summary>1 for success, 0 for a rejected request.</summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>Pushover's ID for the request, useful when contacting its support.</summary>
    [JsonPropertyName("request")]
    public string? Request { get; set; }

    /// <summary>What was wrong, when the request was rejected.</summary>
    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }

    /// <summary>The user's active devices, returned when validating a user key.</summary>
    [JsonPropertyName("devices")]
    public List<string>? Devices { get; set; }
}
