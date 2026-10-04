using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Content;
using Umbraco.Community.Automate.DevTo.Api;

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

        // No connection type, action or trigger registration: Automate discovers classes with
        // [ConnectionType], [Action] and [Trigger] attributes on its own. Only services go here.
    }
}
