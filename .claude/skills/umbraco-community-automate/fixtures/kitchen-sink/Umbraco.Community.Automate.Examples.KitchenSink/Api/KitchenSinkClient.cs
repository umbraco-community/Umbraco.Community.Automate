using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Examples.KitchenSink.Configuration;
using Umbraco.Community.Automate.Examples.KitchenSink.Models;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Api;

/// <summary>
/// The one place that talks HTTP to the service. Every failure becomes an
/// <see cref="KitchenSinkApiException"/> with a <see cref="StepRunErrorCategory"/>, so the connection
/// type and all actions classify errors the same way.
/// </summary>
public sealed class KitchenSinkClient(IHttpClientFactory httpClientFactory)
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    /// <summary>Checks the API key. httpbin.org accepts any bearer token, so this always succeeds with one.</summary>
    public Task<BearerResponse> CheckApiKeyAsync(string apiKey, CancellationToken cancellationToken)
        => SendAsync<BearerResponse>(apiKey, new HttpRequestMessage(HttpMethod.Get, "bearer"), cancellationToken);

    /// <summary>Posts a message. The idempotency key stops a retried step from posting twice.</summary>
    public Task<AnythingResponse> SendMessageAsync(string apiKey, SendMessageRequest message, string idempotencyKey, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "anything") { Content = JsonContent.Create(message) };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return SendAsync<AnythingResponse>(apiKey, request, cancellationToken);
    }

    /// <summary>Asks httpbin.org to answer with the given status code.</summary>
    public async Task<HttpStatusCode> GetStatusAsync(string apiKey, int statusCode, CancellationToken cancellationToken)
    {
        using var response = await SendRawAsync(apiKey, new HttpRequestMessage(HttpMethod.Get, $"status/{statusCode}"), cancellationToken);
        return response.StatusCode;
    }

    private async Task<T> SendAsync<T>(string apiKey, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await SendRawAsync(apiKey, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new KitchenSinkApiException(
                $"The service returned {(int)response.StatusCode}: {body}",
                Classify(response.StatusCode),
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new KitchenSinkApiException("The service returned an empty response.", StepRunErrorCategory.InvalidResponse, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendRawAsync(string apiKey, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(KitchenSinkConfiguration.BaseUrl);
        client.Timeout = RequestTimeout;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            return await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new KitchenSinkApiException($"Could not reach the service: {ex.Message}", StepRunErrorCategory.ServiceUnavailable, inner: ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new KitchenSinkApiException("The request to the service timed out.", StepRunErrorCategory.Timeout, inner: ex);
        }
    }

    /// <summary>
    /// Maps a failed status code to the category Umbraco Automate uses to decide whether to retry
    /// the step: rate limits, timeouts and outages are temporary; the rest need the user to act.
    /// </summary>
    public static StepRunErrorCategory Classify(HttpStatusCode statusCode) => (int)statusCode switch
    {
        401 or 403 => StepRunErrorCategory.Authentication,
        400 or 404 or 422 => StepRunErrorCategory.Validation,
        408 => StepRunErrorCategory.Timeout,
        429 => StepRunErrorCategory.RateLimiting,
        >= 500 => StepRunErrorCategory.ServiceUnavailable,
        _ => StepRunErrorCategory.InvalidResponse,
    };
}
