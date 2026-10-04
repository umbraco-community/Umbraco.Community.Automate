namespace Umbraco.Community.Automate.Examples.Simple.Configuration;

/// <summary>
/// The one place this package's configuration path and fixed values are defined. The API key
/// lives under Umbraco Automate's shared <c>Umbraco:Automate:Secrets</c> section, which Automate
/// resolves <c>$</c> references from by default, so nothing needs registering.
/// </summary>
public static class SimpleConfiguration
{
    public const string SecretsPath = "Umbraco:Automate:Secrets:Simple";

    /// <summary>
    /// The value a new connection's API key field starts with, so a key stored in configuration
    /// (appsettings, environment variables, user secrets) is used without typing anything.
    /// </summary>
    public const string ApiKeyReference = "$" + SecretsPath + ":ApiKey";

    // Fixed values: they're the same for every site, so they're constants here, not configuration.

    /// <summary>The service's API. httpbin.org echoes requests and accepts any bearer token.</summary>
    public const string BaseUrl = "https://httpbin.org/";
}
