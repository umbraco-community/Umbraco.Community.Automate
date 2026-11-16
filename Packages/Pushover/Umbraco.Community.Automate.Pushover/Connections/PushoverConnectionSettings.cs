using System.ComponentModel.DataAnnotations;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Pushover.Configuration;

namespace Umbraco.Community.Automate.Pushover.Connections;

/// <summary>
/// One Pushover application token and one user or group key. To notify different users, groups
/// or Pushover applications, create a connection for each and pick the right one per step.
/// </summary>
public sealed class PushoverConnectionSettings
{
    // Credentials default to references to configuration, so values stored in appsettings just work.
    [Field(
        Label = "API token",
        Description = "The API token of your Pushover application, from https://pushover.net/apps.",
        IsSensitive = true,
        SortOrder = 0)]
    public string ApiToken { get; set; } = PushoverConfiguration.ApiTokenReference;

    [Field(
        Label = "User or group key",
        Description = "Who receives the notifications: your user key (on your Pushover dashboard) or a delivery group's key.",
        IsSensitive = true,
        SortOrder = 1)]
    public string UserKey { get; set; } = PushoverConfiguration.UserKeyReference;

    // Only used for Max (emergency) priority, so they're out of the way under Advanced.
    [Range(30, 10800, ErrorMessage = "Retry must be between 30 and 10,800 seconds.")]
    [Field(
        Label = "Retry (seconds)",
        Description = "For Max priority only: how often Pushover repeats the notification until someone acknowledges it. At least 30 seconds. Defaults to 60.",
        EditorUiAlias = "Umb.PropertyEditorUi.Integer",
        EditorConfig = """[{ "alias": "min", "value": 30 }, { "alias": "max", "value": 10800 }]""",
        Group = "Advanced",
        SortOrder = 2)]
    public int Retry { get; set; } = 60;

    [Range(30, 10800, ErrorMessage = "Expire must be between 30 and 10,800 seconds (3 hours).")]
    [Field(
        Label = "Expire (seconds)",
        Description = "For Max priority only: how long Pushover keeps repeating it before giving up. At most 10,800 seconds (3 hours). Defaults to 1,800 (30 minutes).",
        EditorUiAlias = "Umb.PropertyEditorUi.Integer",
        EditorConfig = """[{ "alias": "min", "value": 30 }, { "alias": "max", "value": 10800 }]""",
        Group = "Advanced",
        SortOrder = 3)]
    public int Expire { get; set; } = 1800;

    /// <summary>Returns the first problem found, or null when the settings are usable. Shared by the connection type and the action.</summary>
    public static string? Validate(PushoverConnectionSettings? settings)
    {
        if (string.IsNullOrWhiteSpace(settings?.ApiToken))
            return "An API token is required.";

        if (string.IsNullOrWhiteSpace(settings.UserKey))
            return "A user or group key is required.";

        // Automate resolves $-references before settings reach this code and reports a missing key
        // itself; this catches one that arrives unresolved anyway.
        if (settings.ApiToken.TrimStart().StartsWith('$') || settings.UserKey.TrimStart().StartsWith('$'))
            return $"A configuration reference could not be resolved. Add ApiToken and UserKey under {PushoverConfiguration.SecretsPath}, or enter the values on the connection.";

        return null;
    }
}
