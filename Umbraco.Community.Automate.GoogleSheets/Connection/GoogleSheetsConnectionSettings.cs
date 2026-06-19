using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.GoogleSheets.Connection;

/// <summary>
/// Settings for the Google Sheets connection type.
/// </summary>
public class GoogleSheetsConnectionSettings
{
    /// <summary>
    /// Gets or sets the OAuth credential ID linking to the stored Google account tokens.
    /// </summary>
    [Field(Label = "Google Account",
        Description = "Authenticate with the Google account that owns the spreadsheet.",
        EditorUiAlias = "Umb.Automate.OAuth",
        EditorConfig = """[{ "alias": "provider", "value": "Google" }]""")]
    public Guid? OAuthCredentialsId { get; set; }
}
