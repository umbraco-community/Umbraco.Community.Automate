using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.WeatherApi.Actions;
using Umbraco.Community.Automate.WeatherApi.Connections;
using Umbraco.Community.Automate.WeatherApi.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.WeatherApi.Tests.Actions;

public class GetCurrentWeatherActionTests
{
    [Fact]
    public async Task Returns_the_current_weather()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, WeatherApiResponses.Current));

        var result = await Run(handler, new GetCurrentWeatherSettings { Location = "London", Culture = "fr-FR" });

        Assert.Equal(ActionResultStatus.Success, result.Status);
        var output = Assert.IsType<GetCurrentWeatherOutput>(result.OutputData);
        Assert.Equal("London", output.LocationName);
        Assert.Equal(14.0, output.TemperatureC);
        Assert.Equal("Partly cloudy", output.Condition);
        // WeatherAPI.com returns protocol-relative icon URLs; the action makes them absolute.
        Assert.Equal("https://cdn.weatherapi.com/weather/64x64/day/116.png", output.ConditionIconUrl);

        var query = handler.Requests.Single().RequestUri!.Query;
        Assert.Contains("q=London", query);
        Assert.Contains("lang=fr", query);
    }

    [Fact]
    public async Task Missing_location_is_a_validation_error()
    {
        var result = await Run(new StubHttpMessageHandler(), new GetCurrentWeatherSettings { Location = "" });

        Assert.Equal(ActionResultStatus.Failed, result.Status);
        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
    }

    [Fact]
    public async Task Malformed_culture_is_a_validation_error()
    {
        // .NET accepts any well-formed name (e.g. "xx-YY") as a culture, so only malformed
        // names are rejected; a well-formed unknown one is passed through to WeatherAPI.com.
        var result = await Run(new StubHttpMessageHandler(), new GetCurrentWeatherSettings { Location = "London", Culture = "!!" });

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
    }

    [Fact]
    public async Task Unresolved_api_key_reference_is_a_configuration_error()
    {
        var handler = new StubHttpMessageHandler();

        var result = await Run(handler, new GetCurrentWeatherSettings { Location = "London" }, new WeatherApiConnectionSettings());

        Assert.Equal(StepRunErrorCategory.ConfigurationError, result.ErrorCategory);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Invalid_key_is_an_authentication_error()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, WeatherApiResponses.InvalidKey));

        var result = await Run(handler, new GetCurrentWeatherSettings { Location = "London" });

        Assert.Equal(StepRunErrorCategory.Authentication, result.ErrorCategory);
        Assert.Equal("API key is invalid.", result.Exception?.Message);
    }

    [Fact]
    public async Task Unknown_location_is_a_validation_error()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.BadRequest, WeatherApiResponses.NoLocation));

        var result = await Run(handler, new GetCurrentWeatherSettings { Location = "Nowhereville" });

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
    }

    private static Task<ActionResult> Run(
        StubHttpMessageHandler handler,
        GetCurrentWeatherSettings settings,
        WeatherApiConnectionSettings? connection = null)
        => ActionTestHarness.For<GetCurrentWeatherAction>()
            .WithService<IHttpClientFactory>(new StubHttpClientFactory(handler))
            .WithSettings(settings)
            .WithConnection("community.weatherApi", connection ?? new WeatherApiConnectionSettings { ApiKey = "abc123" })
            .ExecuteAsync();
}
