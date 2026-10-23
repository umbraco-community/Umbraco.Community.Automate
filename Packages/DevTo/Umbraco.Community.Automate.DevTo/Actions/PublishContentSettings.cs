using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.DevTo.Composers;

namespace Umbraco.Community.Automate.DevTo.Actions;

public sealed class PublishContentSettings
{
    [Field(Label = "Body Properties",
        Description = "The properties holding the article body, in order. Markdown, Rich Text, Block List and Block Grid are converted to Markdown.",
        SortOrder = 0,
        EditorUiAlias = DevToPackageManifestReader.BodyPropertiesEditorUiAlias)]
    public string BodyProperties { get; set; } = string.Empty;

    [Field(Label = "Title",
        Description = "The article title. Supports bindings, e.g. `${ trigger.contentName }` or `${ steps.getContent.properties.pageTitle }`. HTML is removed. Blank uses the content name.",
        SortOrder = 1,
        SupportsBindings = true)]
    public string? Title { get; set; } = "${ trigger.contentName }";

    [Field(Label = "Tags",
        Description = "Up to 4 tags. Type them (\"umbraco, dotnet\"), bind a Tags or picker property from a Get Content step (e.g. `${ steps.getContent.properties.tags }`), or mix both. Tags are lowercased and stripped to letters and numbers, as DEV requires.",
        SortOrder = 2,
        SupportsBindings = true)]
    public string? Tags { get; set; }

    [Field(Label = "Publish immediately",
        Description = "Off: new articles are saved as drafts on DEV. Articles already published on DEV stay published either way.",
        SortOrder = 3,
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle")]
    public bool PublishImmediately { get; set; }

    [Field(Label = "Description",
        Description = "Summary for feeds and link previews. Supports bindings, e.g. `${ steps.getContent.properties.metaDescription }`. HTML is removed.",
        SortOrder = 4,
        SupportsBindings = true,
        Group = "Optional")]
    public string? Description { get; set; }

    [Field(Label = "Cover Image",
        Description = "A URL, or bind a media picker from a Get Content step (e.g. `${ steps.getContent.properties.mainImage }`). Relative URLs are made absolute.",
        SortOrder = 5,
        SupportsBindings = true,
        Group = "Optional")]
    public string? CoverImage { get; set; }

    [Field(Label = "Series",
        Description = "Series name. Articles with the same series are linked together on DEV.",
        SortOrder = 6,
        SupportsBindings = true,
        Group = "Optional")]
    public string? Series { get; set; }

    [Field(Label = "Content Key",
        Description = "The content item to post. With a Content Published trigger, leave it as `${ trigger.contentKey }`.",
        SortOrder = 7,
        SupportsBindings = true,
        Group = "Advanced")]
    public string ContentKey { get; set; } = "${ trigger.contentKey }";

    [Field(Label = "Culture",
        Description = "Culture to post for variant content (e.g. en-US). Leave blank for invariant content or the default culture.",
        SortOrder = 8,
        SupportsBindings = true,
        Group = "Advanced")]
    public string? Culture { get; set; }

    [Field(Label = "Site URL",
        Description = "Your site's public base URL, used for the canonical URL, links and images. Leave blank to use the URL Umbraco generates (requires a domain or UmbracoApplicationUrl). Can reference configuration, e.g. $Umbraco:Community:Automate:DevTo:Variables:SiteUrl",
        SortOrder = 9,
        SupportsBindings = true,
        Group = "Advanced")]
    public string? SiteUrl { get; set; }

    [Field(Label = "Canonical URL",
        Description = "Override the canonical URL. Leave blank to use the content's own URL.",
        SortOrder = 10,
        SupportsBindings = true,
        Group = "Advanced")]
    public string? CanonicalUrl { get; set; }

    [Field(Label = "Existing Article ID",
        Description = "Update this DEV article instead of looking it up by canonical URL. Useful if the content's URL has changed.",
        SortOrder = 11,
        SupportsBindings = true,
        Group = "Advanced")]
    public string? ExistingArticleId { get; set; }
}
