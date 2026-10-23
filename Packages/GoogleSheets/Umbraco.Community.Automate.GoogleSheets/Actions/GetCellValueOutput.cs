namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Output produced by the <see cref="GetCellValueAction"/>.
/// </summary>
public sealed class GetCellValueOutput
{
    /// <summary>
    /// Gets the value of the cell as a string, or an empty string if the cell is empty.
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether the cell had no value.
    /// </summary>
    public bool IsEmpty { get; init; }
}
