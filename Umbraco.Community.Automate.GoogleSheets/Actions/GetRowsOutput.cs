namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Output produced by the <see cref="GetRowsAction"/>.
/// </summary>
public sealed class GetRowsOutput
{
    /// <summary>
    /// Gets the data rows returned from the sheet.
    /// When <see cref="GetRowsSettings.HasHeaderRow"/> is true, the header row is excluded.
    /// </summary>
    public List<List<string>> Rows { get; init; } = [];

    /// <summary>
    /// Gets the number of data rows (excluding the header row if applicable).
    /// </summary>
    public int RowCount { get; init; }

    /// <summary>
    /// Gets the header row values when <see cref="GetRowsSettings.HasHeaderRow"/> is true,
    /// or an empty list otherwise.
    /// </summary>
    public List<string> Headers { get; init; } = [];
}
