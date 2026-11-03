using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Pushover.Api;

namespace Umbraco.Community.Automate.Pushover.Connections;

/// <summary>
/// The Pushover connection. <b>Test connection</b> asks Pushover to validate the token and key,
/// which doesn't send a notification.
/// </summary>
[ConnectionType("community.pushover", "Pushover",
    Description = "Send push notifications to phones and desktops with Pushover.",
    Group = "Notifications",
    Icon = "icon-automate-pushover")]
public sealed class PushoverConnectionType(ConnectionTypeInfrastructure infrastructure, PushoverClient client)
    : ConnectionTypeBase<PushoverConnectionSettings>(infrastructure)
{
    public override async Task<ConnectionValidationResult> ValidateAsync(object? settings, CancellationToken cancellationToken)
    {
        var typed = settings as PushoverConnectionSettings;
        if (PushoverConnectionSettings.Validate(typed) is { } error)
            return ConnectionValidationResult.Failure(error);

        try
        {
            var response = await client.ValidateUserAsync(typed!.ApiToken, typed.UserKey, cancellationToken);
            var devices = response.Devices is { Count: > 0 } list ? $" Devices: {string.Join(", ", list)}." : string.Empty;
            return ConnectionValidationResult.Success($"Pushover accepted the token and key.{devices}");
        }
        catch (PushoverApiException ex)
        {
            return ConnectionValidationResult.Failure(ex.Message);
        }
    }
}
