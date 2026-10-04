using System.Globalization;
using Microsoft.Extensions.Logging;
using Umbraco.Community.Automate.WeatherApi.Connections;
using Umbraco.Community.Automate.WeatherApi.Api;
using Umbraco.Automate.Core.Actions;

namespace Umbraco.Community.Automate.WeatherApi.Actions;

/// <summary>
/// Umbraco Automate action that gets the current weather conditions for a location from
/// WeatherAPI.com, using the connection's API key.
/// </summary>
[Action("community.weatherApi.getCurrentWeather", "Get Current Weather",
    Description = "Gets the current weather conditions for a location",
    Group = "Weather",
    Icon = "icon-partly-cloudy",
    ConnectionTypeAlias = "community.weatherApi")]
public class GetCurrentWeatherAction : ActionBase<GetCurrentWeatherSettings, GetCurrentWeatherOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetCurrentWeatherAction> _logger;

    public GetCurrentWeatherAction(
        ActionInfrastructure infrastructure,
        IHttpClientFactory httpClientFactory,
        ILogger<GetCurrentWeatherAction> logger)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes the action by requesting the current weather for the configured location
    /// from the WeatherAPI.com current weather endpoint. Returns a failed result on validation
    /// errors, missing configuration, or a non-success API response.
    /// </summary>
    public override async Task<ActionResult> ExecuteAsync(
        ActionContext context,
        CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<GetCurrentWeatherSettings>();

        if (string.IsNullOrWhiteSpace(settings.Location))
        {
            return ActionResult.Failed(
                new ArgumentException("Location is required."),
                StepRunErrorCategory.Validation);
        }

        var connectionSettings = context.Connection?.GetSettings<WeatherApiConnectionSettings>();

        if (WeatherApiConnectionSettingsValidator.Validate(connectionSettings) is { } connectionError)
        {
            return ActionResult.Failed(
                new InvalidOperationException(connectionError),
                StepRunErrorCategory.ConfigurationError);
        }

        var apiKey = connectionSettings!.ApiKey;

        var culture = settings.Culture;

        string? languageCode = null;
        if (!string.IsNullOrWhiteSpace(culture))
        {
            try
            {
                languageCode = WeatherApiRequestHelper.ResolveLanguageCode(CultureInfo.GetCultureInfo(culture));
            }
            catch (CultureNotFoundException ex)
            {
                return ActionResult.Failed(
                    new ArgumentException($"\"{culture}\" is not a recognized culture.", ex),
                    StepRunErrorCategory.Validation);
            }
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            var result = await WeatherApiRequestHelper.GetCurrentWeatherAsync(
                httpClient, apiKey, settings.Location, languageCode, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogError("WeatherAPI.com request failed with status {StatusCode}: {ErrorContent}",
                    result.StatusCode, result.RawBody);

                return ActionResult.Failed(
                    new InvalidOperationException(result.ErrorMessage ?? $"WeatherAPI.com returned status {result.StatusCode}"),
                    WeatherApiRequestHelper.Classify(result.StatusCode));
            }

            var location = result.Response?.Location;
            var current = result.Response?.Current;
            var condition = current?.Condition;
            var iconUrl = condition?.Icon;
            if (!string.IsNullOrWhiteSpace(iconUrl) && iconUrl.StartsWith("//"))
            {
                iconUrl = $"https:{iconUrl}";
            }

            _logger.LogInformation("Retrieved current weather for {Location} from WeatherAPI.com", location?.Name);

            return Success(new GetCurrentWeatherOutput
            {
                LocationName = location?.Name,
                Region = location?.Region,
                Country = location?.Country,
                LocalTime = location?.Localtime,
                TemperatureC = current?.TempC ?? 0,
                TemperatureF = current?.TempF ?? 0,
                Condition = condition?.Text,
                ConditionIconUrl = iconUrl,
                ConditionCode = condition?.Code ?? 0,
                Humidity = current?.Humidity ?? 0,
                Cloud = current?.Cloud ?? 0,
                WindKph = current?.WindKph ?? 0,
                WindMph = current?.WindMph ?? 0,
                WindDirection = current?.WindDir,
                LastUpdated = current?.LastUpdated,
                WillItRain = current?.WillItRain == 1,
                ChanceOfRain = current?.ChanceOfRain ?? 0,
                WillItSnow = current?.WillItSnow == 1,
                ChanceOfSnow = current?.ChanceOfSnow ?? 0,
                Uv = current?.Uv ?? 0,
                RawResponse = result.RawBody,
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not reach WeatherAPI.com");
            return ActionResult.Failed(
                new InvalidOperationException($"Could not reach WeatherAPI.com: {ex.Message}", ex),
                StepRunErrorCategory.ServiceUnavailable);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return ActionResult.Failed(
                new InvalidOperationException("The request to WeatherAPI.com timed out.", ex),
                StepRunErrorCategory.Timeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current weather from WeatherAPI.com");
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }
}
