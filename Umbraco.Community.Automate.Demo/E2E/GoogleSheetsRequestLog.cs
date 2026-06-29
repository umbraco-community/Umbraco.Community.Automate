using System.Collections.Concurrent;

namespace Umbraco.Community.Automate.Demo.E2E;

/// <summary>
/// Records the request URLs <see cref="GoogleSheetsStubHandler"/> has observed, so a Playwright
/// test can confirm what an action's outgoing call actually contained — e.g. a value bound from a
/// previous step's output — rather than only that the action ran. Registered as a singleton so it
/// is shared across the transient <see cref="GoogleSheetsStubHandler"/> instances HttpClientFactory
/// creates per request.
/// </summary>
public sealed class GoogleSheetsRequestLog
{
    private readonly ConcurrentQueue<string> _requestUris = new();

    public void Record(string requestUri) => _requestUris.Enqueue(requestUri);

    /// <summary>
    /// Returns every request URL recorded since the last call, clearing the log so each test
    /// starts from a clean slate without needing a separate reset call.
    /// </summary>
    public IReadOnlyList<string> Drain()
    {
        var items = new List<string>();
        while (_requestUris.TryDequeue(out var requestUri))
        {
            items.Add(requestUri);
        }

        return items;
    }
}
