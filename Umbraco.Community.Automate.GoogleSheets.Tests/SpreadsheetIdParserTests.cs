using Shouldly;
using Umbraco.Community.Automate.GoogleSheets.Actions;
using Xunit;

namespace Umbraco.Community.Automate.GoogleSheets.Tests;

public class SpreadsheetIdParserTests
{
    [Theory]
    [InlineData("1aBcD_efGhIjKlMnOpQrStUvWxYz0123456789abcd", "1aBcD_efGhIjKlMnOpQrStUvWxYz0123456789abcd")]
    [InlineData("  1aBcD_efGhIjKlMnOpQrStUvWxYz0123456789abcd  ", "1aBcD_efGhIjKlMnOpQrStUvWxYz0123456789abcd")]
    [InlineData("https://docs.google.com/spreadsheets/d/1aBcD_efGhIjKlMnOpQrStUvWxYz0123456789abcd/edit#gid=0", "1aBcD_efGhIjKlMnOpQrStUvWxYz0123456789abcd")]
    [InlineData("https://docs.google.com/spreadsheets/d/1aBcD_efGhIjKlMnOpQrStUvWxYz0123456789abcd/edit?usp=sharing", "1aBcD_efGhIjKlMnOpQrStUvWxYz0123456789abcd")]
    public void Parse_returns_the_spreadsheet_id(string input, string expected)
        => SpreadsheetIdParser.Parse(input).ShouldBe(expected);
}
