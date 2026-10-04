using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;
using Umbraco.Community.Automate.DevTo.Api;

namespace Umbraco.Community.Automate.DevTo.Content;

public sealed record DevToRenderOptions(IReadOnlyList<string> BodyAliases, string? Culture, string? SiteUrl, string? CanonicalUrl);

public sealed record DevToRenderedContent(string? Culture, Uri SiteBaseUri, string CanonicalUrl, string Body);

public sealed record DevToRenderError(string Message, StepRunErrorCategory Category);

/// <summary>Turns a content item into an article body and canonical URL. Shared by the action and its preview.</summary>
public sealed class DevToContentRenderer
{
    private readonly IPublishedUrlProvider _urlProvider;
    private readonly ContentMarkdownConverter _converter;

    public DevToContentRenderer(IPublishedUrlProvider urlProvider, ContentMarkdownConverter converter)
    {
        _urlProvider = urlProvider;
        _converter = converter;
    }

    public (DevToRenderedContent? Content, DevToRenderError? Error) Render(IPublishedContent content, DevToRenderOptions options)
    {
        var aliases = options.BodyAliases;
        if (aliases.Count == 0)
            return Invalid("At least one body property alias is required.");

        var missing = aliases.Where(alias => content.GetProperty(alias) is null).ToArray();
        if (missing.Length > 0)
            return Invalid($"'{content.ContentType.Alias}' has no propert{(missing.Length == 1 ? "y" : "ies")} named {string.Join(", ", missing.Select(m => $"'{m}'"))}.");

        var culture = ResolveCulture(options.Culture, content);

        var (siteBaseUri, canonicalUrl, urlError) = ResolveUrls(content, culture, options);
        if (urlError is not null)
            return (null, new DevToRenderError(urlError, StepRunErrorCategory.ConfigurationError));

        var body = _converter.Convert(content, aliases, culture, siteBaseUri!);
        if (string.IsNullOrWhiteSpace(body))
            return Invalid($"The body propert{(aliases.Count == 1 ? "y" : "ies")} {string.Join(", ", aliases)} produced no content.");

        return (new DevToRenderedContent(culture, siteBaseUri!, canonicalUrl!, body), null);
    }

    // An explicit Site URL wins; otherwise Umbraco's absolute URL for the content is used.
    private (Uri? SiteBaseUri, string? CanonicalUrl, string? Error) ResolveUrls(
        IPublishedContent content, string? culture, DevToRenderOptions options)
    {
        Uri? siteBaseUri;
        string? contentUrl;

        if (!string.IsNullOrWhiteSpace(options.SiteUrl))
        {
            if (!HttpUrl.TryCreate(options.SiteUrl.Trim().TrimEnd('/') + "/", out siteBaseUri))
                return (null, null, $"Site URL '{options.SiteUrl}' is not an absolute http(s) URL.");

            var relative = content.Url(_urlProvider, culture, UrlMode.Relative);
            contentUrl = IsRoutable(relative) ? UrlResolver.Resolve(relative, siteBaseUri) : null;
        }
        else
        {
            var absolute = content.Url(_urlProvider, culture, UrlMode.Absolute);
            if (!HttpUrl.TryCreate(absolute, out var absoluteUri))
                return (null, null,
                    "Umbraco could not produce an absolute URL for this content. Assign a domain, set " +
                    "Umbraco:CMS:WebRouting:UmbracoApplicationUrl, or fill in the step's Site URL setting.");

            siteBaseUri = new Uri(absoluteUri.GetLeftPart(UriPartial.Authority) + "/");
            contentUrl = absoluteUri.ToString();
        }

        var canonicalUrl = string.IsNullOrWhiteSpace(options.CanonicalUrl) ? contentUrl : options.CanonicalUrl.Trim();

        if (canonicalUrl is null)
            return (null, null, "This content has no public URL to use as the canonical URL. Set the step's Canonical URL setting.");

        if (!HttpUrl.IsValid(canonicalUrl))
            return (null, null, $"Canonical URL '{canonicalUrl}' is not an absolute http(s) URL.");

        return (siteBaseUri, canonicalUrl, null);
    }

    private static bool IsRoutable(string? url) => !string.IsNullOrWhiteSpace(url) && url != "#";

    private static string? ResolveCulture(string? requested, IPublishedContent content)
    {
        if (!content.ContentType.VariesByCulture())
            return null;

        return !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : PublishedContentCompat.GetCultures(content).Keys.FirstOrDefault();
    }

    private static (DevToRenderedContent?, DevToRenderError?) Invalid(string message)
        => (null, new DevToRenderError(message, StepRunErrorCategory.Validation));
}
