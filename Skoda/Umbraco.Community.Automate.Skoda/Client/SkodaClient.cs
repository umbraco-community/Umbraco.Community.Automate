using System.Net.Http.Json;
using System.Text.Json;
using Umbraco.Community.Automate.Skoda.Client.Models;

namespace Umbraco.Community.Automate.Skoda.Client;

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
            ?? throw new InvalidOperationException("The Škoda API returned an empty vehicle response.");
    }

    public Task StartChargingAsync(string apiKey, string vin, CancellationToken cancellationToken = default) =>
        SendCommandAsync(apiKey, vin, "charging/start", null, cancellationToken);

    public Task StopChargingAsync(string apiKey, string vin, CancellationToken cancellationToken = default) =>
        SendCommandAsync(apiKey, vin, "charging/stop", null, cancellationToken);

    public Task StartAirConditioningAsync(
        string apiKey,
        string vin,
        StartAirConditioningConfiguration configuration,
        CancellationToken cancellationToken = default) =>
        SendCommandAsync(apiKey, vin, "air-conditioning/start", configuration, cancellationToken);

    public Task StopAirConditioningAsync(string apiKey, string vin, CancellationToken cancellationToken = default) =>
        SendCommandAsync(apiKey, vin, "air-conditioning/stop", null, cancellationToken);

    public Task StartAuxiliaryHeatingAsync(
        string apiKey,
        string vin,
        StartAuxiliaryHeatingConfiguration configuration,
        CancellationToken cancellationToken = default) =>
        SendCommandAsync(apiKey, vin, "auxiliary-heating/start", configuration, cancellationToken);

    public Task StopAuxiliaryHeatingAsync(string apiKey, string vin, CancellationToken cancellationToken = default) =>
        SendCommandAsync(apiKey, vin, "auxiliary-heating/stop", null, cancellationToken);

    public Task StartActiveVentilationAsync(string apiKey, string vin, CancellationToken cancellationToken = default) =>
        SendCommandAsync(apiKey, vin, "active-ventilation/start", null, cancellationToken);

    public Task StopActiveVentilationAsync(string apiKey, string vin, CancellationToken cancellationToken = default) =>
        SendCommandAsync(apiKey, vin, "active-ventilation/stop", null, cancellationToken);

    private async Task SendCommandAsync<T>(
        string apiKey,
        string vin,
        string command,
        T? body,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            apiKey,
            $"api/v1/vehicles/{EscapeVin(vin)}/{command}");

        request.Content = body is null
            ? JsonContent.Create(new { })
            : JsonContent.Create(body, options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private Task SendCommandAsync(
        string apiKey,
        string vin,
        string command,
        object? body,
        CancellationToken cancellationToken) =>
        SendCommandAsync<object>(apiKey, vin, command, body, cancellationToken);

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

        ProblemDetail? problem = null;

        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetail>(
                JsonOptions,
                cancellationToken);
        }
        catch (JsonException)
        {
            // Fall back to the raw response below.
        }

        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        throw new SkodaClientException(
            problem?.Detail ?? problem?.Title ?? $"Škoda API request failed with status code {(int)response.StatusCode}.",
            response.StatusCode,
            problem,
            rawResponse);
    }
}