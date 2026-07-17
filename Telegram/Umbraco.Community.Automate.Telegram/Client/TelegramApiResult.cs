using System.Net;

namespace Umbraco.Community.Automate.Telegram.Client;

/// <summary>
/// The outcome of a Telegram Bot API call.
/// </summary>
public sealed record TelegramApiResult
{
    public required bool Success { get; init; }
    public HttpStatusCode? StatusCode { get; init; }
    public string? ErrorDescription { get; init; }
    public int? RetryAfterSeconds { get; init; }
    public int? MessageId { get; init; }

    public static TelegramApiResult Succeeded(int? messageId = null) =>
        new() { Success = true, MessageId = messageId };

    public static TelegramApiResult Failed(HttpStatusCode statusCode, string? errorDescription, int? retryAfterSeconds = null) =>
        new() { Success = false, StatusCode = statusCode, ErrorDescription = errorDescription, RetryAfterSeconds = retryAfterSeconds };
}
