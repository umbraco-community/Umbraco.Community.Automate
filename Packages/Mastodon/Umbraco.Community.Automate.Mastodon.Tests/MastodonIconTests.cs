using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Mastodon.Actions;
using Umbraco.Community.Automate.Mastodon.Connections;
using Xunit;

namespace Umbraco.Community.Automate.Mastodon.Tests;

/// <summary>
/// A wrong icon path or name gives no error, just a blank icon in the backoffice, so these tests
/// check the manifest, the icon files and the attributes agree.
/// </summary>
public class MastodonIconTests
{
    private static JsonElement IconsExtension()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(MastodonPackagePaths.Wwwroot, "umbraco-package.json")));
        return Assert.Single(doc.RootElement.GetProperty("extensions").EnumerateArray(),
            e => e.GetProperty("type").GetString() == "icons").Clone();
    }

    [Fact]
    public void Manifest_registers_icons_from_files_that_exist()
    {
        Assert.Equal("UmbracoCommunityAutomateMastodon.Icons", IconsExtension().GetProperty("alias").GetString());

        // Must match StaticWebAssetBasePath in the csproj, or the backoffice 404s on the module.
        Assert.Equal("/App_Plugins/UmbracoCommunityAutomateMastodon/icons/icons.js", IconsExtension().GetProperty("js").GetString());

        Assert.True(File.Exists(Path.Combine(MastodonPackagePaths.Wwwroot, "icons", "icons.js")));
        Assert.True(File.Exists(Path.Combine(MastodonPackagePaths.Wwwroot, "icons", "mastodon.icon.js")));
    }

    private static string[] RegisteredIconNames()
    {
        var icons = File.ReadAllText(Path.Combine(MastodonPackagePaths.Wwwroot, "icons", "icons.js"));

        return Regex.Matches(icons, @"name:\s*""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .ToArray();
    }

    [Fact]
    public void Connection_type_uses_an_icon_that_icons_js_registers()
    {
        var icon = typeof(MastodonConnectionType).GetCustomAttribute<ConnectionTypeAttribute>()?.Icon;

        Assert.Contains(icon, RegisteredIconNames());
    }

    [Fact]
    public void Send_post_action_uses_an_icon_that_icons_js_registers()
    {
        var icon = typeof(SendMastodonPostAction).GetCustomAttribute<ActionAttribute>()?.Icon;

        Assert.Contains(icon, RegisteredIconNames());
    }
}
