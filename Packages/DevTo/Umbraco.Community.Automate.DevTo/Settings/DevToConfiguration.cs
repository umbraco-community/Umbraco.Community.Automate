namespace Umbraco.Community.Automate.DevTo.Settings;

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
}
