using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.Automate.GoogleSheets.Configuration;

/// <summary>
/// Registers the Google OAuth provider with OpenIddict Client WebIntegration, scoped to
/// Google Sheets access, and configures offline access so a refresh token is issued.
/// </summary>
public sealed class GoogleSheetsComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOpenIddict()
            .AddClient(options =>
            {
                // Required for token refresh — OpenIddict rejects AuthenticateWithRefreshTokenAsync
                // calls unless this grant type is explicitly allowed, even though a refresh token
                // is already being issued via SetAccessType("offline") below.
                options.AllowRefreshTokenFlow();

                // ProviderName/RegistrationId are namespaced to this product (not generic "Google")
                // so a future Google-flavored package (Drive, Gmail, etc.) can register its own
                // OpenIddict client without colliding — OpenIddict rejects two registrations that
                // share the same provider name/issuer without a distinct identifier.
                options.UseWebProviders().AddGoogle(google => google
                    .SetProviderName("GoogleSheets")
                    .SetRegistrationId("google-sheets")
                    .AddScopes("https://www.googleapis.com/auth/spreadsheets")
                    .SetAccessType("offline")
                    .SetPrompt("consent"));
            });
    }
}
