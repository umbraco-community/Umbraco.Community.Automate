using Moq;
using Shouldly;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Strings;
using Umbraco.Community.Automate.DevTo.Content;
using Xunit;
using static Umbraco.Community.Automate.DevTo.Tests.Helpers.PublishedContentMocks;

namespace Umbraco.Community.Automate.DevTo.Tests.Content;

public class ContentMarkdownConverterTests
{
    private static readonly Uri Site = new("https://owain.codes/");

    private static ContentMarkdownConverter CreateConverter(params IDevToBlockConverter[] blockConverters)
    {
        var urlProvider = new Mock<IPublishedUrlProvider>();
        urlProvider
            .Setup(p => p.GetMediaUrl(It.IsAny<IPublishedContent>(), It.IsAny<UrlMode>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<Uri?>()))
            .Returns<IPublishedContent, UrlMode, string?, string, Uri?>((media, _, _, _, _) => $"/media/{media.Name}.jpg");

        return new ContentMarkdownConverter(urlProvider.Object, blockConverters);
    }

    private static BlockListModel BlockList(params IPublishedElement[] elements)
        => new(elements.Select(e => new BlockListItem(Guid.NewGuid(), e, null, null)).ToList());

    [Fact]
    public void Markdown_editor_uses_the_source_markdown_not_the_rendered_html()
    {
        var content = Document(Property("body", "Umbraco.MarkdownEditor",
            value: new HtmlEncodedString("<h1>Hello</h1>"), sourceValue: "# Hello\n\nWorld"));

        CreateConverter().Convert(content.Object, ["body"], null, Site).ShouldBe("# Hello\n\nWorld");
    }

    [Fact]
    public void Rich_text_is_converted_from_html_with_absolute_urls()
    {
        var content = Document(Property("body", "Umbraco.RichText",
            new HtmlEncodedString("<p>See <a href=\"/blog/other/\">this</a> <img src=\"/media/x.png\" alt=\"x\"></p>")));

        var markdown = CreateConverter().Convert(content.Object, ["body"], null, Site);

        markdown.ShouldContain("[this](https://owain.codes/blog/other/)");
        markdown.ShouldContain("https://owain.codes/media/x.png");
    }

    [Fact]
    public void Block_list_blocks_are_converted_in_order_with_headings_text_and_images()
    {
        var blocks = BlockList(
            Element(TextBox("heading", "Introduction"), TextArea("text", "Some words.")),
            Element(Property("image", "Umbraco.MediaPicker3", Media("hero"))));

        var markdown = CreateConverter().Convert(Document(Property("blocks", "Umbraco.BlockList", blocks)).Object, ["blocks"], null, Site);

        markdown.ShouldBe("## Introduction\n\nSome words.\n\n![hero](https://owain.codes/media/hero.jpg)");
    }

    [Fact]
    public void Code_blocks_become_fenced_code_with_their_language()
    {
        var blocks = BlockList(Element(
            TextBox("title", "Program.cs"),
            TextArea("code", "var x = 1;\nConsole.WriteLine(x);"),
            TextBox("language", "C#")));

        var markdown = CreateConverter().Convert(Document(Property("blocks", "Umbraco.BlockList", blocks)).Object, ["blocks"], null, Site);

        markdown.ShouldBe("## Program.cs\n\n```c#\nvar x = 1;\nConsole.WriteLine(x);\n```");
    }

    [Fact]
    public void Code_containing_backticks_gets_a_longer_fence()
        => ContentMarkdownConverter.Fence("```js\nfoo\n```", "markdown").ShouldStartWith("````markdown\n");

    [Fact]
    public void Block_grid_includes_nested_areas()
    {
        var inner = new BlockGridItem(Guid.NewGuid(), Element(TextArea("text", "Inside an area")), null, null);
        var outer = new BlockGridItem(Guid.NewGuid(), Element(TextBox("headline", "Two columns")), null, null)
        {
            Areas = [new BlockGridArea([inner], "left", 6, 6)],
        };
        var grid = new BlockGridModel([outer], 12);

        var markdown = CreateConverter().Convert(Document(Property("grid", "Umbraco.BlockGrid", grid)).Object, ["grid"], null, Site);

        markdown.ShouldBe("## Two columns\n\nInside an area");
    }

    [Fact]
    public void Nested_block_lists_and_rich_text_inside_blocks_are_converted()
    {
        var nested = BlockList(Element(Property("text", "Umbraco.RichText", new HtmlEncodedString("<p><em>Nested</em> rich text</p>"))));
        var blocks = BlockList(Element(Property("items", "Umbraco.BlockList", nested)));

        var markdown = CreateConverter().Convert(Document(Property("blocks", "Umbraco.BlockList", blocks)).Object, ["blocks"], null, Site);

        markdown.ShouldBe("*Nested* rich text");
    }

    [Fact]
    public void Multiple_body_properties_are_joined_in_the_order_given()
    {
        var content = Document(
            Property("body", "Umbraco.MarkdownEditor", null, "Body text"),
            TextArea("intro", "Intro text"));

        CreateConverter().Convert(content.Object, ["intro", "body"], null, Site).ShouldBe("Intro text\n\nBody text");
    }

    [Fact]
    public void Properties_with_no_article_representation_are_left_out()
    {
        var blocks = BlockList(Element(
            TextArea("text", "Kept"),
            Property("showBorder", "Umbraco.TrueFalse", true),
            Property("colour", "Umbraco.ColorPicker", "#ff0000")));

        CreateConverter().Convert(Document(Property("blocks", "Umbraco.BlockList", blocks)).Object, ["blocks"], null, Site)
            .ShouldBe("Kept");
    }

    [Fact]
    public void Links_are_written_as_markdown_links()
    {
        var blocks = BlockList(Element(Property("links", "Umbraco.MultiUrlPicker",
            new[] { new Link { Name = "Docs", Url = "https://docs.umbraco.com" }, new Link { Name = "Contact", Url = "/contact" } })));

        CreateConverter().Convert(Document(Property("blocks", "Umbraco.BlockList", blocks)).Object, ["blocks"], null, Site)
            .ShouldBe("[Docs](https://docs.umbraco.com)\n[Contact](https://owain.codes/contact)");
    }

    [Fact]
    public void Registered_block_converters_run_before_the_built_in_conversion()
    {
        var callout = Element("callout", TextArea("text", "Careful!"));
        var paragraph = Element("paragraph", TextArea("text", "Normal"));
        var blocks = BlockList(callout, paragraph);

        var markdown = CreateConverter(new CalloutConverter())
            .Convert(Document(Property("blocks", "Umbraco.BlockList", blocks)).Object, ["blocks"], null, Site);

        markdown.ShouldBe("> **Note:** Careful!\n\nNormal");
    }

    [Fact]
    public void A_block_converter_can_drop_a_block_by_returning_an_empty_string()
    {
        var blocks = BlockList(Element("newsletterSignup", TextBox("heading", "Subscribe!")), Element("paragraph", TextArea("text", "Kept")));

        CreateConverter(new DropNewsletterConverter())
            .Convert(Document(Property("blocks", "Umbraco.BlockList", blocks)).Object, ["blocks"], null, Site)
            .ShouldBe("Kept");
    }

    [Fact]
    public void Image_alt_text_comes_from_an_invariant_alt_text_property_on_variant_content()
    {
        var altText = new Mock<IPublishedProperty>();
        var altTextType = new Mock<IPublishedPropertyType>();
        altTextType.SetupGet(t => t.Variations).Returns(ContentVariation.Nothing);
        altText.SetupGet(p => p.Alias).Returns("altText");
        altText.SetupGet(p => p.PropertyType).Returns(altTextType.Object);
        altText.Setup(p => p.GetValue(null, null)).Returns("A crowd at Codegarden");

        var blocks = BlockList(Element(Property("image", "Umbraco.MediaPicker3", Media("crowd", altText.Object))));

        CreateConverter().Convert(Document(Property("blocks", "Umbraco.BlockList", blocks)).Object, ["blocks"], "da-DK", Site)
            .ShouldBe("![A crowd at Codegarden](https://owain.codes/media/crowd.jpg)");
    }

    [Fact]
    public void Invariant_properties_are_read_without_a_culture()
    {
        var property = new Mock<IPublishedProperty>();
        var propertyType = new Mock<IPublishedPropertyType>();
        propertyType.SetupGet(t => t.EditorAlias).Returns("Umbraco.TextArea");
        propertyType.SetupGet(t => t.Variations).Returns(ContentVariation.Nothing);
        property.SetupGet(p => p.Alias).Returns("intro");
        property.SetupGet(p => p.PropertyType).Returns(propertyType.Object);
        property.Setup(p => p.GetValue(null, null)).Returns("Invariant value");

        CreateConverter().Convert(Document(property.Object).Object, ["intro"], "da-DK", Site).ShouldBe("Invariant value");
    }

    private sealed class CalloutConverter : IDevToBlockConverter
    {
        public string? Convert(IPublishedElement content, IPublishedElement? settings, DevToConversionContext context)
            => content.ContentType.Alias == "callout"
                ? $"> **Note:** {context.ConvertProperty(content, "text")}"
                : null;
    }

    private sealed class DropNewsletterConverter : IDevToBlockConverter
    {
        public string? Convert(IPublishedElement content, IPublishedElement? settings, DevToConversionContext context)
            => content.ContentType.Alias == "newsletterSignup" ? string.Empty : null;
    }
}
