using System.Text.Json.Serialization;
using Umbraco.Community.Automate.Mastodon.Settings;

namespace Umbraco.Community.Automate.Mastodon.Actions;

/// <summary>
/// Request body for the Mastodon <c>POST /api/v1/statuses</c> endpoint.
/// </summary>
public sealed class MastodonStatusRequest
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("visibility")]
    public string Visibility { get; init; } = "public";

    [JsonPropertyName("sensitive")]
    public bool Sensitive { get; init; }

    [JsonPropertyName("spoiler_text")]
    public string? SpoilerText { get; init; }

    public static MastodonStatusRequest FromSettings(MastodonPostSettings settings)
    {
        var status = settings.Content.Trim();
        if (!string.IsNullOrWhiteSpace(settings.PostUrl))
            status = $"{status}\n\n{settings.PostUrl.Trim()}";

        return new MastodonStatusRequest
        {
            Status = status,
            Visibility = string.IsNullOrWhiteSpace(settings.Visibility) ? "public" : settings.Visibility,
            Sensitive = settings.IsSensitive,
            SpoilerText = settings.SpoilerText,
        };
    }
}
