using Microsoft.Extensions.DependencyInjection;
using Umbraco.Community.Automate.Mastodon.Actions;
using Umbraco.Community.Automate.Mastodon.ConnectionTypes;
using Umbraco.Community.Automate.Mastodon.Factory;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Manifest;

namespace Umbraco.Community.Automate.Mastodon.Composers;

public class MastodonComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<MastodonClientFactory>();

        builder.WithCollectionBuilder<ActionCollectionBuilder>()
            .Add<SendMastodonPostAction>();

        builder.WithCollectionBuilder<ConnectionTypeCollectionBuilder>()
            .Add<MastodonConnectionType>();

        builder.Services.AddSingleton<IPackageManifestReader, MastodonPackageManifestReader>();
    }
}
