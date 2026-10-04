using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.WeatherApi.Api;

namespace Umbraco.Community.Automate.WeatherApi.Connections;

/// <summary>
/// Defines the WeatherAPI.com connection type for Umbraco Automate.
/// Stores the API key per connection, and validates it against the
/// WeatherAPI.com current weather endpoint before saving.
/// </summary>
[ConnectionType("community.weatherApi", "WeatherAPI.com",
    Description = "Connect to WeatherAPI.com using an API key",
    Group = "Weather",
    Icon = "icon-partly-cloudy")]
public sealed class WeatherApiConnectionType : ConnectionTypeBase<WeatherApiConnectionSettings>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WeatherApiConnectionType(
        ConnectionTypeInfrastructure infrastructure,
        IHttpClientFactory httpClientFactory)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Validates the connection by requesting the current weather for a known location
    /// using the configured API key.
    /// </summary>
    public override async Task<ConnectionValidationResult> ValidateAsync(
        object? settings,
        CancellationToken cancellationToken)
    {
        var typed = settings as WeatherApiConnectionSettings;

        if (WeatherApiConnectionSettingsValidator.Validate(typed) is { } error)
            return ConnectionValidationResult.Failure(error);

        using var client = _httpClientFactory.CreateClient();

        return await WeatherApiRequestHelper.ValidateApiKeyAsync(client, typed!.ApiKey, cancellationToken);
    }
}
