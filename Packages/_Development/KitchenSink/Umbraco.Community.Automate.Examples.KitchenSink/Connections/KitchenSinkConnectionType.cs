using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Connections;

/// <summary>
/// The Kitchen Sink example connection: stores an API key and checks it when the user clicks
/// <b>Test connection</b>. The alias ("community.examples.kitchenSink") is stored in saved automations and set as each
/// action's ConnectionTypeAlias, so once released it must never change.
/// </summary>
[ConnectionType("community.examples.kitchenSink", "Kitchen Sink Example (httpbin.org)",
    Description = "A reference connection that talks to httpbin.org.",
    Group = "Examples",
    Icon = "icon-automate-kitchensink")]
public sealed class KitchenSinkConnectionType(ConnectionTypeInfrastructure infrastructure, KitchenSinkClient client)
    : ConnectionTypeBase<KitchenSinkConnectionSettings>(infrastructure)
{
    public override async Task<ConnectionValidationResult> ValidateAsync(object? settings, CancellationToken cancellationToken)
    {
        var typed = settings as KitchenSinkConnectionSettings;

        // Check the settings first, without calling the service.
        if (KitchenSinkConnectionSettingsValidator.Validate(typed) is { } error)
            return ConnectionValidationResult.Failure(error);

        // Then make one cheap authenticated call.
        try
        {
            var response = await client.CheckApiKeyAsync(typed!.ApiKey, cancellationToken);
            return response.Authenticated
                ? ConnectionValidationResult.Success("Connected to httpbin.org.")
                : ConnectionValidationResult.Failure("httpbin.org didn't accept the API key.");
        }
        catch (KitchenSinkApiException ex)
        {
            return ConnectionValidationResult.Failure(ex.Message);
        }
    }
}
