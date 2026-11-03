using Umbraco.Community.Automate.Examples.KitchenSink.Connections;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Connections;

public class KitchenSinkConnectionSettingsValidatorTests
{
    [Fact]
    public void New_connections_default_to_the_configuration_reference()
        => Assert.Equal("$Umbraco:Automate:Secrets:KitchenSink:ApiKey", new KitchenSinkConnectionSettings().ApiKey);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_api_key_fails(string apiKey)
        => Assert.Equal("An API key is required.", KitchenSinkConnectionSettingsValidator.Validate(new KitchenSinkConnectionSettings { ApiKey = apiKey }));

    [Fact]
    public void Missing_settings_fail()
        => Assert.Equal("An API key is required.", KitchenSinkConnectionSettingsValidator.Validate(null));

    [Fact]
    public void Unresolved_reference_names_the_configuration_key()
    {
        // A reference arriving unresolved (Automate normally resolves it, or reports the missing key, first).
        var error = KitchenSinkConnectionSettingsValidator.Validate(new KitchenSinkConnectionSettings());

        Assert.NotNull(error);
        Assert.Contains("Umbraco:Automate:Secrets:KitchenSink:ApiKey", error);
    }

    [Fact]
    public void Real_key_passes()
        => Assert.Null(KitchenSinkConnectionSettingsValidator.Validate(new KitchenSinkConnectionSettings { ApiKey = "token" }));
}
