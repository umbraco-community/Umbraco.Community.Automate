using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Settings for the <see cref="CreateSheetAction"/>.
/// </summary>
public sealed class CreateSheetSettings
{
    /// <summary>
    /// Gets or sets the Google Sheets spreadsheet URL or ID to add a tab to.
    /// </summary>
    [Field(Label = "Spreadsheet (URL or ID)",
        Description = "Paste the Google Sheet link or its ID.",
        SupportsBindings = true)]
    public string SpreadsheetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title for the new sheet tab.
    /// </summary>
    [Field(Label = "Sheet tab title",
        Description = "The name of the new tab to create, e.g. January 2025.",
        SortOrder = 1,
        SupportsBindings = true)]
    public string SheetTitle { get; set; } = string.Empty;
}
