using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Settings for the <see cref="CreateSpreadsheetAction"/>.
/// </summary>
public sealed class CreateSpreadsheetSettings
{
    /// <summary>
    /// Gets or sets the title for the new spreadsheet.
    /// </summary>
    [Field(Label = "Title",
        Description = "The title of the new spreadsheet.",
        SupportsBindings = true)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional comma-separated list of sheet tab names to create.
    /// When blank, Google creates a single default sheet named "Sheet1".
    /// </summary>
    [Field(Label = "Sheet tab names",
        Description = "Optional comma-separated list of tab names, e.g. Sheet1, Data, Summary. " +
                      "Leave blank to use Google's default (Sheet1).",
        SortOrder = 1,
        SupportsBindings = true)]
    public string SheetNames { get; set; } = string.Empty;
}
