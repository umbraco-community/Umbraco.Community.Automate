using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Skoda.Connections;
using Umbraco.Community.Automate.Skoda.Api;
using Umbraco.Community.Automate.Skoda.Models;

namespace Umbraco.Community.Automate.Skoda.Actions;

[Action(
    "community.skoda.updateChargingProfile",
    "Update Charging Profile",
    Group = "Vehicles",
    Icon = "icon-automate-skoda",
    ConnectionTypeAlias = "community.skoda")]
public sealed class UpdateChargingProfileAction(ActionInfrastructure infrastructure, ISkodaClient client) : ActionBase<UpdateChargingProfileSettings, VehicleCommandOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<UpdateChargingProfileSettings>();
        if (SkodaActionErrors.CheckConnection(context, out var connectionSettings) is { } invalid)
            return invalid;

        try
        {
            var vehicleResponse = await client.GetVehicleAsync(connectionSettings.ApiKey, connectionSettings.Vin, cancellationToken);
            var profile = vehicleResponse.Vehicle.ChargingProfiles?.Profiles.FirstOrDefault(p => p.Id == settings.ProfileId);
            if (profile is null)
            {
                return ActionResult.Failed(
                    new ArgumentException($"No charging profile with id {settings.ProfileId} was found on this vehicle."),
                    StepRunErrorCategory.Validation);
            }

            // The Škoda API applies the submitted profile as a whole, so the fields this action
            // doesn't expose (preferred charging times, timers, ...) are sent back unchanged here
            // rather than defaulted, to avoid silently clearing them.
            var updatedProfile = profile with
            {
                Name = KeepIfBlank(settings.Name, profile.Name),
                Settings = profile.Settings with
                {
                    TargetStateOfChargeInPercent = settings.TargetStateOfChargeInPercent ?? profile.Settings.TargetStateOfChargeInPercent,
                    MaxChargingCurrent = KeepIfBlank(settings.MaxChargingCurrent, profile.Settings.MaxChargingCurrent),
                    AutoUnlockPlugWhenCharged = KeepIfBlank(settings.AutoUnlockPlugWhenCharged, profile.Settings.AutoUnlockPlugWhenCharged),
                },
            };

            await client.UpdateChargingProfileAsync(
                connectionSettings.ApiKey,
                connectionSettings.Vin,
                settings.ProfileId,
                updatedProfile,
                cancellationToken);

            return Success(new VehicleCommandOutput { Vin = connectionSettings.Vin });
        }
        catch (Exception ex) when (SkodaActionErrors.TryFail(ex, cancellationToken, out var failure))
        {
            return failure;
        }
    }

    private static string KeepIfBlank(string? value, string current) => string.IsNullOrWhiteSpace(value) ? current : value;
}
