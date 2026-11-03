namespace Umbraco.Community.Automate.Mastodon.Configuration;

/// <summary>
/// Where this package's values live in configuration. They sit under Umbraco Automate's shared
/// <c>Umbraco:Automate:Secrets</c> and <c>Umbraco:Automate:Variables</c> sections, which Automate
/// resolves <c>$</c> references from by default, so nothing needs registering. Nesting under
/// <c>Mastodon</c> keeps this package's keys together and avoids clashes with other packages.
/// </summary>
public static class MastodonConfiguration
{
    public const string VariablesPath = "Umbraco:Automate:Variables:Mastodon";
    public const string SecretsPath = "Umbraco:Automate:Secrets:Mastodon";

    /// <summary>The references new connections start with, so values in configuration are used without any typing.</summary>
    public const string InstanceUrlReference = "$" + VariablesPath + ":InstanceUrl";
    public const string AccessTokenReference = "$" + SecretsPath + ":AccessToken";
}
