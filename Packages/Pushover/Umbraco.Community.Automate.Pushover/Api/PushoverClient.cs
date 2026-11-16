using System.Net;
using System.Net.Http.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Pushover.Configuration;
using Umbraco.Community.Automate.Pushover.Models;

namespace Umbraco.Community.Automate.Pushover.Api;

/// <summary>
/// The one place that talks to Pushover. Every failure becomes a <see cref="PushoverApiException"/>
/// with a <see cref="StepRunErrorCategory"/>, so the connection check and the action report errors
/// the same way.
/// </summary>
public sealed class PushoverClient(IHttpClientFactory httpClientFactory)
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    /// <summary>Checks an API token and user or group key. Doesn't send a notification.</summary>
    public Task<PushoverApiResponse> ValidateUserAsync(string apiToken, string userKey, CancellationToken cancellationToken)
        => PostAsync("users/validate.json", new Dictionary<string, string> { ["token"] = apiToken, ["user"] = userKey }, cancellationToken);

    /// <summary>Sends a notification. The fields are Pushover's form fields (token, user, message, ...).</summary>
    public Task<PushoverApiResponse> SendMessageAsync(IDictionary<string, string> fields, CancellationToken cancellationToken)
        => PostAsync("messages.json", fields, cancellationToken);

    private async Task<PushoverApiResponse> PostAsync(string path, IDictionary<string, string> fields, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(PushoverConfiguration.BaseUrl);
        client.Timeout = RequestTimeout;

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(path, new FormUrlEncodedContent(fields), cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new PushoverApiException($"Could not reach Pushover: {ex.Message}", StepRunErrorCategory.ServiceUnavailable, inner: ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PushoverApiException("The request to Pushover timed out.", StepRunErrorCategory.Timeout, inner: ex);
        }

        using (response)
        {
            // Pushover explains a rejection in "errors", e.g. "application token is invalid".
            PushoverApiResponse? body = null;
            try
            {
                body = await response.Content.ReadFromJsonAsync<PushoverApiResponse>(cancellationToken);
            }
            catch (System.Text.Json.JsonException)
            {
                // Not JSON (an outage page, say): fall through with no body.
            }

            if (response.IsSuccessStatusCode && body?.Status == 1)
                return body;

            var reason = body?.Errors is { Count: > 0 } errors ? string.Join(" ", errors) : $"HTTP {(int)response.StatusCode}";
            throw new PushoverApiException($"Pushover rejected the request: {reason}", Classify(response.StatusCode), response.StatusCode);
        }
    }

    /// <summary>
    /// Automate decides whether to retry a step from its category: rate limits and outages are
    /// temporary; a rejected token, key or message needs the user to act.
    /// </summary>
    public static StepRunErrorCategory Classify(HttpStatusCode statusCode) => (int)statusCode switch
    {
        401 or 403 => StepRunErrorCategory.Authentication,
        >= 400 and < 429 => StepRunErrorCategory.Validation,
        429 => StepRunErrorCategory.RateLimiting,
        >= 500 => StepRunErrorCategory.ServiceUnavailable,
        _ => StepRunErrorCategory.InvalidResponse,
    };
}
