namespace Umbraco.Community.Automate.GoogleSheets.Configuration;

/// <summary>
/// OAuth credentials for the Google Sheets provider, read from
/// <c>Umbraco:Automate:Providers:GoogleSheets</c> in application configuration.
/// </summary>
public sealed class GoogleSheetsOAuthOptions
{
    internal const string SectionPath = "Umbraco:Automate:Providers:GoogleSheets";

    /// <summary>Gets or sets the OAuth 2.0 Client ID from Google Cloud Console.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the OAuth 2.0 Client Secret from Google Cloud Console.</summary>
    public string ClientSecret { get; set; } = string.Empty;
}
