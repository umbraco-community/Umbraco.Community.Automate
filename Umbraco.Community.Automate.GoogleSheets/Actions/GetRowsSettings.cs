using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Settings for the <see cref="GetRowsAction"/>.
/// </summary>
public sealed class GetRowsSettings
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
    /// Gets or sets an optional A1 range to restrict the read (e.g. A2:D50).
    /// Leave blank to read the entire sheet.
    /// </summary>
    [Field(Label = "Range",
        Description = "Optional A1 range to read, e.g. A2:D50. Leave blank to read the whole sheet.",
        SortOrder = 2,
        SupportsBindings = true)]
    public string Range { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the first row should be treated as a header row.
    /// When true, the header row is excluded from <see cref="GetRowsOutput.Rows"/> and
    /// surfaced separately as <see cref="GetRowsOutput.Headers"/>.
    /// </summary>
    [Field(Label = "First row is a header",
        Description = "When enabled (default), the first row is returned as Headers and excluded from Rows.",
        SortOrder = 3,
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle")]
    public bool HasHeaderRow { get; set; } = true;
}
