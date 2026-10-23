using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Deserializes a Google Sheets <c>values.get</c> response. Shared across every action that
/// reads a range of cell values, since they all hit the same response shape.
/// </summary>
internal sealed record ValuesResponse(
    [property: JsonPropertyName("values")] List<List<string>>? Values);

/// <summary>
/// Scans rows for the first one whose value in a given column matches, optionally skipping
/// row 0 as a header. Shared by every action that locates a row by column value.
/// </summary>
public static class RowMatcher
{
    /// <summary>
    /// Returns the 0-based index of the first matching row, or -1 if none match.
    /// </summary>
    public static int FindRowIndex(
        List<List<string>> rows,
        int columnIndex,
        string value,
        StringComparison comparison,
        FindRowMatchMode matchMode = FindRowMatchMode.Exact,
        bool hasHeaderRow = true)
    {
        var start = hasHeaderRow ? 1 : 0;
        for (var i = start; i < rows.Count; i++)
        {
            var row = rows[i];
            var cellValue = columnIndex < row.Count ? row[columnIndex] : string.Empty;

            var isMatch = matchMode switch
            {
                FindRowMatchMode.Contains   => cellValue.Contains(value, comparison),
                FindRowMatchMode.StartsWith => cellValue.StartsWith(value, comparison),
                FindRowMatchMode.EndsWith   => cellValue.EndsWith(value, comparison),
                _                           => string.Equals(cellValue, value, comparison),
            };

            if (isMatch) return i;
        }

        return -1;
    }
}
