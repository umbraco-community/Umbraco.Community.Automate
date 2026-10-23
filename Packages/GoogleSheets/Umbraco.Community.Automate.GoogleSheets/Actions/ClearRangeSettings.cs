using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Settings for the <see cref="ClearRangeAction"/>.
/// </summary>
public sealed class ClearRangeSettings
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
    /// Gets or sets an optional A1 range to restrict the clear operation (e.g. A2:Z1000).
    /// Leave blank to clear the entire sheet. To preserve a header row, use A2:Z1000.
    /// </summary>
    [Field(Label = "Range",
        Description = "Optional A1 range to clear, e.g. A2:Z1000. Leave blank to clear the whole sheet.",
        SortOrder = 2,
        SupportsBindings = true)]
    public string Range { get; set; } = string.Empty;
}
