using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Skoda.Connections;
using Umbraco.Community.Automate.Skoda.Api;
using Umbraco.Community.Automate.Skoda.Models;

namespace Umbraco.Community.Automate.Skoda.Actions;

[Action(
    "community.skoda.getVehicleStatus",
    "Get Vehicle Status",
    Group = "Vehicles",
    Icon = "icon-automate-skoda",
    ConnectionTypeAlias = "community.skoda")]
public class GetVehicleStatusAction(ActionInfrastructure infrastructure, ISkodaClient client) : ActionBase<GetVehicleStatusSettings, GetVehicleStatusOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        if (SkodaActionErrors.CheckConnection(context, out var settings) is { } invalid)
            return invalid;

        try
        {
            var response = await client.GetVehicleAsync(settings.ApiKey, settings.Vin, cancellationToken);
            var vehicle = response.Vehicle;

            return Success(new GetVehicleStatusOutput
            {
                Vin = vehicle.Vin,
                Name = vehicle.Name,
                LicensePlate = vehicle.LicensePlate,
                MileageInKm = vehicle.Odometer?.MileageInKm,
                StateOfChargeInPercent = vehicle.Charging?.Status.Battery.StateOfChargeInPercent,
                RemainingCruisingRangeInMeters = vehicle.Charging?.Status.Battery.RemainingCruisingRangeInMeters,
                ChargingState = vehicle.Charging?.Status.State,
                AirConditioningState = vehicle.AirConditioning?.State,
                ParkingState = vehicle.ParkingPosition?.State,
                Latitude = vehicle.ParkingPosition?.GpsCoordinates?.Latitude,
                Longitude = vehicle.ParkingPosition?.GpsCoordinates?.Longitude,
                FormattedAddress = vehicle.ParkingPosition?.FormattedAddress,
                Errors = response.Errors
                    .Select(x => new VehicleErrorOutput
                    {
                        Type = x.Type,
                        Description = x.Description
                    })
                    .ToArray()
            });
        }
        catch (Exception ex) when (SkodaActionErrors.TryFail(ex, cancellationToken, out var failure))
        {
            return failure;
        }
    }
}