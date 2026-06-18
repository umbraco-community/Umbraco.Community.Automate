using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Provider1.Actions;

/// <summary>
/// Settings for the MyAction1 action. Properties decorated with [Field] are
/// automatically rendered in the backoffice settings UI.
/// </summary>
public sealed class MyAction1Settings
{
    [Field(Label = "Message", Description = "The message to process.")]
    public string Message { get; set; } = string.Empty;
}
