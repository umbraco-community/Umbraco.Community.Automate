using Xunit;
using Umbraco.Community.Automate.Mastodon.Connections;

namespace Umbraco.Community.Automate.Mastodon.Tests;

public class MastodonSettingsValidatorTests
{
    [Fact]
    public void Null_settings_fail_with_instance_url_required()
    {
        var error = MastodonSettingsValidator.Validate(null);

        Assert.Equal("Instance URL is required.", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_instance_url_fails(string instanceUrl)
    {
        var settings = new MastodonSettings { InstanceUrl = instanceUrl, AccessToken = "token" };

        var error = MastodonSettingsValidator.Validate(settings);

        Assert.Equal("Instance URL is required.", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_access_token_fails(string accessToken)
    {
        var settings = new MastodonSettings { InstanceUrl = "https://mastodon.social", AccessToken = accessToken };

        var error = MastodonSettingsValidator.Validate(settings);

        Assert.Equal("Access token is required.", error);
    }

    [Theory]
    [InlineData("mastodon.social")]
    [InlineData("ftp://mastodon.social")]
    [InlineData("not a url")]
    public void Instance_url_must_be_an_absolute_http_url(string instanceUrl)
    {
        var settings = new MastodonSettings { InstanceUrl = instanceUrl, AccessToken = "token" };

        var error = MastodonSettingsValidator.Validate(settings);

        Assert.Equal(
            "Instance URL must be an absolute URL starting with http:// or https:// (e.g. https://mastodon.social).",
            error);
    }

    [Theory]
    [InlineData("https://mastodon.social")]
    [InlineData("https://mastodon.social/")]
    public void Valid_settings_pass(string instanceUrl)
    {
        var settings = new MastodonSettings { InstanceUrl = instanceUrl, AccessToken = "token" };

        var error = MastodonSettingsValidator.Validate(settings);

        Assert.Null(error);
    }

    [Fact]
    public void Http_instance_url_is_allowed_for_local_or_self_hosted_instances()
    {
        var settings = new MastodonSettings { InstanceUrl = "http://localhost:8080", AccessToken = "token" };

        var error = MastodonSettingsValidator.Validate(settings);

        Assert.Null(error);
    }

    [Fact]
    public void New_connections_default_to_the_configuration_references()
    {
        var settings = new MastodonSettings();

        Assert.Equal("$Umbraco:Automate:Variables:Mastodon:InstanceUrl", settings.InstanceUrl);
        Assert.Equal("$Umbraco:Automate:Secrets:Mastodon:AccessToken", settings.AccessToken);
    }

    [Fact]
    public void Unresolved_instance_url_reference_explains_where_to_put_the_url()
    {
        // A reference arriving unresolved (Automate normally resolves it, or reports the missing key, first).
        var error = MastodonSettingsValidator.Validate(new MastodonSettings { AccessToken = "token" });

        Assert.NotNull(error);
        Assert.Contains("could not be resolved", error);
        Assert.Contains("Umbraco:Automate:Variables:Mastodon:InstanceUrl", error);
    }

    [Fact]
    public void Unresolved_access_token_reference_explains_where_to_put_the_token()
    {
        var error = MastodonSettingsValidator.Validate(new MastodonSettings { InstanceUrl = "https://mastodon.social" });

        Assert.NotNull(error);
        Assert.Contains("Umbraco:Automate:Secrets:Mastodon:AccessToken", error);
    }
}
