using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Client;
using Shouldly;
using Xunit;

namespace Umbraco.Community.Automate.GoogleSheets.Tests.Configuration;

/// <summary>
/// Verifies that two AddGoogle() registrations with distinct ProviderName/RegistrationId
/// values (e.g. GoogleSheets + GoogleDocs) can coexist in the same OpenIddict options
/// without collision — mirroring the real-app scenario where GoogleSheetsComposer and
/// TestComposer both call AddOpenIddict().AddClient().UseWebProviders().AddGoogle().
/// </summary>
public class MultipleGoogleProvidersTests
{
    // Applies IConfigureOptions only (no PostConfigure validation), giving us the raw
    // list of registrations as they come out of the AddGoogle() lambdas.
    private static OpenIddictClientOptions BuildOptions(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        var sp = services.BuildServiceProvider();

        var opts = new OpenIddictClientOptions();
        foreach (var c in sp.GetServices<IConfigureOptions<OpenIddictClientOptions>>())
            c.Configure(opts);

        return opts;
    }

    [Fact]
    public void Two_AddGoogle_providers_with_distinct_names_produce_two_registrations()
    {
        var opts = BuildOptions(services =>
        {
            // Mirrors GoogleSheetsComposer
            services.AddOpenIddict()
                .AddClient(options => options.UseWebProviders().AddGoogle(g => g
                    .SetProviderName("GoogleSheets")
                    .SetRegistrationId("google-sheets")
                    .SetClientId("sheets-client-id")
                    .SetClientSecret("sheets-client-secret")
                    .AddScopes("https://www.googleapis.com/auth/spreadsheets")
                    .SetAccessType("offline")
                    .SetPrompt("consent")));

            // Mirrors TestComposer
            services.AddOpenIddict()
                .AddClient(options => options.UseWebProviders().AddGoogle(g => g
                    .SetProviderName("GoogleDocs")
                    .SetRegistrationId("google-docs")
                    .SetClientId("docs-client-id")
                    .SetClientSecret("docs-client-secret")
                    .AddScopes("https://www.googleapis.com/auth/documents")
                    .SetAccessType("offline")
                    .SetPrompt("consent")));
        });

        opts.Registrations.Count.ShouldBe(2);
    }

    [Fact]
    public void Provider_names_are_distinct_across_both_registrations()
    {
        var opts = BuildOptions(services =>
        {
            services.AddOpenIddict()
                .AddClient(options => options.UseWebProviders().AddGoogle(g => g
                    .SetProviderName("GoogleSheets")
                    .SetRegistrationId("google-sheets")
                    .SetClientId("sheets-client-id")
                    .SetClientSecret("sheets-client-secret")));

            services.AddOpenIddict()
                .AddClient(options => options.UseWebProviders().AddGoogle(g => g
                    .SetProviderName("GoogleDocs")
                    .SetRegistrationId("google-docs")
                    .SetClientId("docs-client-id")
                    .SetClientSecret("docs-client-secret")));
        });

        opts.Registrations.Select(r => r.ProviderName).Distinct().Count()
            .ShouldBe(2, "each provider must have a unique ProviderName");
    }

    [Fact]
    public void Registration_ids_are_distinct_across_both_registrations()
    {
        var opts = BuildOptions(services =>
        {
            services.AddOpenIddict()
                .AddClient(options => options.UseWebProviders().AddGoogle(g => g
                    .SetProviderName("GoogleSheets")
                    .SetRegistrationId("google-sheets")
                    .SetClientId("sheets-client-id")
                    .SetClientSecret("sheets-client-secret")));

            services.AddOpenIddict()
                .AddClient(options => options.UseWebProviders().AddGoogle(g => g
                    .SetProviderName("GoogleDocs")
                    .SetRegistrationId("google-docs")
                    .SetClientId("docs-client-id")
                    .SetClientSecret("docs-client-secret")));
        });

        opts.Registrations.Select(r => r.RegistrationId).Distinct().Count()
            .ShouldBe(2, "each provider must have a unique RegistrationId");
    }

    [Fact]
    public void GoogleSheets_registration_carries_correct_provider_name_and_id()
    {
        var opts = BuildOptions(services =>
        {
            services.AddOpenIddict()
                .AddClient(options => options.UseWebProviders().AddGoogle(g => g
                    .SetProviderName("GoogleSheets")
                    .SetRegistrationId("google-sheets")
                    .SetClientId("sheets-client-id")
                    .SetClientSecret("sheets-client-secret")));

            services.AddOpenIddict()
                .AddClient(options => options.UseWebProviders().AddGoogle(g => g
                    .SetProviderName("GoogleDocs")
                    .SetRegistrationId("google-docs")
                    .SetClientId("docs-client-id")
                    .SetClientSecret("docs-client-secret")));
        });

        var reg = opts.Registrations.Single(r => r.ProviderName == "GoogleSheets");
        reg.RegistrationId.ShouldBe("google-sheets");
        reg.ClientId.ShouldBe("sheets-client-id");
    }

    [Fact]
    public void GoogleDocs_registration_carries_correct_provider_name_and_id()
    {
        var opts = BuildOptions(services =>
        {
            services.AddOpenIddict()
                .AddClient(options => options.UseWebProviders().AddGoogle(g => g
                    .SetProviderName("GoogleSheets")
                    .SetRegistrationId("google-sheets")
                    .SetClientId("sheets-client-id")
                    .SetClientSecret("sheets-client-secret")));

            services.AddOpenIddict()
                .AddClient(options => options.UseWebProviders().AddGoogle(g => g
                    .SetProviderName("GoogleDocs")
                    .SetRegistrationId("google-docs")
                    .SetClientId("docs-client-id")
                    .SetClientSecret("docs-client-secret")));
        });

        var reg = opts.Registrations.Single(r => r.ProviderName == "GoogleDocs");
        reg.RegistrationId.ShouldBe("google-docs");
        reg.ClientId.ShouldBe("docs-client-id");
    }

    [Fact]
    public void Each_provider_uses_separate_client_credentials()
    {
        var opts = BuildOptions(services =>
        {
            services.AddOpenIddict()
                .AddClient(options => options.UseWebProviders().AddGoogle(g => g
                    .SetProviderName("GoogleSheets")
                    .SetRegistrationId("google-sheets")
                    .SetClientId("sheets-client-id")
                    .SetClientSecret("sheets-secret")));

            services.AddOpenIddict()
                .AddClient(options => options.UseWebProviders().AddGoogle(g => g
                    .SetProviderName("GoogleDocs")
                    .SetRegistrationId("google-docs")
                    .SetClientId("docs-client-id")
                    .SetClientSecret("docs-secret")));
        });

        var sheets = opts.Registrations.Single(r => r.ProviderName == "GoogleSheets");
        var docs = opts.Registrations.Single(r => r.ProviderName == "GoogleDocs");

        sheets.ClientId.ShouldNotBe(docs.ClientId);
        sheets.ClientSecret.ShouldNotBe(docs.ClientSecret);
    }
}
