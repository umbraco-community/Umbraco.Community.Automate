using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.Automate.Provider1.Configuration;

/// <summary>
/// Registers services for the Provider1 provider.
/// Actions and triggers are auto-discovered — you only need this composer
/// if your actions require custom services registered in DI.
/// </summary>
public sealed class Provider1Composer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register custom services here, e.g.:
        // builder.Services.AddHttpClient("Provider1");
    }
}
