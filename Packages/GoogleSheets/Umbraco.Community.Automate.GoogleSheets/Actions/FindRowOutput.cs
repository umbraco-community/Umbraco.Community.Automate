namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Output produced by the <see cref="FindRowAction"/>.
/// </summary>
public sealed class FindRowOutput
{
    /// <summary>
    /// Gets whether a matching row was found.
    /// </summary>
    public bool Found { get; init; }

    /// <summary>
    /// Gets the 1-based row number of the first matching row, or 0 if not found.
    /// </summary>
    public int RowNumber { get; init; }

    /// <summary>
    /// Gets the column values of the matching row, or an empty list if not found.
    /// </summary>
    public List<string> Values { get; init; } = [];
}
