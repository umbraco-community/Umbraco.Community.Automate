using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Automate.Pushover.Api;

namespace Umbraco.Community.Automate.Pushover.Composers;

/// <summary>
/// Registers the package's services. Umbraco finds this composer on its own, and Automate finds
/// the connection type, actions and triggers from their attributes, so sites only install the package.
/// </summary>
public class PushoverComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<PushoverClient>();

        // No connection type, action or trigger registration: Automate discovers classes with
        // [ConnectionType], [Action] and [Trigger] attributes on its own. Only services go here.
        // No icon registration: Umbraco finds wwwroot/umbraco-package.json on its own.
        // No configuration registration: the default references live under Automate's shared
        // Umbraco:Automate:Secrets section, which it resolves out of the box.
    }
}
