namespace Umbraco.Community.Automate.Pushover.Configuration;

/// <summary>
/// Where this package's values live in configuration. They sit under Umbraco Automate's shared
/// <c>Umbraco:Automate:Secrets</c> section, which Automate resolves <c>$</c> references from by
/// default, so nothing needs registering. Nesting under <c>Pushover</c> keeps this package's keys
/// together and avoids clashes with other packages.
/// </summary>
public static class PushoverConfiguration
{
    public const string SecretsPath = "Umbraco:Automate:Secrets:Pushover";

    /// <summary>The reference a new connection's API token starts with, so a token in configuration is used without any typing.</summary>
    public const string ApiTokenReference = "$" + SecretsPath + ":ApiToken";

    /// <summary>The reference a new connection's user or group key starts with.</summary>
    public const string UserKeyReference = "$" + SecretsPath + ":UserKey";

    // Fixed values: they're the same for every site, so they're constants here, not configuration.

    /// <summary>The Pushover API.</summary>
    public const string BaseUrl = "https://api.pushover.net/1/";
}
