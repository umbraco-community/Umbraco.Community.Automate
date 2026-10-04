using System.Globalization;
using System.Net;
using System.Text.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.WeatherApi.Configuration;
using Umbraco.Community.Automate.WeatherApi.Models;

namespace Umbraco.Community.Automate.WeatherApi.Api;

/// <summary>
/// Shared request-building logic for talking to the WeatherAPI.com API, used by both the
/// connection type and the action so the endpoint and key resolution only need to be defined once.
/// </summary>
internal static class WeatherApiRequestHelper
{
    /// <summary>
    /// A well-known location used to verify an API key without requiring the caller to supply one.
    /// </summary>
    private const string TestLocation = "London";

    /// <summary>
    /// Requests the current weather conditions for a location from the WeatherAPI.com
    /// <c>current.json</c> endpoint, optionally localizing the condition text into
    /// <paramref name="languageCode"/> (a WeatherAPI.com <c>lang</c> value, see
    /// <see cref="ResolveLanguageCode"/>).
    /// </summary>
    public static async Task<WeatherApiResult> GetCurrentWeatherAsync(
        HttpClient client,
        string apiKey,
        string location,
        string? languageCode,
        CancellationToken cancellationToken)
    {
        var url = $"{WeatherApiConfiguration.BaseUrl}/current.json?key={Uri.EscapeDataString(apiKey)}&q={Uri.EscapeDataString(location)}";

        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            url += $"&lang={Uri.EscapeDataString(languageCode)}";
        }

        using var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new WeatherApiResult(false, null, TryParseErrorMessage(body), body, response.StatusCode);
        }

        var parsed = JsonSerializer.Deserialize<WeatherApiCurrentResponse>(body);
        return new WeatherApiResult(true, parsed, null, body, response.StatusCode);
    }

    /// <summary>
    /// Requests today's weather forecast for a location from the WeatherAPI.com
    /// <c>forecast.json</c> endpoint (requested with <c>days=1</c>), optionally localizing the
    /// condition text into <paramref name="languageCode"/> (a WeatherAPI.com <c>lang</c> value,
    /// see <see cref="ResolveLanguageCode"/>).
    /// </summary>
    public static async Task<WeatherApiForecastResult> GetTodaysWeatherAsync(
        HttpClient client,
        string apiKey,
        string location,
        string? languageCode,
        CancellationToken cancellationToken)
    {
        var url = $"{WeatherApiConfiguration.BaseUrl}/forecast.json?key={Uri.EscapeDataString(apiKey)}&q={Uri.EscapeDataString(location)}&days=1";

        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            url += $"&lang={Uri.EscapeDataString(languageCode)}";
        }

        using var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new WeatherApiForecastResult(false, null, TryParseErrorMessage(body), body, response.StatusCode);
        }

        var parsed = JsonSerializer.Deserialize<WeatherApiForecastResponse>(body);
        return new WeatherApiForecastResult(true, parsed, null, body, response.StatusCode);
    }

    /// <summary>
    /// Validates an API key with a lightweight current-weather request against a well-known
    /// location, used to power the back office "Test connection" action.
    /// </summary>
    public static async Task<ConnectionValidationResult> ValidateApiKeyAsync(
        HttpClient client,
        string apiKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await GetCurrentWeatherAsync(client, apiKey, TestLocation, null, cancellationToken);

            return result.IsSuccess
                ? ConnectionValidationResult.Success("Connected")
                : ConnectionValidationResult.Failure(
                    result.ErrorMessage ?? $"WeatherAPI.com rejected the request with status {(int)result.StatusCode} ({result.StatusCode}).");
        }
        catch (Exception ex)
        {
            return ConnectionValidationResult.Failure("Could not reach the WeatherAPI.com server.", [ex.Message]);
        }
    }

    /// <summary>
    /// Maps a .NET culture (e.g. "fr-FR") to the language code WeatherAPI.com's <c>lang</c>
    /// query parameter expects. WeatherAPI.com only distinguishes Chinese by script rather than
    /// region, so Traditional Chinese cultures map to "zh_tw" and everything else falls back to
    /// the two-letter ISO language name.
    /// </summary>
    private static readonly string[] TraditionalChineseMarkers = ["Hant", "TW", "HK", "MO"];

    public static string ResolveLanguageCode(CultureInfo culture)
    {
        if (string.Equals(culture.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase) &&
            TraditionalChineseMarkers.Any(marker => culture.Name.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return "zh_tw";
        }

        return culture.TwoLetterISOLanguageName;
    }

    /// <summary>
    /// Maps a failed WeatherAPI.com status code to the category Automate uses to decide whether
    /// to retry the step. WeatherAPI.com returns 401 for a missing or invalid key, 403 for a
    /// disabled key or exceeded quota, and 400 when the location can't be found.
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

    private static string? TryParseErrorMessage(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<WeatherApiErrorResponse>(body)?.Error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>
/// The outcome of a WeatherAPI.com current weather request.
/// </summary>
internal sealed record WeatherApiResult(bool IsSuccess, WeatherApiCurrentResponse? Response, string? ErrorMessage, string RawBody, HttpStatusCode StatusCode);

/// <summary>
/// The outcome of a WeatherAPI.com forecast request.
/// </summary>
internal sealed record WeatherApiForecastResult(bool IsSuccess, WeatherApiForecastResponse? Response, string? ErrorMessage, string RawBody, HttpStatusCode StatusCode);
