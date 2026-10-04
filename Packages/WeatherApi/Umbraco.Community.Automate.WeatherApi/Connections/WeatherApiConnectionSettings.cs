using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.WeatherApi.Configuration;

namespace Umbraco.Community.Automate.WeatherApi.Connections;

/// <summary>
/// Stores the settings for a WeatherAPI.com connection in Umbraco Automate.
/// </summary>
public sealed class WeatherApiConnectionSettings
{
    /// <summary>
    /// The WeatherAPI.com API key used by this connection. Defaults to
    /// <see cref="WeatherApiConfiguration.ApiKeyReference"/>, so a key stored in
    /// configuration works without typing it into the backoffice.
    /// </summary>
    [Field(
        Label = "API Key",
        Description = "The WeatherAPI.com API key.",
        IsSensitive = true,
        SortOrder = 1)]
    public string ApiKey { get; set; } = WeatherApiConfiguration.ApiKeyReference;
}
