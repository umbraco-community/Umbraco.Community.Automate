using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Skoda.Connections;
using Umbraco.Community.Automate.Skoda.Api;

namespace Umbraco.Community.Automate.Skoda.Actions;

[Action(
    "community.skoda.stopAirConditioning", 
    "Stop Air Conditioning", 
    Group = "Vehicles", 
    Icon = "icon-automate-skoda", 
    ConnectionTypeAlias = "community.skoda")]
public sealed class StopAirConditioningAction(ActionInfrastructure infrastructure, ISkodaClient client) : ActionBase<StopAirConditioningSettings, VehicleCommandOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<StopAirConditioningSettings>();
        if (SkodaActionErrors.CheckConnection(context, out var connectionSettings) is { } invalid)
            return invalid;

        try
        {
            await client.StopAirConditioningAsync(connectionSettings.ApiKey, connectionSettings.Vin, cancellationToken);

            return Success(new VehicleCommandOutput { Vin = connectionSettings.Vin });
        }
        catch (Exception ex) when (SkodaActionErrors.TryFail(ex, cancellationToken, out var failure))
        {
            return failure;
        }
    }
}
