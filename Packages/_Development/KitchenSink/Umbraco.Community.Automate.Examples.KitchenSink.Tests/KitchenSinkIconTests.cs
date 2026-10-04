using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Community.Automate.Examples.KitchenSink.Connections;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests;

/// <summary>
/// Icons are plain files registered by Client/public/umbraco-package.json (copied unchanged into
/// wwwroot by the front-end build) and named again on the C# attributes. A mismatch gives no
/// error, just a missing icon in the backoffice, so these tests link them.
/// </summary>
public class KitchenSinkIconTests
{
    private static readonly string Public = Path.Combine(ProjectDirectory(), "Client", "public");

    [Fact]
    public void Manifest_registers_icons_from_files_that_exist()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Public, "umbraco-package.json")));
        var icons = Assert.Single(doc.RootElement.GetProperty("extensions").EnumerateArray(), e => e.GetProperty("type").GetString() == "icons");

        // Must match StaticWebAssetBasePath in the csproj, or the backoffice 404s on the module.
        Assert.Equal("/App_Plugins/UmbracoCommunityAutomateExamplesKitchenSink/icons/icons.js", icons.GetProperty("js").GetString());
        Assert.True(File.Exists(Path.Combine(Public, "icons", "icons.js")));
        Assert.True(File.Exists(Path.Combine(Public, "icons", "kitchensink.icon.js")));
    }

    [Fact]
    public void Every_connection_type_action_and_trigger_uses_a_registered_icon()
    {
        var registered = Regex.Matches(File.ReadAllText(Path.Combine(Public, "icons", "icons.js")), @"name:\s*""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .ToHashSet();

        var used = typeof(KitchenSinkConnectionType).Assembly.GetTypes()
            .Select(t => t.GetCustomAttribute<ConnectionTypeAttribute>()?.Icon
                         ?? t.GetCustomAttribute<ActionAttribute>()?.Icon
                         ?? t.GetCustomAttribute<TriggerAttribute>()?.Icon)
            .OfType<string>()
            .Distinct()
            .ToList();

        Assert.NotEmpty(used);
        Assert.All(used, icon => Assert.Contains(icon, registered));
    }

    // The front-end source isn't copied to the test output, so read it from the project.
    private static string ProjectDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Umbraco.Community.Automate.Examples.KitchenSink");
            if (Directory.Exists(Path.Combine(candidate, "Client")))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate the Kitchen Sink example project directory.");
    }
}
