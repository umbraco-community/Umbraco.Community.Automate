using Shouldly;
using Umbraco.Community.Automate.DevTo.Content;
using Xunit;

namespace Umbraco.Community.Automate.DevTo.Tests.Content;

public class UrlResolverTests
{
    private static readonly Uri Site = new("https://owain.codes/");

    [Theory]
    [InlineData("/media/abc/photo.jpg", "https://owain.codes/media/abc/photo.jpg")]
    [InlineData("//cdn.example.com/a.png", "https://cdn.example.com/a.png")]
    [InlineData("https://cdn.example.com/a.png", "https://cdn.example.com/a.png")]
    [InlineData("#section", "#section")]
    [InlineData("mailto:hi@example.com", "mailto:hi@example.com")]
    public void Resolve_makes_site_relative_urls_absolute(string url, string expected)
        => UrlResolver.Resolve(url, Site).ShouldBe(expected);

    [Fact]
    public void AbsolutizeMarkdown_rewrites_links_images_and_html_attributes()
    {
        var markdown = """
            See [my other post](/blog/other/) and ![a photo](/media/1/photo.jpg "Photo").
            <img src="/media/2/raw.png"> and <a href='/contact'>contact</a>, but not [this](https://example.com/x).
            """;

        UrlResolver.AbsolutizeMarkdown(markdown, Site).ShouldBe("""
            See [my other post](https://owain.codes/blog/other/) and ![a photo](https://owain.codes/media/1/photo.jpg "Photo").
            <img src="https://owain.codes/media/2/raw.png"> and <a href='https://owain.codes/contact'>contact</a>, but not [this](https://example.com/x).
            """);
    }

    [Fact]
    public void AbsolutizeMarkdown_leaves_code_samples_untouched()
    {
        var markdown = """
            Use `<a href="/contact">` in your view:

            ```html
            <a href="/contact">Contact</a>
            [link](/relative)
            ```

            Then [read more](/blog/more).
            """;

        UrlResolver.AbsolutizeMarkdown(markdown, Site).ShouldBe("""
            Use `<a href="/contact">` in your view:

            ```html
            <a href="/contact">Contact</a>
            [link](/relative)
            ```

            Then [read more](https://owain.codes/blog/more).
            """);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void AbsolutizeMarkdown_leaves_code_samples_untouched_with_any_line_ending(string newline)
    {
        // Built explicitly so the test doesn't depend on how this file was checked out:
        // Markdown with Windows line endings must keep its fenced code as written too.
        string Lines(params string[] lines) => string.Join(newline, lines);

        var markdown = Lines("```html", "<a href=\"/contact\">Contact</a>", "```", "", "Then [read more](/blog/more).");

        var result = UrlResolver.AbsolutizeMarkdown(markdown, Site);

        Assert.Equal(
            Lines("```html", "<a href=\"/contact\">Contact</a>", "```", "", "Then [read more](https://owain.codes/blog/more)."),
            result);
    }

    [Fact]
    public void AbsolutizeMarkdown_rewrites_reference_style_link_definitions()
        => UrlResolver.AbsolutizeMarkdown("Read the [docs][1].\n\n[1]: /docs/intro \"Intro\"\n  [logo]: /media/logo.png", Site)
            .ShouldBe("Read the [docs][1].\n\n[1]: https://owain.codes/docs/intro \"Intro\"\n  [logo]: https://owain.codes/media/logo.png");
}
