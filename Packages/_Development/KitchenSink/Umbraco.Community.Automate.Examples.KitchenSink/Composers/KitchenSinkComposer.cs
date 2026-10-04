using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Composers;

/// <summary>
/// Registers the package's services. Umbraco finds this composer on its own, and Automate finds
/// the connection type, actions and triggers from their attributes, so sites only install the package.
/// </summary>
public class KitchenSinkComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<KitchenSinkClient>();

        // No connection type, action or trigger registration: Automate discovers classes with
        // [ConnectionType], [Action] and [Trigger] attributes on its own. Only services go here.
        // No configuration registration: the default API key reference lives under Automate's
        // shared Umbraco:Automate:Secrets section, which it resolves out of the box. Icons and
        // the message editor are registered by Client/public/umbraco-package.json.
    }
}
