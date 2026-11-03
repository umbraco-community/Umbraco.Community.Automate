using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Pushover.Connections;
using Xunit;

namespace Umbraco.Community.Automate.Pushover.Tests;

/// <summary>
/// A wrong icon path or name gives no error, just a blank icon in the backoffice, so these tests
/// check the manifest, the icon files and the attributes agree.
/// </summary>
public class PushoverIconTests
{
    private static readonly Assembly Package = typeof(PushoverConnectionType).Assembly;
    private static readonly string Wwwroot = Path.Combine(ProjectDirectory(), "wwwroot");

    [Fact]
    public void Manifest_registers_icons_from_files_that_exist()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Wwwroot, "umbraco-package.json")));
        var icons = Assert.Single(doc.RootElement.GetProperty("extensions").EnumerateArray(), e => e.GetProperty("type").GetString() == "icons");

        // Must match StaticWebAssetBasePath in the csproj, or the backoffice 404s on the module.
        Assert.Equal("/App_Plugins/UmbracoCommunityAutomatePushover/icons/icons.js", icons.GetProperty("js").GetString());
        Assert.True(File.Exists(Path.Combine(Wwwroot, "icons", "icons.js")));
        Assert.True(File.Exists(Path.Combine(Wwwroot, "icons", "pushover.icon.js")));
    }

    [Fact]
    public void Connection_type_and_action_use_the_registered_icon()
    {
        var registered = Regex.Matches(File.ReadAllText(Path.Combine(Wwwroot, "icons", "icons.js")), @"name:\s*""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .ToHashSet();

        var used = Package.GetTypes()
            .Select(t => t.GetCustomAttribute<ConnectionTypeAttribute>()?.Icon ?? t.GetCustomAttribute<ActionAttribute>()?.Icon)
            .OfType<string>()
            .Distinct()
            .ToList();

        Assert.NotEmpty(used);
        Assert.All(used, icon => Assert.Contains(icon, registered));
    }

    // The static web assets aren't copied to the test output, so read them from the project.
    private static string ProjectDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Umbraco.Community.Automate.Pushover");
            if (Directory.Exists(Path.Combine(candidate, "wwwroot")))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate the Pushover project directory.");
    }
}
