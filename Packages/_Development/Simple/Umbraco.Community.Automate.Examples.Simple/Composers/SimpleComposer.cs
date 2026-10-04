using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.Automate.Examples.Simple.Composers;

/// <summary>
/// Registers the package's services. Umbraco finds this composer on its own, and Automate finds
/// the connection type, actions and triggers from their attributes, so sites only install the package.
/// </summary>
public class SimpleComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient();

        // No connection type, action or trigger registration: Automate discovers classes with
        // [ConnectionType], [Action] and [Trigger] attributes on its own. Only services go here.
    }
}
