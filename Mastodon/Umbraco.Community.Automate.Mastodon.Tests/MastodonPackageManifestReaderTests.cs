using System.Text.Json;
using Umbraco.Community.Automate.Mastodon.Composers;
using Xunit;

namespace Umbraco.Community.Automate.Mastodon.Tests;

public class MastodonPackageManifestReaderTests
{
    /// <summary>
    /// PackageManifest.Extensions is object[] holding anonymous objects, so the backoffice only
    /// ever sees its JSON form — which is what these tests assert against.
    /// </summary>
    private static async Task<JsonElement> GetIconsExtensionAsync()
    {
        var manifests = await new MastodonPackageManifestReader().ReadPackageManifestsAsync();

        var manifest = Assert.Single(manifests);
        var extensions = JsonSerializer.SerializeToElement(manifest.Extensions);

        return Assert.Single(
            extensions.EnumerateArray().Where(e => e.GetProperty("type").GetString() == "icons"));
    }

    [Fact]
    public async Task Registers_an_icons_extension()
    {
        var icons = await GetIconsExtensionAsync();

        Assert.Equal("UmbracoCommunityAutomateMastodon.Icons", icons.GetProperty("alias").GetString());
    }

    [Fact]
    public async Task Icons_extension_points_at_the_static_web_asset_path()
    {
        var icons = await GetIconsExtensionAsync();

        // Must match StaticWebAssetBasePath in the csproj, or the backoffice 404s on the module.
        Assert.Equal(
            "/App_Plugins/UmbracoCommunityAutomateMastodon/icons.js",
            icons.GetProperty("js").GetString());
    }

    [Fact]
    public async Task Icons_module_and_the_svg_it_imports_exist_on_disk()
    {
        var icons = await GetIconsExtensionAsync();
        var fileName = Path.GetFileName(icons.GetProperty("js").GetString()!);

        var wwwroot = MastodonPackagePaths.Wwwroot;

        // Nothing builds these — they are hand-written static web assets, so a rename or a
        // stray delete would otherwise only surface as a broken icon in the backoffice.
        Assert.True(File.Exists(Path.Combine(wwwroot, fileName)), $"Missing wwwroot/{fileName}");
        Assert.True(File.Exists(Path.Combine(wwwroot, "mastodon.icon.js")), "Missing wwwroot/mastodon.icon.js");
    }
}
