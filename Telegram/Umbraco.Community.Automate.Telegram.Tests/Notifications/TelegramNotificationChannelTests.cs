using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.Automate.Core.Notifications.Channels;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Telegram.Notifications;
using Umbraco.Community.Automate.Telegram.Tests.Client;
using Xunit;

namespace Umbraco.Community.Automate.Telegram.Tests.Notifications;

public class TelegramNotificationChannelTests
{
    private const string TestBotToken = "123456:TEST-TOKEN";

    [Fact]
    public async Task NotifyAsync_sends_escaped_subject_and_body_as_markdown()
    {
        HttpRequestMessage? captured = null;
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""", req => captured = req);

        var channel = new TelegramNotificationChannel(
            new NotificationChannelInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler),
            NullLogger<TelegramNotificationChannel>.Instance);

        await channel.NotifyAsync(
            new NotificationMessage { Subject = "Run failed!", TextBody = "Automation MyFlow (v3) failed." },
            new TelegramNotificationChannelSettings { BotToken = TestBotToken, ChatId = "999" },
            CancellationToken.None);

        var requestBody = await captured!.Content!.ReadAsStringAsync();
        var sentText = JsonDocument.Parse(requestBody).RootElement.GetProperty("text").GetString();

        sentText.ShouldBe("*Run failed\\!*\nAutomation MyFlow \\(v3\\) failed\\.");
    }

    [Fact]
    public async Task NotifyAsync_sends_subject_only_when_body_is_empty()
    {
        HttpRequestMessage? captured = null;
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""", req => captured = req);

        var channel = new TelegramNotificationChannel(
            new NotificationChannelInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler),
            NullLogger<TelegramNotificationChannel>.Instance);

        await channel.NotifyAsync(
            new NotificationMessage { Subject = "Run failed" },
            new TelegramNotificationChannelSettings { BotToken = TestBotToken, ChatId = "999" },
            CancellationToken.None);

        var requestBody = await captured!.Content!.ReadAsStringAsync();
        JsonDocument.Parse(requestBody).RootElement.GetProperty("text").GetString().ShouldBe("*Run failed*");
    }

    [Fact]
    public async Task NotifyAsync_does_not_throw_when_telegram_api_call_fails()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.BadRequest, """{"ok":false,"description":"Bad Request"}""");

        var channel = new TelegramNotificationChannel(
            new NotificationChannelInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler),
            NullLogger<TelegramNotificationChannel>.Instance);

        await Should.NotThrowAsync(() => channel.NotifyAsync(
            new NotificationMessage { Subject = "Run failed" },
            new TelegramNotificationChannelSettings { BotToken = TestBotToken, ChatId = "999" },
            CancellationToken.None));
    }

    [Theory]
    [InlineData(null, "999")]
    [InlineData(TestBotToken, null)]
    public async Task NotifyAsync_skips_sending_when_bot_token_or_chat_id_is_missing(string? botToken, string? chatId)
    {
        HttpRequestMessage? captured = null;
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""", req => captured = req);

        var channel = new TelegramNotificationChannel(
            new NotificationChannelInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler),
            NullLogger<TelegramNotificationChannel>.Instance);

        await channel.NotifyAsync(
            new NotificationMessage { Subject = "Run failed" },
            new TelegramNotificationChannelSettings { BotToken = botToken, ChatId = chatId },
            CancellationToken.None);

        captured.ShouldBeNull();
    }
}
