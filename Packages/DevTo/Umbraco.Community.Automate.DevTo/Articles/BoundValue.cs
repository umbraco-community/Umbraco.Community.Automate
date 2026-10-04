using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Umbraco.Community.Automate.DevTo.Articles;

/// <summary>
/// Interprets the text a <c>${ binding }</c> produces. Automate stringifies bound values:
/// text stays text, but lists and objects — a media picker, a content picker — arrive as JSON,
/// and Rich Text arrives as HTML.
/// </summary>
public static partial class BoundValue
{
    private static readonly string[] UrlKeys = ["url", "mediaUrl", "src"];

    /// <summary>Returns the value as a single line of plain text, with any HTML removed.</summary>
    public static string? PlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var text = WebUtility.HtmlDecode(TagPattern().Replace(value, " "));
        text = WhitespacePattern().Replace(text, " ").Trim();

        return text.Length == 0 ? null : text;
    }

    /// <summary>
    /// Returns a URL from a plain URL, or from bound media/content JSON — the first item's
    /// <c>url</c> — such as <c>[{"key":"…","name":"hero","url":"/media/abc/hero.jpg"}]</c>.
    /// </summary>
    public static string? Url(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();
        if (value[0] is not ('[' or '{'))
            return value;

        try
        {
            using var doc = JsonDocument.Parse(value);
            return FindUrl(doc.RootElement);
        }
        catch (JsonException)
        {
            return value;
        }
    }

    private static string? FindUrl(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Array => element.EnumerateArray().Select(FindUrl).FirstOrDefault(u => !string.IsNullOrWhiteSpace(u)),
        JsonValueKind.Object => element.EnumerateObject()
            .Where(p => UrlKeys.Contains(p.Name, StringComparer.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.String)
            .Select(p => p.Value.GetString())
            .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u)),
        _ => null,
    };

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
