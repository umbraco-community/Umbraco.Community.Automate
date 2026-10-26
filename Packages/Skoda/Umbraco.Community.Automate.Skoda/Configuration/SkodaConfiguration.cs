namespace Umbraco.Community.Automate.Skoda.Configuration;

/// <summary>
/// Where this package's values live in configuration. They sit under Umbraco Automate's shared
/// <c>Umbraco:Automate:Secrets</c> and <c>Umbraco:Automate:Variables</c> sections, which Automate
/// resolves <c>$</c> references from by default, so nothing needs registering. Nesting under
/// <c>Skoda</c> keeps this package's keys together and avoids clashes with other packages.
/// </summary>
public static class SkodaConfiguration
{
    public const string VariablesPath = "Umbraco:Automate:Variables:Skoda";
    public const string SecretsPath = "Umbraco:Automate:Secrets:Skoda";

    /// <summary>The reference new connections start with, so a key in configuration is used without any typing.</summary>
    public const string ApiKeyReference = "$" + SecretsPath + ":ApiKey";

    /// <summary>The reference the Start Auxiliary Heating action's S-PIN starts with.</summary>
    public const string SpinReference = "$" + SecretsPath + ":Spin";

    // Fixed values: they're the same for every site, so they're constants here, not configuration.
    /// <summary>The MyŠkoda Public API.</summary>
    public const string BaseUrl = "https://public.api.connect.skoda-auto.cz/";
}
