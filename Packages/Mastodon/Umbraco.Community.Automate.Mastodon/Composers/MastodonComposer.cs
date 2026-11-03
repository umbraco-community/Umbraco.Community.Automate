using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Community.Automate.Mastodon.Api;

namespace Umbraco.Community.Automate.Mastodon.Composers;

public class MastodonComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<MastodonClientFactory>();

        builder.Services.AddSingleton<IPackageManifestReader, MastodonPackageManifestReader>();
    }
}
