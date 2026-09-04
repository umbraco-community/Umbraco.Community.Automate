using Umbraco.Community.Automate.Mastodon.Settings;
using Xunit;

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
    [InlineData("http://localhost:8080")]
    public void Valid_settings_pass(string instanceUrl)
    {
        var settings = new MastodonSettings { InstanceUrl = instanceUrl, AccessToken = "token" };

        var error = MastodonSettingsValidator.Validate(settings);

        Assert.Null(error);
    }
}
