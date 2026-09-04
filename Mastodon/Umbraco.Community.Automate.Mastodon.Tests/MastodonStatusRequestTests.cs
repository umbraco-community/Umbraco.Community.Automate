using System.Text.Json;
using Umbraco.Community.Automate.Mastodon.Actions;
using Umbraco.Community.Automate.Mastodon.Settings;
using Xunit;

namespace Umbraco.Community.Automate.Mastodon.Tests;

public class MastodonStatusRequestTests
{
    [Fact]
    public void Content_is_trimmed()
    {
        var request = MastodonStatusRequest.FromSettings(new MastodonPostSettings { Content = "  Hello world  " });

        Assert.Equal("Hello world", request.Status);
    }

    [Fact]
    public void Post_url_is_appended_after_a_blank_line()
    {
        var request = MastodonStatusRequest.FromSettings(new MastodonPostSettings
        {
            Content = "New blog post!",
            PostUrl = "https://owain.codes/post",
        });

        Assert.Equal("New blog post!\n\nhttps://owain.codes/post", request.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_post_url_is_not_appended(string? postUrl)
    {
        var request = MastodonStatusRequest.FromSettings(new MastodonPostSettings
        {
            Content = "New blog post!",
            PostUrl = postUrl,
        });

        Assert.Equal("New blog post!", request.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_visibility_defaults_to_public(string visibility)
    {
        var request = MastodonStatusRequest.FromSettings(new MastodonPostSettings
        {
            Content = "Hello",
            Visibility = visibility,
        });

        Assert.Equal("public", request.Visibility);
    }

    [Fact]
    public void Explicit_visibility_is_kept()
    {
        var request = MastodonStatusRequest.FromSettings(new MastodonPostSettings
        {
            Content = "Hello",
            Visibility = "unlisted",
        });

        Assert.Equal("unlisted", request.Visibility);
    }

    [Fact]
    public void Sensitive_flag_and_spoiler_text_are_mapped()
    {
        var request = MastodonStatusRequest.FromSettings(new MastodonPostSettings
        {
            Content = "Hello",
            IsSensitive = true,
            SpoilerText = "CW: example",
        });

        Assert.True(request.Sensitive);
        Assert.Equal("CW: example", request.SpoilerText);
    }

    [Fact]
    public void Serializes_using_mastodon_api_field_names()
    {
        var request = MastodonStatusRequest.FromSettings(new MastodonPostSettings
        {
            Content = "Hello",
            IsSensitive = true,
            SpoilerText = "CW",
        });

        var json = JsonSerializer.Serialize(request);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("Hello", root.GetProperty("status").GetString());
        Assert.Equal("public", root.GetProperty("visibility").GetString());
        Assert.True(root.GetProperty("sensitive").GetBoolean());
        Assert.Equal("CW", root.GetProperty("spoiler_text").GetString());
    }
}
