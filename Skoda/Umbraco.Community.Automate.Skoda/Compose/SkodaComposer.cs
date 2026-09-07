using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.Automate.Skoda.Compose;

public class SkodaComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddSkodaAutomate();
    }
}