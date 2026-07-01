using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Settings for the <see cref="DeleteRowAction"/>.
/// </summary>
public sealed class DeleteRowSettings
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
    /// Gets or sets the column letter to search in (e.g. A, B, C).
    /// </summary>
    [Field(Label = "Lookup column",
        Description = "Column letter to search in, e.g. A",
        SortOrder = 2,
        SupportsBindings = true)]
    public string LookupColumn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value to match in the lookup column.
    /// </summary>
    [Field(Label = "Lookup value",
        Description = "The value to search for. The first matching row will be deleted.",
        SortOrder = 3,
        SupportsBindings = true)]
    public string LookupValue { get; set; } = string.Empty;
}
