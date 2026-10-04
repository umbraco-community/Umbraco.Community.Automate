using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Examples.Simple.Actions;
using Umbraco.Community.Automate.Examples.Simple.Connections;
using Umbraco.Community.Automate.Examples.Simple.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Examples.Simple.Tests.Actions;

public class SendMessageActionTests
{
    [Fact]
    public async Task Posts_the_message_and_returns_what_the_service_received()
    {
        var response = StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "json": { "message": "Hello" }, "url": "https://httpbin.org/anything" }""");
        response.Headers.Date = new DateTimeOffset(2026, 10, 2, 15, 32, 50, TimeSpan.Zero);
        var handler = new StubHttpMessageHandler(response);

        var result = await Run(handler, "Hello");

        Assert.Equal(ActionResultStatus.Success, result.Status);
        var output = Assert.IsType<SendMessageOutput>(result.OutputData);
        Assert.Equal("Hello", output.Message);
        Assert.Equal("https://httpbin.org/anything", output.Url);
        Assert.Equal(new DateTimeOffset(2026, 10, 2, 15, 32, 50, TimeSpan.Zero), output.ReceivedAt);

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://httpbin.org/anything", request.RequestUri!.ToString());
        Assert.Equal("Bearer token", request.Headers.Authorization!.ToString());
        Assert.Contains("Hello", handler.Bodies.Single());
    }

    [Fact]
    public async Task Blank_message_is_a_validation_error_and_calls_nothing()
    {
        var handler = new StubHttpMessageHandler();

        var result = await Run(handler, " ");

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.TooManyRequests, StepRunErrorCategory.RateLimiting)]
    [InlineData(HttpStatusCode.BadGateway, StepRunErrorCategory.ServiceUnavailable)]
    public async Task Failed_responses_are_categorised_so_Automate_knows_whether_to_retry(HttpStatusCode status, StepRunErrorCategory expected)
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(status, "{}"));

        var result = await Run(handler, "Hello");

        Assert.Equal(expected, result.ErrorCategory);
    }

    private static Task<ActionResult> Run(StubHttpMessageHandler handler, string message)
        => ActionTestHarness.For<SendMessageAction>()
            .WithService<IHttpClientFactory>(new StubHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Message = message })
            .WithConnection("community.examples.simple", new SimpleConnectionSettings { ApiKey = "token" })
            .ExecuteAsync();
}
