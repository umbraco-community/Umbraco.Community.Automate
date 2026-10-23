using Shouldly;
using Umbraco.Community.Automate.DevTo.Content;
using Xunit;

namespace Umbraco.Community.Automate.DevTo.Tests.Content;

public class HtmlToMarkdownConverterTests
{
    [Fact]
    public void Converts_common_rich_text_markup()
    {
        var markdown = HtmlToMarkdownConverter.Convert(
            "<h2>Getting started</h2><p>Install <strong>the package</strong> and <a href=\"https://nuget.org\">read the docs</a>.</p><ul><li>One</li><li>Two</li></ul>");

        markdown.ShouldContain("## Getting started");
        markdown.ShouldContain("**the package**");
        markdown.ShouldContain("[read the docs](https://nuget.org)");
        markdown.ShouldContain("- One");
    }

    [Fact]
    public void Keeps_code_block_language_for_syntax_highlighting()
    {
        var markdown = HtmlToMarkdownConverter.Convert("<pre><code class=\"language-csharp\">var x = 1;</code></pre>");

        markdown.ShouldContain("```csharp");
        markdown.ShouldContain("var x = 1;");
    }

    [Fact]
    public void Turns_video_iframes_into_dev_embed_tags()
    {
        var markdown = HtmlToMarkdownConverter.Convert(
            "<p>Watch:</p><div class=\"umb-embed-holder\"><iframe src=\"https://www.youtube.com/embed/dQw4w9WgXcQ?feature=oembed\" allowfullscreen></iframe></div>");

        markdown.ShouldContain("{% embed https://www.youtube.com/watch?v=dQw4w9WgXcQ %}");
        markdown.ShouldNotContain("iframe");
    }

    [Theory]
    [InlineData("//player.vimeo.com/video/123456", "https://vimeo.com/123456")]
    [InlineData("https://www.youtube-nocookie.com/embed/abc_DEF-1", "https://www.youtube.com/watch?v=abc_DEF-1")]
    [InlineData("https://codepen.io/team/embed/xyz", "https://codepen.io/team/embed/xyz")]
    public void Maps_player_urls_back_to_page_urls(string src, string expected)
        => HtmlToMarkdownConverter.ToEmbedUrl(src).ShouldBe(expected);

    [Fact]
    public void Keeps_the_text_of_unknown_tags()
        => HtmlToMarkdownConverter.Convert("<p>Hello <custom-thing>world</custom-thing></p>").ShouldBe("Hello world");
}
