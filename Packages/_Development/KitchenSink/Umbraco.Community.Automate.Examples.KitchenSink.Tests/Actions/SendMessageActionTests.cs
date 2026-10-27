using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Examples.KitchenSink.Actions;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;
using Umbraco.Community.Automate.Examples.KitchenSink.Connections;
using Umbraco.Community.Automate.Examples.KitchenSink.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Actions;

public class SendMessageActionTests
{
    [Fact]
    public async Task Sends_the_message_and_returns_what_the_service_received()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK,
            """{ "json": { "message": "Hello" }, "url": "https://httpbin.org/anything" }"""));

        var result = await Run(handler, new SendMessageSettings { Message = "Hello" });

        Assert.Equal(ActionResultStatus.Success, result.Status);
        var output = Assert.IsType<SendMessageOutput>(result.OutputData);
        Assert.Equal("Hello", output.Message);
        Assert.Equal("https://httpbin.org/anything", output.Url);

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.True(request.Headers.Contains("Idempotency-Key"));
    }

    [Fact]
    public async Task Blank_message_is_a_validation_error_and_calls_nothing()
    {
        var handler = new StubHttpMessageHandler();

        var result = await Run(handler, new SendMessageSettings { Message = " " });

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Rate_limiting_is_reported_so_Automate_can_retry()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, "slow down"));

        var result = await Run(handler, new SendMessageSettings { Message = "Hello" });

        Assert.Equal(StepRunErrorCategory.RateLimiting, result.ErrorCategory);
    }

    private static Task<ActionResult> Run(StubHttpMessageHandler handler, SendMessageSettings settings)
        => ActionTestHarness.For<SendMessageAction>()
            .WithService(new KitchenSinkClient(new StubHttpClientFactory(handler)))
            .WithSettings(settings)
            .WithConnection("community.examples.kitchenSink", new KitchenSinkConnectionSettings { ApiKey = "token" })
            .ExecuteAsync();
}
