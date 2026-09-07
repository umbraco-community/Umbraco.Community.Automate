using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Skoda.Client;
using Umbraco.Community.Automate.Skoda.Connections;

namespace Umbraco.Community.Automate.Skoda.Actions;

[Action(
    "community.automate.skoda.stopAuxiliaryHeating", 
    "Stop Auxiliary Heating", 
    Group = "Skoda", 
    Icon = "icon-skoda", 
    ConnectionTypeAlias = SkodaConstants.ConnectionTypeAlias)]
public sealed class StopAuxiliaryHeatingAction(ActionInfrastructure infrastructure, ISkodaClient client) : ActionBase<StopAuxiliaryHeatingSettings, VehicleCommandOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<StopAuxiliaryHeatingSettings>();
        var connection = context.Connection ?? throw new InvalidOperationException("A Škoda connection is required.");
        var connectionSettings = connection.GetSettings<SkodaConnectionSettings>();

        await client.StopAuxiliaryHeatingAsync(connectionSettings.ApiKey, connectionSettings.Vin, cancellationToken);

        return Success(new VehicleCommandOutput { Vin = connectionSettings.Vin });
    }
}
