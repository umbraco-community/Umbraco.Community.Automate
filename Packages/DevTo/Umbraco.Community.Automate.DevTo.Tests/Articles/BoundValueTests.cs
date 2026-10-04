using Shouldly;
using Umbraco.Community.Automate.DevTo.Articles;
using Xunit;

namespace Umbraco.Community.Automate.DevTo.Tests.Articles;

public class BoundValueTests
{
    [Theory]
    [InlineData("A plain title", "A plain title")]
    [InlineData("<p>Rich <strong>text</strong>\n &amp; more</p>", "Rich text & more")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void PlainText_strips_html_and_whitespace(string? value, string? expected)
        => BoundValue.PlainText(value).ShouldBe(expected);

    [Theory]
    [InlineData("https://cdn.example.com/a.png", "https://cdn.example.com/a.png")]
    [InlineData("/media/a.png", "/media/a.png")]
    [InlineData("""{"key":"k","name":"hero","url":"/media/hero.jpg"}""", "/media/hero.jpg")]
    [InlineData("""[{"key":"k","name":"hero","url":"/media/hero.jpg"},{"url":"/media/second.jpg"}]""", "/media/hero.jpg")]
    [InlineData("""["/media/from-string-array.jpg"]""", "/media/from-string-array.jpg")]
    [InlineData("[]", null)]
    [InlineData("", null)]
    public void Url_reads_plain_urls_and_bound_media_json(string? value, string? expected)
        => BoundValue.Url(value).ShouldBe(expected);
}
