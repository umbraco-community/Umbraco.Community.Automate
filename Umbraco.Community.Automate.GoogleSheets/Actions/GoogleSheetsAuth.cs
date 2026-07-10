using System.Net.Http.Headers;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;
using Umbraco.Community.Automate.GoogleSheets.Connection;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Resolves the connection's OAuth credential into a valid access token and hands back an
/// authenticated <see cref="HttpClient"/>, ready to call the Sheets API. Shared across every
/// action in this provider, since they all authenticate the same way.
/// </summary>
public static class GoogleSheetsAuth
{
    public static async Task<(HttpClient? Client, ActionResult? Error)> AuthenticateAsync(
        ActionContext context,
        IHttpClientFactory httpClientFactory,
        IOAuthCredentialsService credentialsService,
        CancellationToken cancellationToken)
    {
        var connectionSettings = context.Connection?.GetSettings<GoogleSheetsConnectionSettings>();
        if (connectionSettings?.OAuthCredentialsId is not { } credentialId || credentialId == Guid.Empty)
            return (null, ActionResult.Failed(
                new InvalidOperationException("Google account is not authenticated."),
                StepRunErrorCategory.Authentication));

        var token = await credentialsService.GetValidAccessTokenAsync(credentialId, cancellationToken);
        if (string.IsNullOrEmpty(token))
            return (null, ActionResult.Failed(
                new InvalidOperationException("Google access token is expired or revoked. Reconnect the account."),
                StepRunErrorCategory.Authentication));

        var client = httpClientFactory.CreateClient("UmbracoAutomate");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, null);
    }
}
