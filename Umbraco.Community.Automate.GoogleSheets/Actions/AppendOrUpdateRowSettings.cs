using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Settings for the <see cref="AppendOrUpdateRowAction"/>.
/// </summary>
public sealed class AppendOrUpdateRowSettings
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
    /// Gets or sets the column letter used as the unique key to detect existing rows (e.g. A).
    /// </summary>
    [Field(Label = "Key column",
        Description = "Column letter used to detect duplicates, e.g. A. " +
                      "If a row already has this value in this column it will be updated; otherwise a new row is appended.",
        SortOrder = 2,
        SupportsBindings = true)]
    public string KeyColumn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordered column values to write.
    /// </summary>
    [Field(Label = "Column values",
        Description = "Values to write, one per column in order (A, B, C…). Bindings supported.",
        SortOrder = 3,
        EditorUiAlias = "UmbracoCommunityAutomateGoogleSheets.PropertyEditorUi.ColumnList",
        SupportsBindings = true)]
    public List<string> Columns { get; set; } = [];
}
