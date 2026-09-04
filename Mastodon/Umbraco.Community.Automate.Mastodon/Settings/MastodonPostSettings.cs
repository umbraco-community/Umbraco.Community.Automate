using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Mastodon.Settings;

public sealed class MastodonPostSettings
{
    [Field(Label = "Content",
     Description = "The content of the post. Supports data bindings using ${ binding } syntax.",
     SortOrder = 0,
     SupportsBindings = true,
     EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
     EditorConfig = """[{ "alias": "rows", "value": 4 }]""")]

    public string Content { get; set; } = string.Empty;

    [Field(Label = "Visibility", 
     Description = "Who can see this post?", 
     SortOrder = 1,
     EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
     EditorConfig = """[{ "alias": "items", "value": ["public", "unlisted", "private", "direct"] }]""")]
    public string Visibility { get; set; } = "public";

    [Field(Label = "Post URL", 
     Description = "Optional URL to append to the post.", 
     SortOrder = 2, 
     SupportsBindings = true)]
    public string? PostUrl { get; set; }

    [Field(Label = "Sensitive", 
     Description = "Mark this post as containing sensitive content.", 
     SortOrder = 3)]
    public bool IsSensitive { get; set; }

    [Field(Label = "Spoiler Text", 
     Description = "Optional content warning text shown before the post.", 
     SortOrder = 4, 
     SupportsBindings = true,
     EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
     EditorConfig = """[{ "alias": "rows", "value": 2 }]""")]
    public string? SpoilerText { get; set; }
}
