using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.Automate.Skoda.Composers;

public class SkodaComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddSkodaAutomate();
    }
}