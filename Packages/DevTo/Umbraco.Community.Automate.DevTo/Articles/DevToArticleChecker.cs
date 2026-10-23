using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.DevTo.Client;
using Umbraco.Community.Automate.DevTo.ConnectionTypes;
using Umbraco.Community.Automate.DevTo.Settings;

namespace Umbraco.Community.Automate.DevTo.Articles;

public sealed record DevToArticleCheckResult(IReadOnlyList<DevToArticleLink> Links, IReadOnlyList<string> Problems);

/// <summary>
/// Asks DEV what became of the articles a content item was posted as (still there? published?) and
/// updates the stored links. Only runs when someone asks, so the backoffice never calls DEV by itself.
/// </summary>
public sealed class DevToArticleChecker
{
    private readonly DevToArticleLinks _links;
    private readonly IConnectionService _connectionService;
    private readonly DevToClient _client;

    public DevToArticleChecker(DevToArticleLinks links, IConnectionService connectionService, DevToClient client)
    {
        _links = links;
        _connectionService = connectionService;
        _client = client;
    }

    public async Task<DevToArticleCheckResult> CheckAsync(Guid contentKey, CancellationToken cancellationToken)
    {
        var problems = new List<string>();

        foreach (var link in _links.Get(contentKey))
        {
            var (settings, problem) = await GetConnectionAsync(link.ConnectionId, cancellationToken);
            if (settings is null)
            {
                problems.Add(problem!);
                continue;
            }

            try
            {
                var article = await _client.FindArticleByIdAsync(settings, link.ArticleId, cancellationToken);

                _links.Save(contentKey, article is null
                    ? link with { Deleted = true, CheckedUtc = DateTime.UtcNow }
                    // Publishing changes the URL: drafts have a temporary slug.
                    : link with { Url = article.Url ?? link.Url, Published = article.Published, Deleted = false, CheckedUtc = DateTime.UtcNow });
            }
            catch (DevToApiException ex)
            {
                problems.Add(ex.Message);
            }
        }

        return new DevToArticleCheckResult(_links.Get(contentKey), problems.Distinct().ToArray());
    }

    // Links saved before connections were recorded fall back to the only DEV connection, if there's just one.
    private async Task<(DevToConnectionSettings? Settings, string? Problem)> GetConnectionAsync(Guid? connectionId, CancellationToken cancellationToken)
    {
        if (connectionId is null)
        {
            var devTo = (await _connectionService.GetAllConnectionsAsync(cancellationToken))
                .Where(c => c.Type == DevToConnectionType.ConnectionTypeAlias)
                .Take(2)
                .ToArray();

            if (devTo.Length != 1)
                return (null, "This link was saved before connections were recorded and there's more than one DEV connection, so it can't be checked. Publishing the page again records it.");

            connectionId = devTo[0].Id;
        }

        var settings = (await _connectionService.GetConfiguredConnectionAsync(connectionId.Value, cancellationToken))?.GetSettings<DevToConnectionSettings>();
        if (settings is null)
            return (null, "The DEV connection this was posted with no longer exists.");

        return DevToConnectionSettingsValidator.Validate(settings) is { } error ? (null, error) : (settings, null);
    }
}
