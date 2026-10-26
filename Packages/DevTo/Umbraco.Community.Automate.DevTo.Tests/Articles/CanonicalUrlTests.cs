using Shouldly;
using Umbraco.Community.Automate.DevTo.Client;
using Xunit;

namespace Umbraco.Community.Automate.DevTo.Tests.Articles;

public class CanonicalUrlTests
{
    [Theory]
    [InlineData("https://owain.codes/blog/post/", "https://owain.codes/blog/post")]
    [InlineData("https://OWAIN.codes/blog/post", "http://owain.codes/blog/post/")]
    [InlineData("https://owain.codes/Blog/Post", "https://owain.codes/blog/post")]
    public void Treats_scheme_case_and_trailing_slash_as_the_same_page(string left, string right)
        => CanonicalUrl.AreEquivalent(left, right).ShouldBeTrue();

    [Theory]
    [InlineData("https://owain.codes/blog/post", "https://owain.codes/blog/other")]
    [InlineData("https://owain.codes/blog/post", "https://other.codes/blog/post")]
    [InlineData("https://owain.codes/blog/post", null)]
    [InlineData(null, null)]
    [InlineData("/blog/post", "/blog/post")]
    public void Distinguishes_different_or_unusable_urls(string? left, string? right)
        => CanonicalUrl.AreEquivalent(left, right).ShouldBeFalse();
}
