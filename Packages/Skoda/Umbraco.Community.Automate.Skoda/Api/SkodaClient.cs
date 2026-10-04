using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Skoda.Models;

namespace Umbraco.Community.Automate.Skoda.Api;

internal sealed class SkodaClient(HttpClient httpClient) : ISkodaClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<VehicleResponse> GetVehicleAsync(
        string apiKey,
        string vin,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, apiKey, $"api/v1/vehicles/{EscapeVin(vin)}");
        using var response = await httpClient.SendAsync(request, cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<VehicleResponse>(JsonOptions, cancellationToken)
            ?? throw new SkodaClientException("The Škoda API returned an empty vehicle response.", HttpStatusCode.OK, null, null);
    }

    public Task StartChargingAsync(string apiKey, string vin, CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Post, apiKey, vin, "charging/start", null, cancellationToken);

    public Task StopChargingAsync(string apiKey, string vin, CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Post, apiKey, vin, "charging/stop", null, cancellationToken);

    public Task SetChargingLimitAsync(
        string apiKey,
        string vin,
        int targetStateOfChargeInPercent,
        CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Put, apiKey, vin, "charging/limit", new ChargingLimit(targetStateOfChargeInPercent), cancellationToken);

    public Task SetChargeModeAsync(string apiKey, string vin, string chargeMode, CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Put, apiKey, vin, "charging/mode", new ChargeMode(chargeMode), cancellationToken);

    public Task UpdateChargingProfileAsync(
        string apiKey,
        string vin,
        long profileId,
        ChargingProfile profile,
        CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Put, apiKey, vin, $"charging-profiles/{profileId}", profile, cancellationToken);

    public Task StartAirConditioningAsync(
        string apiKey,
        string vin,
        StartAirConditioningConfiguration configuration,
        CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Post, apiKey, vin, "air-conditioning/start", configuration, cancellationToken);

    public Task StopAirConditioningAsync(string apiKey, string vin, CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Post, apiKey, vin, "air-conditioning/stop", null, cancellationToken);

    public Task StartAuxiliaryHeatingAsync(
        string apiKey,
        string vin,
        StartAuxiliaryHeatingConfiguration configuration,
        CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Post, apiKey, vin, "auxiliary-heating/start", configuration, cancellationToken);

    public Task StopAuxiliaryHeatingAsync(string apiKey, string vin, CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Post, apiKey, vin, "auxiliary-heating/stop", null, cancellationToken);

    public Task StartActiveVentilationAsync(string apiKey, string vin, CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Post, apiKey, vin, "active-ventilation/start", null, cancellationToken);

    public Task StopActiveVentilationAsync(string apiKey, string vin, CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Post, apiKey, vin, "active-ventilation/stop", null, cancellationToken);

    private async Task SendCommandAsync<T>(
        HttpMethod method,
        string apiKey,
        string vin,
        string command,
        T? body,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(
            method,
            apiKey,
            $"api/v1/vehicles/{EscapeVin(vin)}/{command}");

        request.Content = body is null
            ? JsonContent.Create(new { })
            : JsonContent.Create(body, options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private Task SendCommandAsync(
        HttpMethod method,
        string apiKey,
        string vin,
        string command,
        object? body,
        CancellationToken cancellationToken) =>
        SendCommandAsync<object>(method, apiKey, vin, command, body, cancellationToken);

    private static HttpRequestMessage CreateRequest(HttpMethod method, string apiKey, string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-API-Key", apiKey);
        request.Headers.Accept.ParseAdd("application/json");
        return request;
    }

    private static string EscapeVin(string vin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vin);
        return Uri.EscapeDataString(vin);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        // Read the body once as a string and parse the JSON from that - some HttpContent
        // implementations (a real network response chief among them) back a forward-only
        // stream, so reading it a second time after ReadFromJsonAsync already consumed it
        // silently returns empty content instead of the actual body.
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        ProblemDetail? problem = null;

        try
        {
            problem = JsonSerializer.Deserialize<ProblemDetail>(rawResponse, JsonOptions);
        }
        catch (JsonException)
        {
            // Fall back to the raw response below.
        }

        throw new SkodaClientException(
            problem?.Detail ?? problem?.Title ?? $"Škoda API request failed with status code {(int)response.StatusCode}.",
            response.StatusCode,
            problem,
            rawResponse);
    }

    /// <summary>
    /// Automate decides whether to retry a step from its category: rate limits and outages are
    /// temporary; a rejected key, an unknown vehicle or an invalid command needs the user to act.
    /// </summary>
    internal static StepRunErrorCategory Classify(HttpStatusCode? statusCode) => (int?)statusCode switch
    {
        401 or 403 => StepRunErrorCategory.Authentication,
        429 => StepRunErrorCategory.RateLimiting,
        >= 400 and < 500 => StepRunErrorCategory.Validation,
        >= 500 => StepRunErrorCategory.ServiceUnavailable,
        _ => StepRunErrorCategory.InvalidResponse,
    };
}
