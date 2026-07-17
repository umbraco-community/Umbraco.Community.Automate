using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Telegram.Client;

namespace Umbraco.Community.Automate.Telegram.Connection;

/// <summary>
/// Connection type for a Telegram bot, authenticated with a static bot token (no OAuth).
/// </summary>
[ConnectionType("telegram", "Telegram",
    Group = "Messaging",
    Icon = "icon-telegram",
    Description = "Connect a Telegram bot to send messages from your workflows.")]
public sealed class TelegramConnectionType : ConnectionTypeBase<TelegramConnectionSettings>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TelegramConnectionType(ConnectionTypeInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override async Task<ConnectionValidationResult> ValidateAsync(object? settings, CancellationToken cancellationToken)
    {
        if (settings is not TelegramConnectionSettings typed || string.IsNullOrWhiteSpace(typed.BotToken))
            return ConnectionValidationResult.Failure("A bot token is required.");

        var result = await TelegramApiClient.GetMeAsync(_httpClientFactory, typed.BotToken, cancellationToken);

        return result.Success
            ? ConnectionValidationResult.Success("Bot token verified.")
            : ConnectionValidationResult.Failure(result.ErrorDescription ?? "Telegram rejected the bot token.");
    }
}
