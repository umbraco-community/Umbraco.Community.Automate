using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Automate.Skoda.Api;
using Umbraco.Community.Automate.Skoda.Configuration;

namespace Umbraco.Community.Automate.Skoda.Composers;

internal static class UmbracoBuilderExtensions
{
    extension(IUmbracoBuilder builder)
    {
        public IUmbracoBuilder AddSkodaAutomate()
        {
            builder.Services.AddHttpClient<ISkodaClient, SkodaClient>(
                client => client.BaseAddress = new Uri(SkodaConfiguration.BaseUrl));

            return builder;
        }
    }
}
