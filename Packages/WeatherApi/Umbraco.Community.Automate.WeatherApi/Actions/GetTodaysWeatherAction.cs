using System.Globalization;
using Microsoft.Extensions.Logging;
using Umbraco.Community.Automate.WeatherApi.Connections;
using Umbraco.Community.Automate.WeatherApi.Api;
using Umbraco.Automate.Core.Actions;

namespace Umbraco.Community.Automate.WeatherApi.Actions;

/// <summary>
/// Umbraco Automate action that gets today's weather forecast for a location from
/// WeatherAPI.com, using the connection's API key.
/// </summary>
[Action("community.weatherApi.getTodaysWeather", "Get Today's Weather",
    Description = "Gets today's weather forecast for a location",
    Group = "Weather",
    Icon = "icon-partly-cloudy",
    ConnectionTypeAlias = "community.weatherApi")]
public class GetTodaysWeatherAction : ActionBase<GetTodaysWeatherSettings, GetTodaysWeatherOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetTodaysWeatherAction> _logger;

    public GetTodaysWeatherAction(
        ActionInfrastructure infrastructure,
        IHttpClientFactory httpClientFactory,
        ILogger<GetTodaysWeatherAction> logger)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes the action by requesting today's weather forecast for the configured location
    /// from the WeatherAPI.com forecast endpoint. Returns a failed result on validation
    /// errors, missing configuration, or a non-success API response.
    /// </summary>
    public override async Task<ActionResult> ExecuteAsync(
        ActionContext context,
        CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<GetTodaysWeatherSettings>();

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

            var result = await WeatherApiRequestHelper.GetTodaysWeatherAsync(
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
            var forecastDay = result.Response?.Forecast?.ForecastDay?.FirstOrDefault();
            var day = forecastDay?.Day;
            var condition = day?.Condition;
            var iconUrl = condition?.Icon;
            if (!string.IsNullOrWhiteSpace(iconUrl) && iconUrl.StartsWith("//"))
            {
                iconUrl = $"https:{iconUrl}";
            }

            _logger.LogInformation("Retrieved today's weather for {Location} from WeatherAPI.com", location?.Name);

            return Success(new GetTodaysWeatherOutput
            {
                LocationName = location?.Name,
                Region = location?.Region,
                Country = location?.Country,
                LocalTime = location?.Localtime,
                Date = forecastDay?.Date,
                MaxTemperatureC = day?.MaxTempC ?? 0,
                MaxTemperatureF = day?.MaxTempF ?? 0,
                MinTemperatureC = day?.MinTempC ?? 0,
                MinTemperatureF = day?.MinTempF ?? 0,
                AvgTemperatureC = day?.AvgTempC ?? 0,
                AvgTemperatureF = day?.AvgTempF ?? 0,
                MaxWindMph = day?.MaxWindMph ?? 0,
                MaxWindKph = day?.MaxWindKph ?? 0,
                TotalPrecipMm = day?.TotalPrecipMm ?? 0,
                TotalPrecipIn = day?.TotalPrecipIn ?? 0,
                TotalSnowCm = day?.TotalSnowCm ?? 0,
                AvgVisKm = day?.AvgVisKm ?? 0,
                AvgVisMiles = day?.AvgVisMiles ?? 0,
                AvgHumidity = day?.AvgHumidity ?? 0,
                WillItRain = day?.DailyWillItRain == 1,
                ChanceOfRain = day?.DailyChanceOfRain ?? 0,
                WillItSnow = day?.DailyWillItSnow == 1,
                ChanceOfSnow = day?.DailyChanceOfSnow ?? 0,
                Condition = condition?.Text,
                ConditionIconUrl = iconUrl,
                ConditionCode = condition?.Code ?? 0,
                Uv = day?.Uv ?? 0,
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
            _logger.LogError(ex, "Failed to get today's weather from WeatherAPI.com");
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }
}
