using Shouldly;
using Umbraco.Community.Automate.Telegram.Client;
using Xunit;

namespace Umbraco.Community.Automate.Telegram.Tests.Client;

public class TelegramMarkdownEscaperTests
{
    [Theory]
    [InlineData("hello world", "hello world")]
    [InlineData("Deploy v1.2.3 failed!", "Deploy v1\\.2\\.3 failed\\!")]
    [InlineData("100% done (ok)", "100% done \\(ok\\)")]
    [InlineData("a_b*c[d]e", "a\\_b\\*c\\[d\\]e")]
    [InlineData(@"back\slash", @"back\\slash")]
    public void Escape_escapes_all_markdownv2_reserved_characters(string input, string expected)
    {
        TelegramMarkdownEscaper.Escape(input).ShouldBe(expected);
    }
}
