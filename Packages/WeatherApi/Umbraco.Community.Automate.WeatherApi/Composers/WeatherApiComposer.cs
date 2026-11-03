using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.Automate.WeatherApi.Composers;

public class WeatherApiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient();
    }
}
