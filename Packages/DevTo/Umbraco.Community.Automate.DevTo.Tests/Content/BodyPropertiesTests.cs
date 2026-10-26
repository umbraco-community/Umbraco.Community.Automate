using Umbraco.Community.Automate.DevTo.Content;
using Xunit;

namespace Umbraco.Community.Automate.DevTo.Tests.Content;

public class BodyPropertiesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_is_empty(string? value)
    {
        var parsed = BodyProperties.Parse(value);

        Assert.Null(parsed.DocumentType);
        Assert.Empty(parsed.Aliases);
    }

    [Fact]
    public void Typed_aliases_split_on_commas_semicolons_and_whitespace_without_duplicates()
    {
        var parsed = BodyProperties.Parse("intro, contentRows;outro\nIntro");

        Assert.Null(parsed.DocumentType);
        Assert.Equal(["intro", "contentRows", "outro"], parsed.Aliases);
    }

    [Fact]
    public void Picked_value_carries_the_document_type()
    {
        var documentType = Guid.NewGuid();

        var parsed = BodyProperties.Parse($$"""{"documentType":"{{documentType}}","aliases":["intro"," contentRows ","intro"]}""");

        Assert.Equal(documentType, parsed.DocumentType);
        Assert.Equal(["intro", "contentRows"], parsed.Aliases);
    }

    [Fact]
    public void Malformed_json_is_empty_rather_than_read_as_aliases()
        => Assert.Empty(BodyProperties.Parse("""{"aliases":["intro"]""").Aliases);
}
