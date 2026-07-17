using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Telegram.Actions;

/// <summary>
/// Settings for the <see cref="SendMessageAction"/>.
/// </summary>
public sealed class SendMessageSettings
{
    [Field(Label = "Message",
        Description = "The message to send, formatted as Telegram MarkdownV2. Reserved characters " +
            "(_ * [ ] ( ) ~ ` > # + - = | { } . ! \\) must be escaped with a backslash, including in bound values.",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SupportsBindings = true)]
    public string Text { get; set; } = string.Empty;

    [Field(Label = "Chat ID override",
        Description = "Optional. Overrides the connection's default chat ID for this step.",
        SortOrder = 1,
        SupportsBindings = true)]
    public string? ChatId { get; set; }
}
