namespace Umbraco.Community.Automate.WeatherApi.Configuration;

/// <summary>
/// Where this package's values live in configuration. They sit under Umbraco Automate's shared
/// <c>Umbraco:Automate:Secrets</c> and <c>Umbraco:Automate:Variables</c> sections, which Automate
/// resolves <c>$</c> references from by default, so nothing needs registering. Nesting under
/// <c>WeatherApi</c> keeps this package's keys together and avoids clashes with other packages.
/// </summary>
public static class WeatherApiConfiguration
{
    public const string VariablesPath = "Umbraco:Automate:Variables:WeatherApi";
    public const string SecretsPath = "Umbraco:Automate:Secrets:WeatherApi";

    /// <summary>The reference new connections start with, so a key in configuration is used without any typing.</summary>
    public const string ApiKeyReference = "$" + SecretsPath + ":ApiKey";

    // Fixed values: they're the same for every site, so they're constants here, not configuration.
    /// <summary>The WeatherAPI.com REST API.</summary>
    public const string BaseUrl = "https://api.weatherapi.com/v1";
}
