using System.Text.RegularExpressions;

namespace Umbraco.Community.Automate.DevTo.Content;

/// <summary>
/// Makes site-relative URLs absolute. A post on DEV can't resolve <c>/media/…</c> or
/// <c>/blog/…</c> against your site, so every relative link and image would break.
/// </summary>
internal static partial class UrlResolver
{
    private static readonly string[] UrlKinds = ["inline", "reference", "attribute"];

    public static string? Resolve(string? url, Uri baseUri)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        url = url.Trim();

        if (url.StartsWith("//", StringComparison.Ordinal))
            return $"{baseUri.Scheme}:{url}";

        // On Linux "/media/a.jpg" parses as an absolute file:// URI, so check the scheme
        // rather than trusting UriKind.Absolute.
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.Scheme != Uri.UriSchemeFile)
            return url;

        if (url.StartsWith('#'))
            return url;

        return Uri.TryCreate(baseUri, url, out var resolved) ? resolved.ToString() : url;
    }

    /// <summary>
    /// Rewrites root-relative inline links and images, reference-style link definitions and HTML
    /// <c>src</c>/<c>href</c> attributes, leaving fenced and inline code untouched: a code sample
    /// showing <c>href="/contact"</c> must stay exactly as written.
    /// </summary>
    public static string AbsolutizeMarkdown(string markdown, Uri baseUri)
        => RelativeUrlPattern().Replace(markdown, m =>
        {
            if (m.Groups["code"].Success)
                return m.Value;

            var kind = UrlKinds.First(k => m.Groups[k + "Url"].Success);
            return m.Groups[kind + "Prefix"].Value + Resolve(m.Groups[kind + "Url"].Value, baseUri);
        });

    [GeneratedRegex(
        // \r? lets the closing fence match CRLF line endings too: in multiline mode $ only
        // matches before \n, so "```\r\n" would otherwise not close the block.
        """(?<code>^(?<fence>`{3,}|~{3,})[^\n]*\n.*?^\k<fence>[ \t]*\r?$|`[^`\n]+`)"""
        + """|(?<inlinePrefix>\]\()(?<inlineUrl>/(?!/)[^)\s]*)"""
        + """|(?<referencePrefix>^[ \t]{0,3}\[[^\]\n]+\]:[ \t]*)(?<referenceUrl>/(?!/)\S*)"""
        + """|(?<attributePrefix>\b(?:src|href)=["'])(?<attributeUrl>/(?!/)[^"']*)""",
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex RelativeUrlPattern();
}
