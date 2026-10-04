using Shouldly;
using Xunit;
using Umbraco.Community.Automate.DevTo.Connections;

namespace Umbraco.Community.Automate.DevTo.Tests;

public class DevToConnectionSettingsValidatorTests
{
    [Fact]
    public void Valid_settings_pass()
        => DevToConnectionSettingsValidator.Validate(new DevToConnectionSettings { ApiKey = "abc" }).ShouldBeNull();

    [Theory]
    [InlineData("", "https://dev.to", "API key is required")]
    [InlineData("$Umbraco:Automate:Secrets:DevTo:ApiKey", "https://dev.to", "could not be resolved")]
    [InlineData("abc", "dev.to", "not a valid instance URL")]
    [InlineData("abc", "ftp://dev.to", "not a valid instance URL")]
    public void Invalid_settings_explain_the_problem(string apiKey, string instanceUrl, string expected)
        => DevToConnectionSettingsValidator.Validate(new DevToConnectionSettings { ApiKey = apiKey, InstanceUrl = instanceUrl })
            .ShouldNotBeNull().ShouldContain(expected);

    [Fact]
    public void Null_settings_fail()
        => DevToConnectionSettingsValidator.Validate(null).ShouldNotBeNull();

    [Fact]
    public void New_connections_default_to_the_configuration_reference()
        => Assert.Equal("$Umbraco:Automate:Secrets:DevTo:ApiKey", new DevToConnectionSettings().ApiKey);
}
