using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Notifications.Channels;
using Umbraco.Community.Automate.Telegram.Client;

namespace Umbraco.Community.Automate.Telegram.Notifications;

/// <summary>
/// Notification channel that sends a Telegram message when a run matches the automation's
/// configured notification conditions (e.g. Failed).
/// </summary>
[NotificationChannel("telegram", "Telegram",
    Description = "Sends a Telegram message when a run matches the configured notification conditions.",
    Icon = "icon-telegram")]
public sealed class TelegramNotificationChannel : NotificationChannelBase<TelegramNotificationChannelSettings>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramNotificationChannel> _logger;

    public TelegramNotificationChannel(
        NotificationChannelInfrastructure infrastructure,
        IHttpClientFactory httpClientFactory,
        ILogger<TelegramNotificationChannel> logger)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task NotifyAsync(
        NotificationMessage message, TelegramNotificationChannelSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.BotToken) || string.IsNullOrWhiteSpace(settings.ChatId))
        {
            _logger.LogWarning("Telegram notification channel is missing a bot token or chat ID, skipping");
            return;
        }

        var text = BuildMessageText(message);
        var result = await TelegramApiClient.SendMessageAsync(_httpClientFactory, settings.BotToken, settings.ChatId, text, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning("Telegram notification failed: {Description}", result.ErrorDescription);
        }
    }

    private static string BuildMessageText(NotificationMessage message)
    {
        var subject = TelegramMarkdownEscaper.Escape(message.Subject ?? "Automation notification");
        var body = string.IsNullOrWhiteSpace(message.TextBody) ? null : TelegramMarkdownEscaper.Escape(message.TextBody);
        return body is null ? $"*{subject}*" : $"*{subject}*\n{body}";
    }
}
