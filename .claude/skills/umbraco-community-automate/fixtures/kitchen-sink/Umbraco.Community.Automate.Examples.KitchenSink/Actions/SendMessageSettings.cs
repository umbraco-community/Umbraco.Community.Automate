using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Actions;

/// <summary>Settings for <see cref="SendMessageAction"/>.</summary>
public sealed class SendMessageSettings
{
    // SupportsBindings lets the value come from the trigger or an earlier step, e.g.
    // "${ trigger.key } was saved".
    [Field(
        Label = "Message",
        Description = "The message to send. Supports ${ binding } expressions.",
        SupportsBindings = true,
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = """[{ "alias": "rows", "value": 4 }]""",
        SortOrder = 0)]
    public string Message { get; set; } = string.Empty;
}
