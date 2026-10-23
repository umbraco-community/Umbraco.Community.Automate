using Umbraco.Community.Automate.Mastodon.Settings;
using System.Net.Http.Headers;

namespace Umbraco.Community.Automate.Mastodon.Factory;

public sealed class MastodonClientFactory
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    private readonly IHttpClientFactory _httpClientFactory;

    public MastodonClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public HttpClient CreateClient(MastodonSettings settings)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = RequestTimeout;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
        return client;
    }
}
