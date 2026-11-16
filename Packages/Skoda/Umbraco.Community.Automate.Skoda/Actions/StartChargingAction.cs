using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Skoda.Connections;
using Umbraco.Community.Automate.Skoda.Api;
using Umbraco.Community.Automate.Skoda.Models;

namespace Umbraco.Community.Automate.Skoda.Actions;

[Action(
    "community.skoda.startCharging",
    "Start Charging",
    Group = "Vehicles",
    Icon = "icon-automate-skoda",
    ConnectionTypeAlias = "community.skoda")]
public sealed class StartChargingAction(ActionInfrastructure infrastructure, ISkodaClient client) : ActionBase<StartChargingSettings, VehicleCommandOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<StartChargingSettings>();
        if (SkodaActionErrors.CheckConnection(context, out var connectionSettings) is { } invalid)
            return invalid;

        try
        {
            await client.StartChargingAsync(connectionSettings.ApiKey, connectionSettings.Vin, cancellationToken);

            return Success(new VehicleCommandOutput { Vin = connectionSettings.Vin });
        }
        catch (Exception ex) when (SkodaActionErrors.TryFail(ex, cancellationToken, out var failure))
        {
            return failure;
        }
    }
}
