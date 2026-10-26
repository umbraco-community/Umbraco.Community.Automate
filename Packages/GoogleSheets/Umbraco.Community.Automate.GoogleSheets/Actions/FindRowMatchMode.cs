namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// How <see cref="FindRowAction"/> compares the search value against each cell.
/// </summary>
public enum FindRowMatchMode
{
    /// <summary>The cell value must equal the search value.</summary>
    Exact = 0,

    /// <summary>The cell value must contain the search value as a substring.</summary>
    Contains = 1,

    /// <summary>The cell value must start with the search value.</summary>
    StartsWith = 2,

    /// <summary>The cell value must end with the search value.</summary>
    EndsWith = 3,
}
