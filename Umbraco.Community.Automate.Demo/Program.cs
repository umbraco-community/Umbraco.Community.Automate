using Umbraco.Community.Automate.Demo.E2E;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Your own real credentials for trying the packages against live services. The file is
// git-ignored, so nothing in it can be committed; copy appsettings.Local.example.json to start.
// Loaded last, so it overrides appsettings.Development.json's placeholders and user secrets, and
// before Umbraco's builder runs, because some packages (Google Sheets) read their settings then.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

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
