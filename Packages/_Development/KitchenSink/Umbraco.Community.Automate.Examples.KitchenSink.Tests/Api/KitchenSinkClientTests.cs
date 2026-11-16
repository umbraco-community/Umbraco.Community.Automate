using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;
using Umbraco.Community.Automate.Examples.KitchenSink.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Api;

public class KitchenSinkClientTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.Forbidden, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.BadRequest, StepRunErrorCategory.Validation)]
    [InlineData(HttpStatusCode.TooManyRequests, StepRunErrorCategory.RateLimiting)]
    [InlineData(HttpStatusCode.RequestTimeout, StepRunErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.BadGateway, StepRunErrorCategory.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Conflict, StepRunErrorCategory.InvalidResponse)]
    public void Status_codes_map_to_retry_categories(HttpStatusCode status, StepRunErrorCategory expected)
        => Assert.Equal(expected, KitchenSinkClient.Classify(status));

    [Fact]
    public async Task Unreachable_service_is_retryable()
    {
        var client = new KitchenSinkClient(new StubHttpClientFactory(new ThrowingHandler()));

        var ex = await Assert.ThrowsAsync<KitchenSinkApiException>(() => client.CheckApiKeyAsync("token", CancellationToken.None));

        Assert.Equal(StepRunErrorCategory.ServiceUnavailable, ex.Category);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("No such host is known.");
    }
}
