namespace Umbraco.Community.Automate.DevTo.Configuration;

/// <summary>
/// Where this package's values live in configuration. They sit under Umbraco Automate's shared
/// <c>Umbraco:Automate:Secrets</c> and <c>Umbraco:Automate:Variables</c> sections, which Automate
/// resolves <c>$</c> references from by default, so nothing needs registering. Nesting under
/// <c>DevTo</c> keeps this package's keys together and avoids clashes with other packages.
/// </summary>
public static class DevToConfiguration
{
    public const string VariablesPath = "Umbraco:Automate:Variables:DevTo";
    public const string SecretsPath = "Umbraco:Automate:Secrets:DevTo";

    /// <summary>The reference new connections start with, so a key in configuration is used without any typing.</summary>
    public const string ApiKeyReference = "$" + SecretsPath + ":ApiKey";

    // Fixed values: they're the same for every site, so they're constants here, not configuration.
    /// <summary>The Forem instance new connections start with. Other Forem sites can be entered instead.</summary>
    public const string DefaultInstanceUrl = "https://dev.to";
}
