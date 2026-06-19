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
}
