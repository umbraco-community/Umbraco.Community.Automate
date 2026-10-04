using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Actions;

/// <summary>Settings for <see cref="SendMessageAction"/>.</summary>
public sealed class SendMessageSettings
{
    // SupportsBindings lets the value come from the trigger or an earlier step, e.g.
    // "${ trigger.key } was saved". EditorUiAlias swaps the default text box for the custom
    // editor in Client/src/message-editor, which shows a character count.
    [Field(
        Label = "Message",
        Description = "The message to send. Supports ${ binding } expressions.",
        SupportsBindings = true,
        EditorUiAlias = "UmbracoCommunityAutomateExamplesKitchenSink.PropertyEditorUi.Message",
        SortOrder = 0)]
    public string Message { get; set; } = string.Empty;
}
