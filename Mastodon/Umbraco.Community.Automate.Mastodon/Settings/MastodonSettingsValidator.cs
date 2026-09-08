namespace Umbraco.Community.Automate.Mastodon.Settings;

/// <summary>
/// Shared validation for <see cref="MastodonSettings"/>, used by both the
/// connection type (Test connection) and the send-post action.
/// </summary>
public static class MastodonSettingsValidator
{
    /// <summary>
    /// Returns an error message describing the first problem found, or <c>null</c> when the settings are valid.
    /// </summary>
    public static string? Validate(MastodonSettings? settings)
    {
        if (string.IsNullOrWhiteSpace(settings?.InstanceUrl))
            return "Instance URL is required.";

        if (string.IsNullOrWhiteSpace(settings.AccessToken))
            return "Access token is required.";

        if (!Uri.TryCreate(settings.InstanceUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return "Instance URL must be an absolute URL starting with http:// or https:// (e.g. https://mastodon.social).";
        }

        return null;
    }
}
