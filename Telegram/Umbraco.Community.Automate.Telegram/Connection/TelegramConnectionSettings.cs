using System.ComponentModel.DataAnnotations;
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Telegram.Connection;

/// <summary>
/// Settings for the Telegram connection type.
/// </summary>
public sealed class TelegramConnectionSettings
{
    [Field(Label = "Bot Token", Description = "The token issued by @BotFather for your bot.", IsSensitive = true)]
    [Required]
    public string? BotToken { get; set; }

    [Field(Label = "Chat ID",
        Description = "The default chat, group, or channel ID this bot sends messages to.",
        SortOrder = 1)]
    [Required]
    public string? ChatId { get; set; }
}
