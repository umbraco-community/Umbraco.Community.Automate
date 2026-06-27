using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Settings for the <see cref="AppendRowAction"/>.
/// </summary>
public sealed class AppendRowSettings
{
    /// <summary>
    /// Gets or sets the Google Sheets spreadsheet URL or ID.
    /// </summary>
    [Field(Label = "Spreadsheet (URL or ID)",
        Description = "Paste the Google Sheet link or its ID.",
        SupportsBindings = true)]
    public string SpreadsheetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sheet/tab name to append the row to.
    /// </summary>
    [Field(Label = "Sheet / tab name", Description = "e.g. Sheet1", SortOrder = 1, SupportsBindings = true)]
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordered column values to append as a row.
    /// </summary>
    [Field(Label = "Column values",
        Description = "One value per column, in order (A, B, C…). Bindings supported.",
        SortOrder = 2,
        EditorUiAlias = "UmbracoCommunityAutomateGoogleSheets.PropertyEditorUi.ColumnList",
        SupportsBindings = true)]
    public List<string> Columns { get; set; } = [];
}
