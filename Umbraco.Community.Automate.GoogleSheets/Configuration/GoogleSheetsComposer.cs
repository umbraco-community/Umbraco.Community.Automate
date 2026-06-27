using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.Automate.GoogleSheets.Configuration;

/// <summary>
/// Registers the Google Sheets OAuth provider with OpenIddict Client WebIntegration,
/// scoped to Google Sheets access, and configures offline access so a refresh token is issued.
/// </summary>
public sealed class GoogleSheetsComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        // Read credentials at composition time — same path the Core's PostConfigure reads from,
        // so appsettings structure stays consistent for users installing multiple Google packages.
        var clientId = builder.Config["Umbraco:Automate:Providers:GoogleSheets:ClientId"] ?? string.Empty;
        var clientSecret = builder.Config["Umbraco:Automate:Providers:GoogleSheets:ClientSecret"] ?? string.Empty;

        builder.Services.AddOpenIddict()
            .AddClient(options =>
            {
                // Required for token refresh — OpenIddict rejects AuthenticateWithRefreshTokenAsync
                // calls unless this grant type is explicitly allowed, even though a refresh token
                // is already being issued via SetAccessType("offline") below.
                options.AllowRefreshTokenFlow();

                // SetProviderName gives this registration a unique name ("GoogleSheets") so a
                // future package (Google Drive, Google Docs, etc.) can register its own AddGoogle()
                // with a different SetProviderName without colliding — following the pattern shown
                // in the OpenIddict docs for multiple instances of the same provider.
                // SetClientId/SetClientSecret here (not via PostConfigure) is also the pattern the
                // docs require for this scenario: credentials go into the web integration options
                // object, where Google's built-in AttachTokenRequestNonStandardClientCredentials
                // handler reads them when sending the token request.
                options.UseWebProviders().AddGoogle(google => google
                    .SetProviderName("GoogleSheets")
                    .SetRegistrationId("google-sheets")
                    .SetClientId(clientId)
                    .SetClientSecret(clientSecret)
                    .AddScopes("https://www.googleapis.com/auth/spreadsheets")
                    .SetAccessType("offline")
                    .SetPrompt("consent"));
            });
    }
}
