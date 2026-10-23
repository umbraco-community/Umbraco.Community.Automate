namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Output produced by the <see cref="AppendRowAction"/>.
/// </summary>
public sealed class AppendRowOutput
{
    /// <summary>
    /// Gets the A1 range that was updated by the append, as reported by the Sheets API.
    /// </summary>
    public string? UpdatedRange { get; init; }

    /// <summary>
    /// Gets the number of rows updated by the append.
    /// </summary>
    public int UpdatedRows { get; init; }

    /// <summary>
    /// Gets the number of cells updated by the append.
    /// </summary>
    public int UpdatedCells { get; init; }
}
