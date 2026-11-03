using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.WeatherApi.Configuration;

namespace Umbraco.Community.Automate.WeatherApi.Connections;

public sealed class WeatherApiConnectionSettings
{
    [Field(
        Label = "API Key",
        Description = "The WeatherAPI.com API key.",
        IsSensitive = true,
        SortOrder = 1)]
    public string ApiKey { get; set; } = WeatherApiConfiguration.ApiKeyReference;
}
