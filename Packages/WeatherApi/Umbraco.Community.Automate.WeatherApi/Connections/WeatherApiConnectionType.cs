using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.WeatherApi.Api;

namespace Umbraco.Community.Automate.WeatherApi.Connections;

[ConnectionType("community.weatherApi", "WeatherAPI.com",
    Description = "Connect to WeatherAPI.com using an API key",
    Group = "Weather",
    Icon = "icon-partly-cloudy")]
public sealed class WeatherApiConnectionType(ConnectionTypeInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
    : ConnectionTypeBase<WeatherApiConnectionSettings>(infrastructure)
{
    public override async Task<ConnectionValidationResult> ValidateAsync(
        object? settings,
        CancellationToken cancellationToken)
    {
        var typed = settings as WeatherApiConnectionSettings;

        if (WeatherApiConnectionSettingsValidator.Validate(typed) is { } error)
            return ConnectionValidationResult.Failure(error);

        using var client = httpClientFactory.CreateClient();

        return await WeatherApiRequestHelper.ValidateApiKeyAsync(client, typed!.ApiKey, cancellationToken);
    }
}
