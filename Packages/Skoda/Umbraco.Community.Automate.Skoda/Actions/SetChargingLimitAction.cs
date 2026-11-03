using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Skoda.Connections;
using Umbraco.Community.Automate.Skoda.Api;
using Umbraco.Community.Automate.Skoda.Models;

namespace Umbraco.Community.Automate.Skoda.Actions;

[Action(
    "community.skoda.setChargingLimit",
    "Set Charging Limit",
    Group = "Vehicles",
    Icon = "icon-automate-skoda",
    ConnectionTypeAlias = "community.skoda")]
public sealed class SetChargingLimitAction(ActionInfrastructure infrastructure, ISkodaClient client) : ActionBase<SetChargingLimitSettings, VehicleCommandOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<SetChargingLimitSettings>();
        if (SkodaActionErrors.CheckConnection(context, out var connectionSettings) is { } invalid)
            return invalid;

        try
        {
            await client.SetChargingLimitAsync(
                connectionSettings.ApiKey,
                connectionSettings.Vin,
                settings.TargetStateOfChargeInPercent,
                cancellationToken);

            return Success(new VehicleCommandOutput { Vin = connectionSettings.Vin });
        }
        catch (Exception ex) when (SkodaActionErrors.TryFail(ex, cancellationToken, out var failure))
        {
            return failure;
        }
    }
}
