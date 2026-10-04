using System.Net.Http.Headers;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Examples.Simple.Configuration;

namespace Umbraco.Community.Automate.Examples.Simple.Connections;

/// <summary>
/// The connection: stores an API key and checks it when the user clicks <b>Test connection</b>.
/// The alias is stored in saved automations and set as the action's ConnectionTypeAlias, so once
/// released it must never change.
/// </summary>
[ConnectionType("community.examples.simple", "Simple Example (httpbin.org)",
    Description = "The smallest useful connection: an API key and one action.",
    Group = "Examples",
    Icon = "icon-paper-plane")]
public sealed class SimpleConnectionType(ConnectionTypeInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
    : ConnectionTypeBase<SimpleConnectionSettings>(infrastructure)
{
    public override async Task<ConnectionValidationResult> ValidateAsync(object? settings, CancellationToken cancellationToken)
    {
        var apiKey = (settings as SimpleConnectionSettings)?.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return ConnectionValidationResult.Failure("An API key is required.");

        // One cheap authenticated call. httpbin.org/bearer returns 200 for any bearer token.
        using var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(SimpleConfiguration.BaseUrl), "bearer"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode
                ? ConnectionValidationResult.Success("Connected to httpbin.org.")
                : ConnectionValidationResult.Failure($"httpbin.org rejected the API key ({(int)response.StatusCode}).");
        }
        catch (HttpRequestException ex)
        {
            return ConnectionValidationResult.Failure($"Could not reach httpbin.org: {ex.Message}");
        }
    }
}
