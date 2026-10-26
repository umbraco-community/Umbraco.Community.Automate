using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Community.Automate.DevTo.Api;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Content;

namespace Umbraco.Community.Automate.DevTo.Composers;

public class DevToComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient(DevToClient.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(30));
        builder.Services.AddSingleton<DevToClient>();
        builder.Services.AddSingleton<DevToArticlePublisher>();
        builder.Services.AddSingleton<DevToArticleLinks>();
        builder.Services.AddScoped<DevToArticleChecker>();
        builder.Services.AddSingleton<ContentMarkdownConverter>();
        builder.Services.AddSingleton<DevToContentRenderer>();

        builder.Services.AddSingleton<IPackageManifestReader, DevToPackageManifestReader>();

        // No connection type or action registration: Automate discovers classes with
        // [ConnectionType] and [Action] attributes on its own. Only services go here.
    }
}
