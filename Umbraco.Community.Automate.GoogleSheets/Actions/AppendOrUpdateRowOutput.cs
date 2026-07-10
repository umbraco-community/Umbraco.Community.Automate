namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Output produced by the <see cref="AppendOrUpdateRowAction"/>.
/// </summary>
public sealed class AppendOrUpdateRowOutput
{
    /// <summary>
    /// Gets the 1-based row number that was written to.
    /// For an append this is the new last row; for an update it is the matched row.
    /// </summary>
    public int RowNumber { get; init; }

    /// <summary>
    /// Gets the A1 range that was written to, as reported by the Sheets API.
    /// </summary>
    public string? UpdatedRange { get; init; }

    /// <summary>
    /// Gets the number of rows updated or appended.
    /// </summary>
    public int UpdatedRows { get; init; }

    /// <summary>
    /// Gets the number of cells updated or appended.
    /// </summary>
    public int UpdatedCells { get; init; }
}
