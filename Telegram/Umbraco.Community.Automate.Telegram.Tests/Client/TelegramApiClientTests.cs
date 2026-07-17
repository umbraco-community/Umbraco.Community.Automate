using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using Shouldly;
using Umbraco.Community.Automate.Telegram.Client;
using Xunit;

namespace Umbraco.Community.Automate.Telegram.Tests.Client;

public class TelegramApiClientTests
{
    private const string TestBotToken = "123456:TEST-TOKEN";

    [Fact]
    public async Task SendMessageAsync_returns_success_with_message_id_on_ok_response()
    {
        var handler = StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":42}}""");

        var result = await TelegramApiClient.SendMessageAsync(
            CreateHttpClientFactory(handler), TestBotToken, "12345", "hello", CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.MessageId.ShouldBe(42);
    }

    [Fact]
    public async Task SendMessageAsync_posts_to_the_expected_telegram_url()
    {
        HttpRequestMessage? captured = null;
        var handler = StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""", req => captured = req);

        await TelegramApiClient.SendMessageAsync(
            CreateHttpClientFactory(handler), TestBotToken, "12345", "hello", CancellationToken.None);

        captured!.RequestUri!.ToString().ShouldBe($"https://api.telegram.org/bot{TestBotToken}/sendMessage");
        captured.Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    public async Task SendMessageAsync_returns_authentication_failure_on_401()
    {
        var handler = StubHandler(HttpStatusCode.Unauthorized, """{"ok":false,"description":"Unauthorized"}""");

        var result = await TelegramApiClient.SendMessageAsync(
            CreateHttpClientFactory(handler), "bad-token", "12345", "hello", CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        result.ErrorDescription.ShouldBe("Unauthorized");
    }

    [Fact]
    public async Task SendMessageAsync_retries_once_after_429_then_succeeds()
    {
        var callCount = 0;
        var handler = StubHandlerSequence(_ =>
        {
            callCount++;
            return callCount == 1
                ? (HttpStatusCode.TooManyRequests, """{"ok":false,"description":"Too Many Requests: retry after 0","parameters":{"retry_after":0}}""")
                : (HttpStatusCode.OK, """{"ok":true,"result":{"message_id":7}}""");
        });

        var result = await TelegramApiClient.SendMessageAsync(
            CreateHttpClientFactory(handler), TestBotToken, "12345", "hello", CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.MessageId.ShouldBe(7);
        callCount.ShouldBe(2);
    }

    [Fact]
    public async Task SendMessageAsync_fails_rate_limited_when_still_429_after_retry()
    {
        var handler = StubHandler(HttpStatusCode.TooManyRequests,
            """{"ok":false,"description":"Too Many Requests: retry after 0","parameters":{"retry_after":0}}""");

        var result = await TelegramApiClient.SendMessageAsync(
            CreateHttpClientFactory(handler), TestBotToken, "12345", "hello", CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GetMeAsync_returns_success_when_token_is_valid()
    {
        var handler = StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"id":1,"is_bot":true}}""");

        var result = await TelegramApiClient.GetMeAsync(CreateHttpClientFactory(handler), TestBotToken, CancellationToken.None);

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task GetMeAsync_returns_failure_when_token_is_invalid()
    {
        var handler = StubHandler(HttpStatusCode.Unauthorized, """{"ok":false,"description":"Unauthorized"}""");

        var result = await TelegramApiClient.GetMeAsync(CreateHttpClientFactory(handler), "bad-token", CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.ErrorDescription.ShouldBe("Unauthorized");
    }

    internal static IHttpClientFactory CreateHttpClientFactory(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("UmbracoAutomate")).Returns(client);
        return factory.Object;
    }

    internal static HttpMessageHandler StubHandler(HttpStatusCode code, string json, Action<HttpRequestMessage>? capture = null)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capture?.Invoke(req);
                return Task.FromResult(new HttpResponseMessage(code)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                });
            });
        return mock.Object;
    }

    internal static HttpMessageHandler StubHandlerSequence(Func<HttpRequestMessage, (HttpStatusCode Code, string Json)> responder)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                var (code, json) = responder(req);
                return Task.FromResult(new HttpResponseMessage(code)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                });
            });
        return mock.Object;
    }
}
