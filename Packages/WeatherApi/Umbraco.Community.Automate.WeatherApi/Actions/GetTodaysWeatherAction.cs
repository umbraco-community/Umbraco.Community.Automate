using System.Globalization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.WeatherApi.Api;
using Umbraco.Community.Automate.WeatherApi.Connections;

namespace Umbraco.Community.Automate.WeatherApi.Actions;

[Action("community.weatherApi.getTodaysWeather", "Get Today's Weather",
    Description = "Gets today's weather forecast for a location",
    Group = "Weather",
    Icon = "icon-partly-cloudy",
    ConnectionTypeAlias = "community.weatherApi")]
public class GetTodaysWeatherAction(ActionInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
    : ActionBase<GetTodaysWeatherSettings, GetTodaysWeatherOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
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

        string? languageCode = null;
        if (!string.IsNullOrWhiteSpace(settings.Culture))
        {
            try
            {
                languageCode = WeatherApiRequestHelper.ResolveLanguageCode(CultureInfo.GetCultureInfo(settings.Culture));
            }
            catch (CultureNotFoundException ex)
            {
                return ActionResult.Failed(
                    new ArgumentException($"\"{settings.Culture}\" is not a recognized culture.", ex),
                    StepRunErrorCategory.Validation);
            }
        }

        try
        {
            var client = httpClientFactory.CreateClient();

            var result = await WeatherApiRequestHelper.GetTodaysWeatherAsync(
                client, connectionSettings!.ApiKey, settings.Location, languageCode, cancellationToken);

            if (!result.IsSuccess)
            {
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
                iconUrl = $"https:{iconUrl}";

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
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }
}
