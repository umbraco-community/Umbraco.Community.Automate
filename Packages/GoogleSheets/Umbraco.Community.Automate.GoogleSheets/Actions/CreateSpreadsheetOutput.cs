namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Output produced by the <see cref="CreateSpreadsheetAction"/>.
/// </summary>
public sealed class CreateSpreadsheetOutput
{
    /// <summary>
    /// Gets the ID of the newly created spreadsheet.
    /// </summary>
    public string SpreadsheetId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full URL of the newly created spreadsheet.
    /// </summary>
    public string SpreadsheetUrl { get; init; } = string.Empty;
}
