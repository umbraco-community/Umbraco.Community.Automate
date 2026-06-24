using Umbraco.Community.Automate.Demo.E2E;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

if (AutomateE2EMode.IsEnabled)
{
    builder.Services.AddSingleton<GoogleSheetsRequestLog>();
    builder.Services.AddTransient<GoogleSheetsStubHandler>();
    builder.Services.AddHttpClient("UmbracoAutomate")
        .AddHttpMessageHandler<GoogleSheetsStubHandler>();
}

WebApplication app = builder.Build();


await app.BootUmbracoAsync();


app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

if (AutomateE2EMode.IsEnabled)
{
    app.MapAutomateE2EEndpoints();
}

await app.RunAsync();
