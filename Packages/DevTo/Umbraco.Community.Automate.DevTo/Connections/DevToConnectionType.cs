using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.DevTo.Api;

namespace Umbraco.Community.Automate.DevTo.Connections;

[ConnectionType(ConnectionTypeAlias, "DEV Community",
    Description = "Publish articles to DEV (dev.to).",
    Group = "Social Networks",
    Icon = "icon-automate-devto")]
public sealed class DevToConnectionType : ConnectionTypeBase<DevToConnectionSettings>
{
    public const string ConnectionTypeAlias = "community.devto";

    private readonly DevToClient _client;

    public DevToConnectionType(ConnectionTypeInfrastructure infrastructure, DevToClient client)
        : base(infrastructure)
    {
        _client = client;
    }

    public override async Task<ConnectionValidationResult> ValidateAsync(object? settings, CancellationToken cancellationToken)
    {
        var devToSettings = settings as DevToConnectionSettings;

        if (DevToConnectionSettingsValidator.Validate(devToSettings) is { } error)
            return ConnectionValidationResult.Failure(error);

        try
        {
            var user = await _client.GetCurrentUserAsync(devToSettings!, cancellationToken);
            return ConnectionValidationResult.Success($"Connected as @{user.Username ?? "unknown"}.");
        }
        catch (DevToApiException ex) when (ex.Category == StepRunErrorCategory.Authentication)
        {
            return ConnectionValidationResult.Failure("Authentication failed. Check that your API key is valid.");
        }
        catch (DevToApiException ex)
        {
            return ConnectionValidationResult.Failure(ex.Message);
        }
    }
}
