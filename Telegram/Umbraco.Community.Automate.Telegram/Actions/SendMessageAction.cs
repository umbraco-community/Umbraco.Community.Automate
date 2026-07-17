using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Telegram.Client;
using Umbraco.Community.Automate.Telegram.Connection;

namespace Umbraco.Community.Automate.Telegram.Actions;

/// <summary>
/// Sends a text message to a Telegram chat via the connected bot.
/// </summary>
[Action("telegram.sendMessage", "Send Telegram Message",
    Description = "Sends a text message to a Telegram chat via a bot.",
    Group = "Messaging",
    Icon = "icon-telegram",
    ConnectionTypeAlias = "telegram")]
public sealed class SendMessageAction : ActionBase<SendMessageSettings, SendMessageOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SendMessageAction(ActionInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<SendMessageSettings>();

        if (string.IsNullOrWhiteSpace(settings.Text))
            return ActionResult.Failed(new ArgumentException("Message text is required."), StepRunErrorCategory.Validation);

        var connectionSettings = context.Connection?.GetSettings<TelegramConnectionSettings>();
        if (string.IsNullOrWhiteSpace(connectionSettings?.BotToken))
            return ActionResult.Failed(
                new InvalidOperationException("Telegram bot token is not configured on the connection."),
                StepRunErrorCategory.ConfigurationError);

        var chatId = string.IsNullOrWhiteSpace(settings.ChatId) ? connectionSettings.ChatId : settings.ChatId;
        if (string.IsNullOrWhiteSpace(chatId))
            return ActionResult.Failed(
                new InvalidOperationException("No chat ID configured on the connection or this step."),
                StepRunErrorCategory.ConfigurationError);

        var result = await TelegramApiClient.SendMessageAsync(_httpClientFactory, connectionSettings.BotToken, chatId, settings.Text, cancellationToken);

        if (result.Success)
            return Success(new SendMessageOutput { MessageId = result.MessageId ?? 0, SentAt = DateTimeOffset.UtcNow });

        return result.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                ActionResult.Failed(new InvalidOperationException(result.ErrorDescription ?? "Telegram rejected the bot token."), StepRunErrorCategory.Authentication),
            HttpStatusCode.TooManyRequests =>
                ActionResult.Failed(new InvalidOperationException(result.ErrorDescription ?? "Telegram rate-limited this bot."), StepRunErrorCategory.RateLimiting),
            _ =>
                ActionResult.Failed(new InvalidOperationException(result.ErrorDescription ?? $"Telegram API returned {(int?)result.StatusCode}."), StepRunErrorCategory.InvalidResponse),
        };
    }
}
