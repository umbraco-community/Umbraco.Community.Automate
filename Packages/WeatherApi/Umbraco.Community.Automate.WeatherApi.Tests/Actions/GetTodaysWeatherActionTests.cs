using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.WeatherApi.Actions;
using Umbraco.Community.Automate.WeatherApi.Connections;
using Umbraco.Community.Automate.WeatherApi.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.WeatherApi.Tests.Actions;

public class GetTodaysWeatherActionTests
{
    [Fact]
    public async Task Returns_todays_forecast()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, WeatherApiResponses.Forecast));

        var result = await Run(handler, new GetTodaysWeatherSettings { Location = "London" });

        Assert.Equal(ActionResultStatus.Success, result.Status);
        var output = Assert.IsType<GetTodaysWeatherOutput>(result.OutputData);
        Assert.Equal("2026-10-01", output.Date);
        Assert.Equal(17.2, output.MaxTemperatureC);
        Assert.Equal(9.1, output.MinTemperatureC);
        Assert.True(output.WillItRain);
        Assert.Equal(80, output.ChanceOfRain);
        Assert.False(output.WillItSnow);
        Assert.Equal("https://cdn.weatherapi.com/weather/64x64/day/176.png", output.ConditionIconUrl);

        var request = handler.Requests.Single().RequestUri!;
        Assert.EndsWith("/forecast.json", request.AbsolutePath);
        Assert.Contains("days=1", request.Query);
    }

    [Fact]
    public async Task Missing_location_is_a_validation_error()
    {
        var result = await Run(new StubHttpMessageHandler(), new GetTodaysWeatherSettings { Location = " " });

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
    }

    [Fact]
    public async Task Server_error_is_retryable()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.ServiceUnavailable, "{}"));

        var result = await Run(handler, new GetTodaysWeatherSettings { Location = "London" });

        Assert.Equal(StepRunErrorCategory.ServiceUnavailable, result.ErrorCategory);
    }

    private static Task<ActionResult> Run(StubHttpMessageHandler handler, GetTodaysWeatherSettings settings)
        => ActionTestHarness.For<GetTodaysWeatherAction>()
            .WithService<IHttpClientFactory>(new StubHttpClientFactory(handler))
            .WithSettings(settings)
            .WithConnection("community.weatherApi", new WeatherApiConnectionSettings { ApiKey = "abc123" })
            .ExecuteAsync();
}
