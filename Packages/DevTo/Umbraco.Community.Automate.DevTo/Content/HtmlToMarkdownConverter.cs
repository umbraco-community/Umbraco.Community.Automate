using System.Net;
using System.Text.RegularExpressions;
using ReverseMarkdown;

namespace Umbraco.Community.Automate.DevTo.Content;

/// <summary>
/// Converts Rich Text HTML to the GitHub-flavoured Markdown DEV expects.
/// </summary>
internal static partial class HtmlToMarkdownConverter
{
    public static string Convert(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        html = IframePattern().Replace(html, m => $"<p>{{% embed {ToEmbedUrl(m.Groups["src"].Value)} %}}</p>");

        // A converter per call, as ReverseMarkdown makes no thread-safety promises. Bypass keeps
        // the text of tags Markdown has no equivalent for (<span>, <figure>).
        var config = new Config { GithubFlavored = true };
        config.Tags.Unknown = Config.UnknownTagsOption.Bypass;
        config.Formatting.RemoveComments = true;
        config.Links.SmartHref = true;

        return new Converter(config).Convert(html).Trim();
    }

    // DEV's {% embed %} wants a video's page URL, not the player URL an iframe points at.
    internal static string ToEmbedUrl(string src)
    {
        src = WebUtility.HtmlDecode(src.Trim());
        if (src.StartsWith("//", StringComparison.Ordinal))
            src = "https:" + src;

        if (YouTubePattern().Match(src) is { Success: true } youTube)
            return $"https://www.youtube.com/watch?v={youTube.Groups["id"].Value}";

        if (VimeoPattern().Match(src) is { Success: true } vimeo)
            return $"https://vimeo.com/{vimeo.Groups["id"].Value}";

        return src;
    }

    [GeneratedRegex("""<iframe\b[^>]*?\bsrc=["'](?<src>[^"']+)["'][^>]*>.*?</iframe>""", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex IframePattern();

    [GeneratedRegex("""youtube(?:-nocookie)?\.com/embed/(?<id>[\w-]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex YouTubePattern();

    [GeneratedRegex("""player\.vimeo\.com/video/(?<id>\d+)""", RegexOptions.IgnoreCase)]
    private static partial Regex VimeoPattern();
}
