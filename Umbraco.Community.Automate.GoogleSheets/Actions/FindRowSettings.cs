using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Settings for the <see cref="FindRowAction"/>.
/// </summary>
public sealed class FindRowSettings
{
    /// <summary>
    /// Gets or sets the Google Sheets spreadsheet URL or ID.
    /// </summary>
    [Field(Label = "Spreadsheet (URL or ID)",
        Description = "Paste the Google Sheet link or its ID.",
        SupportsBindings = true)]
    public string SpreadsheetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sheet/tab name to search.
    /// </summary>
    [Field(Label = "Sheet / tab name", Description = "e.g. Sheet1", SortOrder = 1, SupportsBindings = true)]
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column letter to search in (e.g. A, B, C).
    /// </summary>
    [Field(Label = "Search column",
        Description = "Column letter to search in, e.g. A",
        SortOrder = 2,
        SupportsBindings = true)]
    public string SearchColumn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value to search for.
    /// </summary>
    [Field(Label = "Search value",
        Description = "The value to look for in the search column.",
        SortOrder = 3,
        SupportsBindings = true)]
    public string SearchValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the match mode. Stored as a string so the dropdown picker
    /// round-trips cleanly — parsed into <see cref="FindRowMatchMode"/> at execute time.
    /// </summary>
    [Field(
        Label = "Match mode",
        Description = "How to compare the search value against each cell. Exact (default) requires the whole value to match.",
        SortOrder = 4,
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = """[{ "alias": "items", "value": ["Exact", "Contains", "StartsWith", "EndsWith"] }]""")]
    public string MatchMode { get; set; } = nameof(FindRowMatchMode.Exact);

    /// <summary>
    /// Gets or sets a value indicating whether matching is case-sensitive.
    /// </summary>
    [Field(
        Label = "Case sensitive",
        Description = "When off, 'Bob' matches 'bob'. Off by default.",
        SortOrder = 5,
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle")]
    public bool CaseSensitive { get; set; } = false;
}
