using Umbraco.Community.Automate.DevTo.Client;
using Umbraco.Community.Automate.DevTo.Settings;

namespace Umbraco.Community.Automate.DevTo.Articles;

/// <summary>
/// Creates or updates a DEV article. An existing article is found by explicit ID or, failing
/// that, by canonical URL — so re-publishing a post updates its DEV copy instead of creating
/// a duplicate, and a retried step can't post twice.
/// </summary>
public sealed class DevToArticlePublisher
{
    public const string OutcomeCreated = "created";
    public const string OutcomeUpdated = "updated";

    private readonly DevToClient _client;

    public DevToArticlePublisher(DevToClient client)
    {
        _client = client;
    }

    public async Task<(string Outcome, DevToArticleOutput Output)> PublishAsync(
        DevToConnectionSettings connection, DevToArticleDraft draft, CancellationToken cancellationToken)
    {
        var articleId = draft.ExistingArticleId;

        if (articleId is null && !string.IsNullOrWhiteSpace(draft.CanonicalUrl))
        {
            var existing = await _client.FindArticleByCanonicalUrlAsync(connection, draft.CanonicalUrl, cancellationToken);
            articleId = existing?.Id;
        }

        var request = BuildRequest(draft, isUpdate: articleId is not null);

        var (outcome, article) = articleId is { } id
            ? (OutcomeUpdated, await _client.UpdateArticleAsync(connection, id, request, cancellationToken))
            : (OutcomeCreated, await _client.CreateArticleAsync(connection, request, cancellationToken));

        return (outcome, new DevToArticleOutput
        {
            ArticleId = article.Id,
            Url = article.Url,
            Slug = article.Slug,
            Title = article.Title ?? draft.Title,
            Published = article.Published,
            CanonicalUrl = article.CanonicalUrl ?? draft.CanonicalUrl,
        });
    }

    private static DevToArticleRequest BuildRequest(DevToArticleDraft draft, bool isUpdate) => new()
    {
        Title = draft.Title.Trim(),
        BodyMarkdown = draft.BodyMarkdown,
        // Not publishing never unpublishes: on an update the flag is left out, so an article
        // already published on DEV stays published.
        Published = draft.Publish ? true : isUpdate ? null : false,
        Tags = draft.Tags.Count > 0 ? draft.Tags : null,
        CanonicalUrl = NullIfBlank(draft.CanonicalUrl),
        Description = NullIfBlank(draft.Description),
        MainImage = NullIfBlank(draft.CoverImageUrl),
        Series = NullIfBlank(draft.Series),
    };

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
