using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Skoda.Client;
using Umbraco.Community.Automate.Skoda.Client.Models;
using Umbraco.Community.Automate.Skoda.Connections;

namespace Umbraco.Community.Automate.Skoda.Actions;

[Action(
    "community.automate.skoda.startAirConditioning",
    "Start Air Conditioning",
    Group = "Skoda",
    Icon = "icon-skoda",
    ConnectionTypeAlias = SkodaConstants.ConnectionTypeAlias)]
public sealed class StartAirConditioningAction(ActionInfrastructure infrastructure, ISkodaClient client) : ActionBase<StartAirConditioningSettings, VehicleCommandOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<StartAirConditioningSettings>();
        var connection = context.Connection ?? throw new InvalidOperationException("A Škoda connection is required.");
        var connectionSettings = connection.GetSettings<SkodaConnectionSettings>();

        var configuration = new StartAirConditioningConfiguration(
            new TargetTemperature(settings.TargetTemperature, settings.TemperatureUnit),
            settings.WithoutExternalPower);

        await client.StartAirConditioningAsync(
            connectionSettings.ApiKey,
            connectionSettings.Vin,
            configuration,
            cancellationToken);

        return Success(new VehicleCommandOutput { Vin = connectionSettings.Vin });
    }
}
