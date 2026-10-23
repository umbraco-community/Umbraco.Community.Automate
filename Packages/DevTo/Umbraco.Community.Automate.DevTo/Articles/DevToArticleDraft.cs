namespace Umbraco.Community.Automate.DevTo.Articles;

public sealed class DevToArticleDraft
{
    public required string Title { get; init; }

    public required string BodyMarkdown { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>The original post's URL; also how an article from an earlier run is found.</summary>
    public string? CanonicalUrl { get; init; }

    public string? Description { get; init; }

    public string? CoverImageUrl { get; init; }

    public string? Series { get; init; }

    /// <summary>Publish on DEV; otherwise new articles are drafts and existing ones keep their state.</summary>
    public bool Publish { get; init; }

    /// <summary>Updates this article directly, skipping the canonical URL lookup.</summary>
    public long? ExistingArticleId { get; init; }
}
