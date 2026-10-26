namespace Umbraco.Community.Automate.GoogleSheets.Configuration;

/// <summary>
/// The one place this package's configuration path and fixed Google values are defined.
/// </summary>
public static class GoogleSheetsConfiguration
{
    /// <summary>
    /// Where the OAuth client ID and secret live. OAuth providers sit under
    /// <c>Umbraco:Automate:Providers</c>, where Umbraco Automate's OAuth support expects them.
    /// </summary>
    public const string ProviderPath = "Umbraco:Automate:Providers:GoogleSheets";

    // Fixed values: they're the same for every site, so they're constants here, not configuration.

    /// <summary>The Google Sheets REST API's spreadsheets endpoint.</summary>
    public const string ApiBaseUrl = "https://sheets.googleapis.com/v4/spreadsheets";

    /// <summary>The OAuth scope connections ask for: read and write access to spreadsheets.</summary>
    public const string Scope = "https://www.googleapis.com/auth/spreadsheets";
}
