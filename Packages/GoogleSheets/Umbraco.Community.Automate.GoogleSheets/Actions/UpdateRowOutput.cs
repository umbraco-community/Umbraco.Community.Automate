namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Output produced by the <see cref="UpdateRowAction"/>.
/// </summary>
public sealed class UpdateRowOutput
{
    /// <summary>
    /// Gets the 1-based row number that was updated, or 0 if not found.
    /// </summary>
    public int RowNumber { get; init; }

    /// <summary>
    /// Gets the A1 range that was updated, as reported by the Sheets API.
    /// </summary>
    public string? UpdatedRange { get; init; }

    /// <summary>
    /// Gets the number of rows updated.
    /// </summary>
    public int UpdatedRows { get; init; }

    /// <summary>
    /// Gets the number of cells updated.
    /// </summary>
    public int UpdatedCells { get; init; }
}
