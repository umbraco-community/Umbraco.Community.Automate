using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace Umbraco.Community.Automate.Mastodon.Composers;

public class MastodonPackageManifestReader : IPackageManifestReader
{
    /// <summary>
    /// Where this package's static web assets are served from, set by
    /// StaticWebAssetBasePath in the csproj.
    /// </summary>
    private const string AppPluginPath = "/App_Plugins/UmbracoCommunityAutomateMastodon";

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var version = typeof(MastodonPackageManifestReader).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        return Task.FromResult<IEnumerable<PackageManifest>>(new[]
        {
            new PackageManifest
            {
                Id = "Umbraco.Community.Automate.Mastodon",
                Name = "Umbraco Community Automate Mastodon",
                Version = version,
                AllowTelemetry = true,

                // Extensions is object[], serialised straight to the backoffice as JSON, so
                // an anonymous object is the C# equivalent of an umbraco-package.json entry.
                // Registering here keeps the package free of a Client build step — there is
                // only one icon and no other front-end code to compile.
                Extensions =
                [
                    new
                    {
                        type = "icons",
                        alias = "UmbracoCommunityAutomateMastodon.Icons",
                        name = "Mastodon Icons",
                        js = $"{AppPluginPath}/icons.js",
                    },
                ]
            }
        });
    }
}
