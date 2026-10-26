using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Mastodon.Configuration;

namespace Umbraco.Community.Automate.Mastodon.Connections;

/// <summary>
/// Settings for a Mastodon connection. Both fields default to references to the package's
/// configuration section, so values stored in appsettings or environment variables are used
/// without any typing; replace a reference with the value itself to store it on the connection.
/// </summary>
public sealed class MastodonSettings
{
    [Field(
        Label = "Instance URL",
        Description = "The base URL of your Mastodon instance, e.g. https://mastodon.social.",
        SortOrder = 1)]
    public string InstanceUrl { get; set; } = MastodonConfiguration.InstanceUrlReference;

    [Field(
        Label = "Access Token",
        Description = "An access token with the write:statuses scope.",
        IsSensitive = true,
        SortOrder = 2)]
    public string AccessToken { get; set; } = MastodonConfiguration.AccessTokenReference;
}
