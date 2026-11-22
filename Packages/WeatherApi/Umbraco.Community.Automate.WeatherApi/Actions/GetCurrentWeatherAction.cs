using System.Globalization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.WeatherApi.Api;
using Umbraco.Community.Automate.WeatherApi.Connections;

namespace Umbraco.Community.Automate.WeatherApi.Actions;

[Action("community.weatherApi.getCurrentWeather", "Get Current Weather",
    Description = "Gets the current weather conditions for a location",
    Group = "Weather",
    Icon = "icon-partly-cloudy",
    ConnectionTypeAlias = "community.weatherApi")]
public class GetCurrentWeatherAction(ActionInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
    : ActionBase<GetCurrentWeatherSettings, GetCurrentWeatherOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
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

            var result = await WeatherApiRequestHelper.GetCurrentWeatherAsync(
                client, connectionSettings!.ApiKey, settings.Location, languageCode, cancellationToken);

            if (!result.IsSuccess)
            {
                return ActionResult.Failed(
                    new InvalidOperationException(result.ErrorMessage ?? $"WeatherAPI.com returned status {result.StatusCode}"),
                    WeatherApiRequestHelper.Classify(result.StatusCode));
            }

            var location = result.Response?.Location;
            var current = result.Response?.Current;
            var condition = current?.Condition;
            var iconUrl = condition?.Icon;
            if (!string.IsNullOrWhiteSpace(iconUrl) && iconUrl.StartsWith("//"))
                iconUrl = $"https:{iconUrl}";

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
