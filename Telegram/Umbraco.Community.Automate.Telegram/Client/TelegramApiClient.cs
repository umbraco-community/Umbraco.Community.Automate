using System.Net;
using System.Text;
using System.Text.Json;

namespace Umbraco.Community.Automate.Telegram.Client;

/// <summary>
/// Thin wrapper around the Telegram Bot API's sendMessage and getMe endpoints. Shared by
/// SendMessageAction, TelegramNotificationChannel, and TelegramConnectionType's connectivity
/// check, since they all authenticate and call Telegram the same way. Retries once on HTTP 429
/// (rate limited) using Telegram's own retry_after value before returning a failure.
/// </summary>
public static class TelegramApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static async Task<TelegramApiResult> SendMessageAsync(
        IHttpClientFactory httpClientFactory, string botToken, string chatId, string text, CancellationToken cancellationToken)
    {
        var firstAttempt = await SendMessageOnceAsync(httpClientFactory, botToken, chatId, text, cancellationToken);

        if (firstAttempt.StatusCode != HttpStatusCode.TooManyRequests)
            return firstAttempt;

        var delaySeconds = firstAttempt.RetryAfterSeconds ?? 1;
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

        return await SendMessageOnceAsync(httpClientFactory, botToken, chatId, text, cancellationToken);
    }

    public static async Task<TelegramApiResult> GetMeAsync(
        IHttpClientFactory httpClientFactory, string botToken, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("UmbracoAutomate");
        var url = $"https://api.telegram.org/bot{botToken}/getMe";

        using var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
            return TelegramApiResult.Succeeded();

        var errorBody = JsonSerializer.Deserialize<TelegramResponse<object>>(body, JsonOptions);
        return TelegramApiResult.Failed(response.StatusCode, errorBody?.Description);
    }

    private static async Task<TelegramApiResult> SendMessageOnceAsync(
        IHttpClientFactory httpClientFactory, string botToken, string chatId, string text, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("UmbracoAutomate");
        var url = $"https://api.telegram.org/bot{botToken}/sendMessage";

        var payload = JsonSerializer.Serialize(new
        {
            chat_id = chatId,
            text,
            parse_mode = "MarkdownV2",
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var parsed = JsonSerializer.Deserialize<TelegramResponse<TelegramMessageResult>>(body, JsonOptions);
            return TelegramApiResult.Succeeded(parsed?.Result?.MessageId);
        }

        var errorBody = JsonSerializer.Deserialize<TelegramResponse<object>>(body, JsonOptions);
        return TelegramApiResult.Failed(response.StatusCode, errorBody?.Description, errorBody?.Parameters?.RetryAfter);
    }

    private sealed class TelegramResponse<T>
    {
        public bool Ok { get; set; }
        public T? Result { get; set; }
        public string? Description { get; set; }
        public TelegramResponseParameters? Parameters { get; set; }
    }

    private sealed class TelegramResponseParameters
    {
        public int? RetryAfter { get; set; }
    }

    private sealed class TelegramMessageResult
    {
        public int MessageId { get; set; }
    }
}
