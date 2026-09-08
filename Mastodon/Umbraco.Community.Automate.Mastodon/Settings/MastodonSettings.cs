using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Mastodon.Settings;

public sealed class MastodonSettings
{
    [Field(
        Label = "Information",
        Description = "",
        EditorUiAlias = "Umb.PropertyEditorUi.Label",
        EditorConfig = """
            [{ "alias": "labelTemplate", "value": "**Instance Url**<br>The base URL of your Mastodon instance (e.g. https://umbracocommunity.social). If set in appsettings, reference it like $Umbraco:Automate:Variables:MastodonInstance<br><br>**Access Token**<br>If set in appsettings, reference it like $Umbraco:Automate:Secrets:MastodonAccessToken" }]
            """,
        SortOrder = 0
        )]

    public string? Labels { get; set; }

    [Field(
        Label = "Instance URL",
        Description = "",
        SortOrder = 1
        )]

    public string InstanceUrl { get; set; } = string.Empty;

    [Field(
        Label = "Access Token",
        Description = "",
        IsSensitive = true,
        SortOrder = 2)]
    public string AccessToken { get; set; } = string.Empty;
}
