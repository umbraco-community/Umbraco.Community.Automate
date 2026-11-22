using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Mastodon.Actions;
using Umbraco.Community.Automate.Mastodon.Api;
using Umbraco.Community.Automate.Mastodon.Connections;
using Umbraco.Community.Automate.Mastodon.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Mastodon.Tests.Actions;

public class SendMastodonPostActionTests
{
    private static readonly MastodonSettings Connection = new() { InstanceUrl = "https://mastodon.example", AccessToken = "token" };

    [Fact]
    public async Task Posts_the_status_with_the_token_and_an_idempotency_key()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "id": "1" }"""));

        var result = await Run(handler, new MastodonPostSettings { Content = "Hello" });

        Assert.Equal(ActionResultStatus.Success, result.Status);
        var request = handler.Requests.Single();
        Assert.Equal("https://mastodon.example/api/v1/statuses", request.RequestUri!.ToString());
        Assert.True(request.Headers.Contains("Idempotency-Key"));
        Assert.Contains("Hello", handler.Bodies.Single());
    }

    [Fact]
    public async Task Blank_content_is_a_validation_error_and_calls_nothing()
    {
        var handler = new StubHttpMessageHandler();

        var result = await Run(handler, new MastodonPostSettings { Content = " " });

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.UnprocessableEntity, StepRunErrorCategory.Validation)]
    [InlineData(HttpStatusCode.TooManyRequests, StepRunErrorCategory.RateLimiting)]
    [InlineData(HttpStatusCode.BadGateway, StepRunErrorCategory.ServiceUnavailable)]
    public async Task Failed_responses_are_categorised_so_Automate_knows_whether_to_retry(HttpStatusCode status, StepRunErrorCategory expected)
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(status, """{ "error": "nope" }"""));

        var result = await Run(handler, new MastodonPostSettings { Content = "Hello" });

        Assert.Equal(expected, result.ErrorCategory);
    }

    [Fact]
    public void Server_errors_are_retryable()
        => Assert.Equal(StepRunErrorCategory.ServiceUnavailable, MastodonErrors.Classify(HttpStatusCode.InternalServerError));

    private static Task<ActionResult> Run(StubHttpMessageHandler handler, MastodonPostSettings settings)
        => ActionTestHarness.For<SendMastodonPostAction>()
            .WithService(new MastodonClientFactory(new StubHttpClientFactory(handler)))
            .WithSettings(settings)
            .WithConnection("community.mastodon", Connection)
            .ExecuteAsync();
}
