using System.Net;
using Moq;
using Shouldly;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Telegram.Connection;
using Umbraco.Community.Automate.Telegram.Tests.Client;
using Xunit;

namespace Umbraco.Community.Automate.Telegram.Tests.Connection;

public class TelegramConnectionTypeTests
{
    [Fact]
    public async Task ValidateAsync_returns_success_when_bot_token_is_valid()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"id":1,"is_bot":true}}""");
        var connectionType = new TelegramConnectionType(
            new ConnectionTypeInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler));

        var result = await connectionType.ValidateAsync(
            new TelegramConnectionSettings { BotToken = "123456:TEST-TOKEN", ChatId = "123" },
            CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Success);
    }

    [Fact]
    public async Task ValidateAsync_returns_failure_when_bot_token_is_rejected()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.Unauthorized, """{"ok":false,"description":"Unauthorized"}""");
        var connectionType = new TelegramConnectionType(
            new ConnectionTypeInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler));

        var result = await connectionType.ValidateAsync(
            new TelegramConnectionSettings { BotToken = "bad-token", ChatId = "123" },
            CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Failure);
        result.Message.ShouldBe("Unauthorized");
    }

    [Fact]
    public async Task ValidateAsync_returns_failure_when_bot_token_is_missing()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, "{}");
        var connectionType = new TelegramConnectionType(
            new ConnectionTypeInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler));

        var result = await connectionType.ValidateAsync(
            new TelegramConnectionSettings { BotToken = null, ChatId = "123" },
            CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Failure);
    }
}
