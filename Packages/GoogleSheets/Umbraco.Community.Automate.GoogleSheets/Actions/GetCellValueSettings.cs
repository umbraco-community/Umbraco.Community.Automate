using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Settings for the <see cref="GetCellValueAction"/>.
/// </summary>
public sealed class GetCellValueSettings
{
    /// <summary>
    /// Gets or sets the Google Sheets spreadsheet URL or ID.
    /// </summary>
    [Field(Label = "Spreadsheet (URL or ID)",
        Description = "Paste the Google Sheet link or its ID.",
        SupportsBindings = true)]
    public string SpreadsheetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sheet/tab name.
    /// </summary>
    [Field(Label = "Sheet / tab name", Description = "e.g. Sheet1", SortOrder = 1, SupportsBindings = true)]
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cell address in A1 notation (e.g. A1, B5, C12).
    /// </summary>
    [Field(Label = "Cell",
        Description = "Cell address in A1 notation, e.g. A1, B5",
        SortOrder = 2,
        SupportsBindings = true)]
    public string Cell { get; set; } = string.Empty;
}
