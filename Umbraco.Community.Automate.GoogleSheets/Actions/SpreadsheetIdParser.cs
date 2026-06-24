using System.Text.RegularExpressions;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Extracts a Google Sheets spreadsheet id from either a full Sheets URL
/// (https://docs.google.com/spreadsheets/d/{id}/...) or a raw id string.
/// </summary>
public static partial class SpreadsheetIdParser
{
    [GeneratedRegex(@"/spreadsheets/d/(?<id>[a-zA-Z0-9-_]+)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    public static string Parse(string input)
    {
        var trimmed = (input ?? string.Empty).Trim();
        var match = UrlPattern().Match(trimmed);
        return match.Success ? match.Groups["id"].Value : trimmed;
    }

    /// <summary>
    /// True when <paramref name="input"/> is an absolute http(s) URL that doesn't match the
    /// Google Sheets URL shape — e.g. a link to an unrelated site. Used to reject obviously
    /// wrong input before sending it to the Google API as if it were a literal spreadsheet id.
    /// </summary>
    public static bool LooksLikeUnrelatedUrl(string input)
    {
        var trimmed = (input ?? string.Empty).Trim();
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !UrlPattern().IsMatch(trimmed);
    }
}
