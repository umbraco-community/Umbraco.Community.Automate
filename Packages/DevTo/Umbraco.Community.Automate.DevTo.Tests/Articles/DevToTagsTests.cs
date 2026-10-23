using Shouldly;
using Umbraco.Community.Automate.DevTo.Articles;
using Xunit;

namespace Umbraco.Community.Automate.DevTo.Tests.Articles;

public class DevToTagsTests
{
    [Fact]
    public void Normalise_lowercases_and_strips_to_alphanumerics()
        => DevToTags.Normalise(["Umbraco CMS", ".NET", "C#", "Héllo-World"]).ShouldBe(["umbracocms", "net", "c", "helloworld"]);

    [Fact]
    public void Normalise_removes_duplicates_and_keeps_the_first_four()
        => DevToTags.Normalise(["umbraco", "Umbraco", "dotnet", "csharp", "", "webdev", "blog"])
            .ShouldBe(["umbraco", "dotnet", "csharp", "webdev"]);

    [Theory]
    [InlineData("umbraco, dotnet", new[] { "umbraco", "dotnet" })]
    [InlineData("#umbraco #dotnet", new[] { "umbraco", "dotnet" })]
    [InlineData("umbraco;dotnet", new[] { "umbraco", "dotnet" })]
    [InlineData("Umbraco CMS", new[] { "Umbraco CMS" })]
    [InlineData("""["umbraco","dotnet"]""", new[] { "umbraco", "dotnet" })]
    [InlineData("""[{"key":"a","name":"Community","url":"/c/"},{"key":"b","name":"Umbraco","url":"/u/"}]""", new[] { "Community", "Umbraco" })]
    [InlineData("""csharp, ["umbraco","dotnet"], webdev""", new[] { "csharp", "umbraco", "dotnet", "webdev" })]
    [InlineData("""{"name":"Single Picker"}""", new[] { "Single Picker" })]
    [InlineData("""["with, comma"]""", new[] { "with, comma" })]
    [InlineData("not [json, really", new[] { "not [json", "really" })]
    [InlineData("", new string[0])]
    public void Parse_reads_text_bound_json_and_mixes_of_both(string input, string[] expected)
        => DevToTags.Parse(input).ShouldBe(expected);

    [Fact]
    public void Parsed_picker_names_normalise_to_dev_tags()
        => DevToTags.Normalise(DevToTags.Parse("""[{"name":"Umbraco CMS"},{"name":".NET"}]""")).ShouldBe(["umbracocms", "net"]);
}
