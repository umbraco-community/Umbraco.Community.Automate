using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.DevTo.Settings;

namespace Umbraco.Community.Automate.DevTo.Client;

/// <summary>The parts of the Forem (DEV) API this package uses. Failures throw <see cref="DevToApiException"/>.</summary>
public sealed class DevToClient
{
    public const string HttpClientName = "Umbraco.Community.Automate.DevTo";

    // The largest page /api/articles/me/all allows, and a cap so a misbehaving API can't page forever.
    private const int PageSize = 1000;
    private const int MaxPages = 20;

    private static readonly string UserAgent =
        $"Umbraco.Community.Automate.DevTo/{typeof(DevToClient).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public DevToClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<DevToUser> GetCurrentUserAsync(DevToConnectionSettings settings, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(settings, HttpMethod.Get, "/api/users/me");
        return await SendAsync<DevToUser>(request, "get the current user", cancellationToken);
    }

    /// <summary>
    /// Finds one of the authenticated user's articles (published or draft) whose canonical URL
    /// matches <paramref name="canonicalUrl"/>, ignoring scheme, case and trailing slashes.
    /// </summary>
    public Task<DevToArticle?> FindArticleByCanonicalUrlAsync(
        DevToConnectionSettings settings, string canonicalUrl, CancellationToken cancellationToken)
        => FindArticleAsync(settings, a => CanonicalUrl.AreEquivalent(a.CanonicalUrl, canonicalUrl), cancellationToken);

    /// <summary>Finds one of the authenticated user's articles (published or draft) by ID; <c>null</c> if it's been deleted.</summary>
    public Task<DevToArticle?> FindArticleByIdAsync(DevToConnectionSettings settings, long articleId, CancellationToken cancellationToken)
        => FindArticleAsync(settings, a => a.Id == articleId, cancellationToken);

    private async Task<DevToArticle?> FindArticleAsync(
        DevToConnectionSettings settings, Func<DevToArticle, bool> predicate, CancellationToken cancellationToken)
    {
        for (var page = 1; page <= MaxPages; page++)
        {
            using var request = CreateRequest(settings, HttpMethod.Get, $"/api/articles/me/all?page={page}&per_page={PageSize}");
            var articles = await SendAsync<List<DevToArticle>>(request, "list your articles", cancellationToken);

            var match = articles.FirstOrDefault(predicate);
            if (match is not null)
                return match;

            if (articles.Count < PageSize)
                return null;
        }

        return null;
    }

    public async Task<DevToArticle> CreateArticleAsync(
        DevToConnectionSettings settings, DevToArticleRequest article, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(settings, HttpMethod.Post, "/api/articles", article);
        return await SendAsync<DevToArticle>(request, "create the article", cancellationToken);
    }

    public async Task<DevToArticle> UpdateArticleAsync(
        DevToConnectionSettings settings, long articleId, DevToArticleRequest article, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(settings, HttpMethod.Put, $"/api/articles/{articleId}", article);
        return await SendAsync<DevToArticle>(request, $"update article {articleId}", cancellationToken);
    }

    private static HttpRequestMessage CreateRequest(
        DevToConnectionSettings settings, HttpMethod method, string pathAndQuery, DevToArticleRequest? article = null)
    {
        var baseUri = new Uri(settings.InstanceUrl.Trim().TrimEnd('/') + "/");
        var request = new HttpRequestMessage(method, new Uri(baseUri, pathAndQuery.TrimStart('/')));

        request.Headers.Add("api-key", settings.ApiKey.Trim());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.forem.api-v1+json"));
        // Forem rejects API requests that don't identify themselves.
        request.Headers.UserAgent.ParseAdd(UserAgent);

        // Not JsonContent: that streams the body chunked, which some APIs and proxies reject.
        if (article is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(new { article }, JsonOptions), Encoding.UTF8, "application/json");

        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, string operation, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new DevToApiException(
                $"Could not reach {request.RequestUri?.GetLeftPart(UriPartial.Authority)} to {operation}: {ex.Message}",
                StepRunErrorCategory.ServiceUnavailable, inner: ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DevToApiException($"Timed out trying to {operation}.", StepRunErrorCategory.Timeout, inner: ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new DevToApiException(
                    $"DEV API failed to {operation} ({(int)response.StatusCode} {response.ReasonPhrase}): {ExtractError(body)}",
                    Classify(response.StatusCode));
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                       ?? throw new JsonException("The response body was empty.");
            }
            catch (JsonException ex)
            {
                throw new DevToApiException(
                    $"DEV API returned an unexpected response when trying to {operation}: {ex.Message}",
                    StepRunErrorCategory.InvalidResponse, ex);
            }
        }
    }

    private static StepRunErrorCategory Classify(HttpStatusCode statusCode) => (int)statusCode switch
    {
        401 or 403 => StepRunErrorCategory.Authentication,
        400 or 404 or 422 => StepRunErrorCategory.Validation,
        408 => StepRunErrorCategory.Timeout,
        429 => StepRunErrorCategory.RateLimiting,
        >= 500 => StepRunErrorCategory.ServiceUnavailable,
        _ => StepRunErrorCategory.InvalidResponse,
    };

    // Forem errors look like {"error":"...","status":422}; fall back to the raw body.
    private static string ExtractError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "(no response body)";

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.String)
                return error.GetString()!;
        }
        catch (JsonException)
        {
            // Not JSON, e.g. an HTML error page.
        }

        return body.Length > 500 ? body[..500] + "…" : body;
    }
}
