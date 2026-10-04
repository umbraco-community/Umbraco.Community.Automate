using Umbraco.Community.Automate.DevTo.Api;
using Umbraco.Community.Automate.DevTo.Configuration;


namespace Umbraco.Community.Automate.DevTo.Connections;

public static class DevToConnectionSettingsValidator
{
    /// <summary>Returns an error message, or <c>null</c> when the settings are usable.</summary>
    public static string? Validate(DevToConnectionSettings? settings)
    {
        if (settings is null)
            return "No DEV connection settings were provided.";

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return "An API key is required.";

        if (settings.ApiKey.TrimStart().StartsWith('$'))
            return $"The API key reference '{settings.ApiKey}' could not be resolved. Check the key exists under {DevToConfiguration.SecretsPath}.";

        if (!HttpUrl.IsValid(settings.InstanceUrl))
            return $"'{settings.InstanceUrl}' is not a valid instance URL. Use a full URL such as {DevToConfiguration.DefaultInstanceUrl}.";

        return null;
    }
}
