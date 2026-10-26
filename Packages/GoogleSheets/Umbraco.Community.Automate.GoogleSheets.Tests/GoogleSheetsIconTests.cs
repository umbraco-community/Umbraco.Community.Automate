using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.GoogleSheets.Connections;
using Xunit;

namespace Umbraco.Community.Automate.GoogleSheets.Tests;

/// <summary>
/// Icons are registered by hand-written files (Client/public/umbraco-package.json and
/// Client/public/icons/icons.js, copied unchanged into wwwroot by the front-end build) and named
/// again on the C# attributes. A mismatch gives no error, just a missing icon, so these tests link them.
/// </summary>
public class GoogleSheetsIconTests
{
    private static readonly string Public = Path.Combine(ProjectDirectory(), "Client", "public");

    [Fact]
    public void Manifest_registers_icons_from_files_that_exist()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Public, "umbraco-package.json")));
        var icons = Assert.Single(doc.RootElement.GetProperty("extensions").EnumerateArray(), e => e.GetProperty("type").GetString() == "icons");

        // Must match StaticWebAssetBasePath in the csproj, or the backoffice 404s on the module.
        Assert.Equal("/App_Plugins/UmbracoCommunityAutomateGoogleSheets/icons/icons.js", icons.GetProperty("js").GetString());
        Assert.True(File.Exists(Path.Combine(Public, "icons", "icons.js")));
        Assert.True(File.Exists(Path.Combine(Public, "icons", "googlesheets.icon.js")));
    }

    [Fact]
    public void Every_connection_type_and_action_uses_a_registered_icon()
    {
        var registered = Regex.Matches(File.ReadAllText(Path.Combine(Public, "icons", "icons.js")), @"name:\s*""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .ToHashSet();

        var used = typeof(GoogleSheetsConnectionType).Assembly.GetTypes()
            .Select(t => t.GetCustomAttribute<ConnectionTypeAttribute>()?.Icon ?? t.GetCustomAttribute<ActionAttribute>()?.Icon)
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
            var candidate = Path.Combine(dir.FullName, "Umbraco.Community.Automate.GoogleSheets");
            if (Directory.Exists(Path.Combine(candidate, "Client")))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate the Google Sheets project directory.");
    }
}
