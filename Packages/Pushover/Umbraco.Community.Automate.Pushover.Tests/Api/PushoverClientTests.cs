using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Pushover.Api;
using Umbraco.Community.Automate.Pushover.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Pushover.Tests.Api;

public class PushoverClientTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, StepRunErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.TooManyRequests, StepRunErrorCategory.RateLimiting)]
    [InlineData(HttpStatusCode.BadGateway, StepRunErrorCategory.ServiceUnavailable)]
    public void Status_codes_map_to_retry_categories(HttpStatusCode status, StepRunErrorCategory expected)
        => Assert.Equal(expected, PushoverClient.Classify(status));

    [Fact]
    public async Task A_non_json_error_page_still_fails_cleanly()
    {
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("<html>Down</html>") };
        var client = new PushoverClient(new StubHttpClientFactory(new StubHttpMessageHandler(response)));

        var ex = await Assert.ThrowsAsync<PushoverApiException>(() => client.ValidateUserAsync("t", "u", CancellationToken.None));

        Assert.Equal(StepRunErrorCategory.ServiceUnavailable, ex.Category);
        Assert.Contains("503", ex.Message);
    }

    [Fact]
    public async Task Unreachable_service_is_service_unavailable()
    {
        var client = new PushoverClient(new StubHttpClientFactory(new ThrowingHandler()));

        var ex = await Assert.ThrowsAsync<PushoverApiException>(() => client.ValidateUserAsync("t", "u", CancellationToken.None));

        Assert.Equal(StepRunErrorCategory.ServiceUnavailable, ex.Category);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("No such host is known.");
    }
}
