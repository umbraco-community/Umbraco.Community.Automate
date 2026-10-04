using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.Community.Automate.DevTo.Content;

/// <summary>
/// State for one content-to-Markdown conversion, and the helpers an
/// <see cref="IDevToBlockConverter"/> needs to convert nested content consistently.
/// </summary>
public sealed class DevToConversionContext
{
    private readonly ContentMarkdownConverter _converter;

    internal DevToConversionContext(ContentMarkdownConverter converter, string? culture, Uri siteBaseUri, int depth = 0)
    {
        _converter = converter;
        Culture = culture;
        SiteBaseUri = siteBaseUri;
        Depth = depth;
    }

    /// <summary>Gets the culture being converted, or <c>null</c> for invariant content.</summary>
    public string? Culture { get; }

    /// <summary>Gets the absolute base URL that relative links and media URLs are resolved against.</summary>
    public Uri SiteBaseUri { get; }

    /// <summary>Gets how deeply nested the current block is.</summary>
    public int Depth { get; }

    /// <summary>Converts a nested element (for example a block inside a block) to Markdown.</summary>
    public string ConvertElement(IPublishedElement content, IPublishedElement? settings = null)
        => _converter.ConvertElement(content, settings, Deeper());

    /// <summary>Converts a property to Markdown, or returns <c>null</c> if it has nothing to show.</summary>
    public string? ConvertProperty(IPublishedProperty property)
        => _converter.ConvertProperty(property, this);

    /// <summary>Converts the property with the given alias, or returns <c>null</c> if there isn't one.</summary>
    public string? ConvertProperty(IPublishedElement element, string alias)
        => element.GetProperty(alias) is { } property ? ConvertProperty(property) : null;

    /// <summary>Gets a property's value for the culture being converted.</summary>
    public object? GetValue(IPublishedProperty property)
        => property.GetValue(ContentMarkdownConverter.CultureFor(property, Culture));

    /// <summary>Converts an HTML fragment to Markdown.</summary>
    public string HtmlToMarkdown(string html) => HtmlToMarkdownConverter.Convert(html);

    /// <summary>Makes a URL absolute against <see cref="SiteBaseUri"/>.</summary>
    public string? ResolveUrl(string? url) => UrlResolver.Resolve(url, SiteBaseUri);

    /// <summary>Gets the absolute URL of a media item.</summary>
    public string? GetMediaUrl(IPublishedContent media) => _converter.GetMediaUrl(media, this);

    internal DevToConversionContext Deeper() => new(_converter, Culture, SiteBaseUri, Depth + 1);
}
