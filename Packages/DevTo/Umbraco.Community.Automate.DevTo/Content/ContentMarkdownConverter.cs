using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace Umbraco.Community.Automate.DevTo.Content;

/// <summary>
/// Converts Umbraco content to Markdown for DEV. Handles the Markdown editor, Rich Text,
/// Block List and Block Grid (including nested blocks and grid areas), plus the text, media
/// and link properties typically found inside blocks.
/// </summary>
public sealed partial class ContentMarkdownConverter
{
    private const string MarkdownEditorAlias = "Umbraco.MarkdownEditor";
    private const string TextBoxEditorAlias = "Umbraco.TextBox";
    private const string TextAreaEditorAlias = "Umbraco.TextArea";

    // Nesting beyond this is almost certainly a loop.
    private const int MaxDepth = 10;

    // Blocks with one of these properties are treated as code samples and rendered as a
    // fenced code block, using a language property for syntax highlighting if there is one.
    private static readonly HashSet<string> CodeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "code", "codeSnippet", "snippet", "sourceCode", "codeBlock",
    };

    private static readonly HashSet<string> LanguageAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "language", "lang", "codeLanguage", "syntax",
    };

    private static readonly string[] HeadingSuffixes = ["heading", "headline", "title"];

    private static readonly string[] AltTextAliases = ["altText", "alt", "alternativeText"];

    private readonly IPublishedUrlProvider _urlProvider;
    private readonly IReadOnlyList<IDevToBlockConverter> _blockConverters;

    public ContentMarkdownConverter(IPublishedUrlProvider urlProvider, IEnumerable<IDevToBlockConverter> blockConverters)
    {
        _urlProvider = urlProvider;
        _blockConverters = blockConverters.ToArray();
    }

    /// <summary>
    /// Converts the given properties, in order, into one Markdown document with absolute URLs.
    /// Aliases the element doesn't have are skipped.
    /// </summary>
    public string Convert(IPublishedElement content, IEnumerable<string> propertyAliases, string? culture, Uri siteBaseUri)
    {
        var context = new DevToConversionContext(this, culture, siteBaseUri);

        var markdown = Join(propertyAliases
            .Select(alias => content.GetProperty(alias))
            .Select(property => property is null ? null : ConvertProperty(property, context)));

        return UrlResolver.AbsolutizeMarkdown(markdown, siteBaseUri).Replace("\r\n", "\n");
    }

    /// <summary>Converts one property to Markdown, or returns <c>null</c> if it has nothing to show.</summary>
    public string? ConvertProperty(IPublishedProperty property, DevToConversionContext context)
    {
        if (context.Depth > MaxDepth)
            return null;

        var culture = CultureFor(property, context.Culture);
        var editorAlias = property.PropertyType.EditorAlias;

        // The Markdown editor's published value is rendered HTML; DEV wants the Markdown
        // the author actually wrote.
        if (editorAlias == MarkdownEditorAlias)
            return property.GetSourceValue(culture)?.ToString()?.Trim();

        return ConvertValue(property.GetValue(culture), editorAlias, context);
    }

    /// <summary>
    /// Converts a block's content element, trying registered <see cref="IDevToBlockConverter"/>s
    /// before the built-in conversion.
    /// </summary>
    public string ConvertElement(IPublishedElement content, IPublishedElement? settings, DevToConversionContext context)
    {
        if (context.Depth > MaxDepth)
            return string.Empty;

        foreach (var converter in _blockConverters)
        {
            if (converter.Convert(content, settings, context) is { } custom)
                return custom.Trim();
        }

        return ConvertElementDefault(content, context);
    }

    internal string? GetMediaUrl(IPublishedContent media, DevToConversionContext context)
    {
        if (media.ContentType.ItemType != PublishedItemType.Media)
            return null;

        var url = _urlProvider.GetMediaUrl(media, UrlMode.Default, context.Culture);
        return string.IsNullOrWhiteSpace(url) ? null : UrlResolver.Resolve(url, context.SiteBaseUri);
    }

    // Invariant properties on variant content must be read without a culture.
    internal static string? CultureFor(IPublishedProperty property, string? culture)
        => property.PropertyType.VariesByCulture() ? culture : null;

    private string? ConvertValue(object? value, string editorAlias, DevToConversionContext context) => value switch
    {
        null => null,

        // Block models come first: they are also IEnumerables of their items.
        BlockGridModel grid => ConvertGridItems(grid, context.Deeper()),
        BlockListModel list => Join(list.Select(item => ConvertElement(item.Content, item.Settings, context.Deeper()))),

        // Rich Text. Blocks inside it have already been rendered through the site's own
        // partial views, so they come out looking the way they do on the site.
        IHtmlEncodedString html => HtmlToMarkdownConverter.Convert(html.ToHtmlString()),

        string text when editorAlias is TextBoxEditorAlias or TextAreaEditorAlias => text.Trim(),

        IPublishedContent media => Image(media, context),
        IEnumerable<IPublishedContent> media => Join(media.Select(m => Image(m, context))),

        Link link => LinkMarkdown(link, context),
        IEnumerable<Link> links => Join(links.Select(l => LinkMarkdown(l, context)), "\n"),

        // Pickers of documents, colours, toggles, numbers and so on have no sensible
        // representation in an article, so they are left out.
        _ => null,
    };

    private string ConvertGridItems(IEnumerable<BlockGridItem> items, DevToConversionContext context)
        => Join(items.Select(item => Join(
            [
                ConvertElement(item.Content, item.Settings, context),
                .. item.Areas.Select(area => ConvertGridItems(area, context.Deeper())),
            ])));

    private string ConvertElementDefault(IPublishedElement content, DevToConversionContext context)
    {
        var properties = content.Properties.ToArray();

        var codeProperty = properties.FirstOrDefault(p =>
            CodeAliases.Contains(p.Alias) && context.GetValue(p) is string code && !string.IsNullOrWhiteSpace(code));

        var languageProperty = codeProperty is null
            ? null
            : properties.FirstOrDefault(p => LanguageAliases.Contains(p.Alias));

        var parts = new List<string?>();

        foreach (var property in properties)
        {
            if (property == codeProperty || property == languageProperty)
                continue;

            var markdown = ConvertProperty(property, context);
            if (string.IsNullOrWhiteSpace(markdown))
                continue;

            parts.Add(IsHeading(property) ? "## " + WhitespacePattern().Replace(markdown, " ") : markdown);
        }

        if (codeProperty is not null)
        {
            var language = languageProperty is null ? null : context.GetValue(languageProperty) as string;
            parts.Add(Fence((string)context.GetValue(codeProperty)!, language));
        }

        return Join(parts);
    }

    private static bool IsHeading(IPublishedProperty property)
        => property.PropertyType.EditorAlias == TextBoxEditorAlias &&
           HeadingSuffixes.Any(suffix => property.Alias.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

    private string? Image(IPublishedContent media, DevToConversionContext context)
    {
        if (GetMediaUrl(media, context) is not { } url)
            return null;

        var alt = AltTextAliases
            .Select(alias => media.GetProperty(alias) is { } property ? context.GetValue(property) as string : null)
            .FirstOrDefault(a => !string.IsNullOrWhiteSpace(a)) ?? PublishedContentCompat.GetName(media);

        return $"![{EscapeLinkText(alt)}]({url})";
    }

    private static string? LinkMarkdown(Link link, DevToConversionContext context)
    {
        if (context.ResolveUrl(link.Url) is not { } url)
            return null;

        return $"[{EscapeLinkText(string.IsNullOrWhiteSpace(link.Name) ? url : link.Name)}]({url})";
    }

    // A fence longer than any run of backticks in the code, so the code can't close it early.
    internal static string Fence(string code, string? language)
    {
        var longestRun = BacktickRunPattern().Matches(code).Select(m => m.Length).DefaultIfEmpty(0).Max();
        var fence = new string('`', Math.Max(3, longestRun + 1));
        var info = language is null ? string.Empty : LanguagePattern().Replace(language.Trim().ToLowerInvariant(), string.Empty);

        return $"{fence}{info}\n{code.Replace("\r\n", "\n").Trim('\n')}\n{fence}";
    }

    private static string EscapeLinkText(string? text)
        => (text ?? string.Empty).Replace("[", "\\[").Replace("]", "\\]");

    private static string Join(IEnumerable<string?> parts, string separator = "\n\n")
        => string.Join(separator, parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));

    [GeneratedRegex("`+")]
    private static partial Regex BacktickRunPattern();

    [GeneratedRegex("[^a-z0-9+#-]")]
    private static partial Regex LanguagePattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
