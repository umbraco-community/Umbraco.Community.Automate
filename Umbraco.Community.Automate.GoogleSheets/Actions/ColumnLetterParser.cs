using System.Text.RegularExpressions;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Converts spreadsheet column letter notation (A, B, ..., Z, AA, AB, ...) to a
/// zero-based column index, and validates that a given string is a valid column letter.
/// </summary>
public static partial class ColumnLetterParser
{
    [GeneratedRegex(@"^[A-Za-z]+$")]
    private static partial Regex LetterOnlyPattern();

    /// <summary>
    /// Returns <c>true</c> when <paramref name="column"/> consists only of ASCII letters
    /// (e.g. "A", "b", "AA") and is not empty.
    /// </summary>
    public static bool IsValid(string column) =>
        !string.IsNullOrWhiteSpace(column) && LetterOnlyPattern().IsMatch(column.Trim());

    /// <summary>
    /// Converts a column letter (A=0, B=1, Z=25, AA=26, ...) to a zero-based index.
    /// </summary>
    public static int ToIndex(string column)
    {
        var upper = column.Trim().ToUpperInvariant();
        var index = 0;
        foreach (var c in upper)
            index = index * 26 + (c - 'A' + 1);
        return index - 1;
    }
}
