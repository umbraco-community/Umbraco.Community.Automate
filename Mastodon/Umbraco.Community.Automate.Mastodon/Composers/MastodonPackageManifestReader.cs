using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace Umbraco.Community.Automate.Mastodon.Composers;

public class MastodonPackageManifestReader : IPackageManifestReader
{
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
                Extensions = []
            }
        });
    }
}
