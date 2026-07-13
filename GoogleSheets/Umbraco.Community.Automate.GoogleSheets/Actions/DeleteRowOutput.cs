namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Output produced by the <see cref="DeleteRowAction"/>.
/// </summary>
public sealed class DeleteRowOutput
{
    /// <summary>
    /// Gets the 1-based row number that was deleted, or 0 if not found.
    /// </summary>
    public int DeletedRowNumber { get; init; }
}
