using System.Net;
using System.Text.Json;
using Shouldly;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Telegram.Actions;
using Umbraco.Community.Automate.Telegram.Connection;
using Umbraco.Community.Automate.Telegram.Tests.Client;
using Xunit;

namespace Umbraco.Community.Automate.Telegram.Tests.Actions;

public class SendMessageActionTests
{
    private const string TestBotToken = "123456:TEST-TOKEN";

    [Fact]
    public async Task ExecuteAsync_sends_message_and_returns_message_id_on_success()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":99}}""");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (SendMessageOutput)result.OutputData!;
        output.MessageId.ShouldBe(99);
    }

    [Fact]
    public async Task ExecuteAsync_uses_connection_chat_id_when_no_override_is_set()
    {
        HttpRequestMessage? captured = null;
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""", req => captured = req);

        await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        var requestBody = await captured!.Content!.ReadAsStringAsync();
        JsonDocument.Parse(requestBody).RootElement.GetProperty("chat_id").GetString().ShouldBe("999");
    }

    [Fact]
    public async Task ExecuteAsync_uses_per_action_chat_id_override_when_set()
    {
        HttpRequestMessage? captured = null;
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""", req => captured = req);

        await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello", ChatId = "override-chat" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        var requestBody = await captured!.Content!.ReadAsStringAsync();
        JsonDocument.Parse(requestBody).RootElement.GetProperty("chat_id").GetString().ShouldBe("override-chat");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_text_is_empty()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, "{}");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_configuration_error_when_bot_token_missing()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, "{}");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = null, ChatId = "999" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.ConfigurationError);
    }

    [Fact]
    public async Task ExecuteAsync_fails_configuration_error_when_no_chat_id_available()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, "{}");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = null })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.ConfigurationError);
    }

    [Fact]
    public async Task ExecuteAsync_fails_authentication_on_401()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.Unauthorized, """{"ok":false,"description":"Unauthorized"}""");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Authentication);
    }

    [Fact]
    public async Task ExecuteAsync_fails_rate_limiting_when_still_429_after_retry()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.TooManyRequests,
            """{"ok":false,"description":"Too Many Requests","parameters":{"retry_after":0}}""");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.RateLimiting);
    }

    [Fact]
    public async Task ExecuteAsync_fails_invalid_response_on_other_errors()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.BadRequest,
            """{"ok":false,"description":"Bad Request: chat not found"}""");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
        result.Exception!.Message.ShouldContain("chat not found");
    }
}
