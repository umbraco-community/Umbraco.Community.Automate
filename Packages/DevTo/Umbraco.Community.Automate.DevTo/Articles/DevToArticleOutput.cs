using Umbraco.Community.Automate.DevTo.Api;

namespace Umbraco.Community.Automate.DevTo.Articles;

/// <summary>
/// Output of Publish Content to DEV, available to later steps as
/// <c>${ steps.&lt;alias&gt;.url }</c> and so on.
/// </summary>
public sealed class DevToArticleOutput
{
    public long ArticleId { get; init; }

    public string? Url { get; init; }

    public string? Slug { get; init; }

    public string? Title { get; init; }

    public bool Published { get; init; }

    public string? CanonicalUrl { get; init; }
}
