namespace Umbraco.Community.Automate.Examples.KitchenSink.Configuration;

/// <summary>
/// The one place this package's configuration paths are defined. Values sit under Umbraco
/// Automate's shared <c>Umbraco:Automate:Secrets</c> (sensitive) and
/// <c>Umbraco:Automate:Variables</c> (non-sensitive) sections, which Automate resolves
/// <c>$</c> references from by default, so nothing needs registering. Nesting under the area
/// name keeps the package's keys together.
/// </summary>
public static class KitchenSinkConfiguration
{
    public const string SecretsPath = "Umbraco:Automate:Secrets:KitchenSink";

    /// <summary>
    /// The value a new connection's API key field starts with, so a key stored in configuration
    /// (appsettings, environment variables, user secrets) is used without typing anything.
    /// </summary>
    public const string ApiKeyReference = "$" + SecretsPath + ":ApiKey";

    // Fixed values: they're the same for every site, so they're constants here rather than
    // settings in appsettings. Keeping them in this class still gives one place to change them.

    /// <summary>The service's API. httpbin.org echoes requests and accepts any bearer token.</summary>
    public const string BaseUrl = "https://httpbin.org/";
}
