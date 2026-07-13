namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Output produced by the <see cref="ClearRangeAction"/>.
/// </summary>
public sealed class ClearRangeOutput
{
    /// <summary>
    /// Gets the A1 range that was cleared, as reported by the Sheets API.
    /// </summary>
    public string? ClearedRange { get; init; }
}
