using System.Reflection;
using System.Text.RegularExpressions;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Mastodon.Actions;
using Umbraco.Community.Automate.Mastodon.ConnectionTypes;
using Xunit;

namespace Umbraco.Community.Automate.Mastodon.Tests;

/// <summary>
/// The icon name is declared twice — in the C# attributes and in wwwroot/icons.js — with
/// nothing linking them. Renaming one and not the other leaves no build error and no failing
/// request: the backoffice just silently renders no icon. These tests are that link.
/// </summary>
public class MastodonIconTests
{
    private static string[] RegisteredIconNames()
    {
        var icons = File.ReadAllText(Path.Combine(MastodonPackagePaths.Wwwroot, "icons.js"));

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
