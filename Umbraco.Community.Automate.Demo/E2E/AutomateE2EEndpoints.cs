using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.OpenIddict.Credentials;

namespace Umbraco.Community.Automate.Demo.E2E;

/// <summary>
/// Test-only endpoint that seeds a fully-configured Google Sheets connection — an
/// <see cref="OAuthCredentials"/> row plus the <see cref="Connection"/> that references it —
/// bypassing the real Google OAuth consent flow entirely. Only mapped when
/// <see cref="AutomateE2EMode.IsEnabled"/> is true; a Playwright global setup calls this once
/// before the suite runs so the workflow builder has a connection to pick from.
/// </summary>
public static class AutomateE2EEndpoints
{
    private const string ConnectionAlias = "e2e-google-sheets";

    public static IEndpointRouteBuilder MapAutomateE2EEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/umbraco-automate-e2e/seed-google-sheets", SeedGoogleSheetsConnectionAsync);
        return app;
    }

    private static async Task<IResult> SeedGoogleSheetsConnectionAsync(
        IConnectionService connectionService,
        IOAuthCredentialsService credentialsService,
        CancellationToken cancellationToken)
    {
        var existing = await connectionService.GetConnectionByAliasAsync(ConnectionAlias, cancellationToken);
        if (existing is not null)
        {
            return Results.Ok(new { connectionId = existing.Id, connectionAlias = existing.Alias });
        }

        var credentials = await credentialsService.CreateCredentialsAsync(
            new OAuthCredentials
            {
                Provider = "GoogleSheets",
                AccessToken = "e2e-fake-access-token",
                AccountLabel = "E2E Test Account",
                // No expiry/refresh token — GetValidAccessTokenAsync returns the access token
                // as-is, so the run never needs a real refresh-token round trip to Google.
                ExpiresUtc = null,
            },
            cancellationToken);

        var connection = await connectionService.CreateConnectionAsync(
            new Connection
            {
                Alias = ConnectionAlias,
                Name = "E2E Google Sheets",
                Type = "googleSheets",
                Settings = new Dictionary<string, object?>
                {
                    ["OAuthCredentialsId"] = credentials.Id,
                },
            },
            cancellationToken: cancellationToken);

        return Results.Ok(new { connectionId = connection.Id, connectionAlias = connection.Alias });
    }
}
