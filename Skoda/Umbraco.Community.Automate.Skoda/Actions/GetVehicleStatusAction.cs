using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Skoda.Client;
using Umbraco.Community.Automate.Skoda.Connections;

namespace Umbraco.Community.Automate.Skoda.Actions;

[Action(
    "community.automate.skoda.getVehicleStatus",
    "Get Vehicle Status",
    Group = "Skoda",
    Icon = "icon-skoda",
    ConnectionTypeAlias = SkodaConstants.ConnectionTypeAlias)]
public class GetVehicleStatusAction(ActionInfrastructure infrastructure, ISkodaClient client) : ActionBase<GetVehicleStatusSettings, GetVehicleStatusOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var connection = context.Connection ?? throw new InvalidOperationException("A Škoda connection is required.");
        var settings = connection.GetSettings<SkodaConnectionSettings>();

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
}