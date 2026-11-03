using Umbraco.Community.Automate.WeatherApi.Configuration;

namespace Umbraco.Community.Automate.WeatherApi.Connections;

/// <summary>
/// Shared validation for <see cref="WeatherApiConnectionSettings"/>, used by the connection
/// type (Test connection) and both actions, so they report the same message.
/// </summary>
public static class WeatherApiConnectionSettingsValidator
{
    /// <summary>Returns the first problem found, or <c>null</c> when the settings are valid.</summary>
    public static string? Validate(WeatherApiConnectionSettings? settings)
    {
        if (string.IsNullOrWhiteSpace(settings?.ApiKey))
            return "A WeatherAPI.com API key is required.";

        // Automate resolves $-references before settings reach this code, and reports a missing
        // key itself; this is a safety net for a reference that arrives unresolved anyway.
        if (settings.ApiKey.TrimStart().StartsWith('$'))
            return $"The API key reference '{settings.ApiKey}' could not be resolved. Add the key to configuration at {WeatherApiConfiguration.SecretsPath}:ApiKey, or enter the key itself on the connection.";

        return null;
    }
}
