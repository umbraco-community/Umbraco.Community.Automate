namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Output produced by the <see cref="CreateSheetAction"/>.
/// </summary>
public sealed class CreateSheetOutput
{
    /// <summary>
    /// Gets the numeric ID assigned to the new sheet tab by Google Sheets.
    /// </summary>
    public int SheetId { get; init; }

    /// <summary>
    /// Gets the title of the newly created sheet tab.
    /// </summary>
    public string SheetTitle { get; init; } = string.Empty;
}
