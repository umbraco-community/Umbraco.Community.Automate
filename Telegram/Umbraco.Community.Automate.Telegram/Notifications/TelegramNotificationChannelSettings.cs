using System.ComponentModel.DataAnnotations;
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Telegram.Notifications;

/// <summary>
/// Settings for the Telegram notification channel. Self-contained (not linked to a Connection),
/// matching the existing Email/Webhook notification channel pattern.
/// </summary>
public sealed class TelegramNotificationChannelSettings
{
    [Field(Label = "Bot Token", Description = "The token issued by @BotFather for your bot.", IsSensitive = true)]
    [Required]
    public string? BotToken { get; set; }

    [Field(Label = "Chat ID", Description = "The chat, group, or channel ID to notify.", SortOrder = 1)]
    [Required]
    public string? ChatId { get; set; }
}
