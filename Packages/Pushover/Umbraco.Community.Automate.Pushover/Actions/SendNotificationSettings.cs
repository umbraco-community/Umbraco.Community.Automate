using System.ComponentModel.DataAnnotations;
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Pushover.Actions;

/// <summary>Settings for <see cref="SendNotificationAction"/>, shown as fields in the automation editor.</summary>
public sealed class SendNotificationSettings
{
    [Field(
        Label = "Title",
        Description = "Optional. Shown above the message. Supports ${ binding } expressions.",
        SupportsBindings = true,
        SortOrder = 0)]
    public string? Title { get; set; }

    [Required(ErrorMessage = "A message is required.")]
    [Field(
        Label = "Message",
        Description = "The notification's text. Supports ${ binding } expressions.",
        SupportsBindings = true,
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = """[{ "alias": "rows", "value": 4 }]""",
        SortOrder = 1)]
    public string Message { get; set; } = string.Empty;

    // Pushover's built-in sounds; see https://pushover.net/api#sounds.
    [Field(
        Label = "Sound",
        Description = "The sound the notification plays on the device. See https://pushover.net/api#sounds.",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = """[{ "alias": "items", "value": ["pushover", "bike", "bugle", "cashregister", "classical", "cosmic", "falling", "gamelan", "incoming", "intermission", "magic", "mechanical", "pianobar", "siren", "spacealarm", "tugboat", "alien", "climb", "persistent", "echo", "updown", "vibrate", "none"] }]""",
        SortOrder = 2)]
    public string? Sound { get; set; } = "pushover";

    [Field(
        Label = "Custom sound",
        Description = "Optional. The name of a sound you've uploaded to your Pushover account. Used instead of Sound when set. Supports ${ binding } expressions.",
        SupportsBindings = true,
        SortOrder = 3)]
    public string? CustomSound { get; set; }

    [Field(
        Label = "URL",
        Description = "Optional. A link people can open from the notification: a web address, or mailto:, tel: and other URI schemes. Supports ${ binding } expressions.",
        SupportsBindings = true,
        SortOrder = 4)]
    public string? Url { get; set; }

    [Field(
        Label = "URL title",
        Description = "Optional. The text shown for the link. Without it, the URL itself is shown. Supports ${ binding } expressions.",
        SupportsBindings = true,
        SortOrder = 5)]
    public string? UrlTitle { get; set; }

    [Field(
        Label = "Priority",
        Description = "How urgently the notification is shown. Max repeats it until someone acknowledges it, using the connection's Retry and Expire settings.",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = """[{ "alias": "items", "value": ["Min", "Low", "Default", "High", "Max"] }]""",
        SortOrder = 6)]
    public string Priority { get; set; } = "Default";
}
