using Moq;
using Shouldly;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Skoda.Actions;
using Umbraco.Community.Automate.Skoda.Connections;
using Xunit;
using Umbraco.Community.Automate.Skoda.Api;
using Umbraco.Community.Automate.Skoda.Models;

namespace Umbraco.Community.Automate.Skoda.Tests.Actions;

public class GetVehicleStatusActionTests
{
    private const string ApiKey = "test-api-key";
    private const string Vin = "TMBJB9NY5RF999999";

    [Fact]
    public async Task ExecuteAsync_maps_full_vehicle_response_to_output()
    {
        var vehicle = new Vehicle(
            Vin,
            "My Car",
            "AB123CD",
            new Uri("https://example.com"),
            null,
            null,
            new Odometer(12345, DateTimeOffset.UtcNow),
            new ParkingPosition("PARKED", new GpsCoordinates(52.1, 5.1), "Some Street 1, Prague"),
            new AirConditioning("COOLING", new TargetTemperature(21, "CELSIUS"), DateTimeOffset.UtcNow, false, false, new WindowHeating(false, "OFF", "OFF"), DateTimeOffset.UtcNow),
            null,
            null,
            new Charging(
                false,
                new ChargingStatus(10, 5, 30, DateTimeOffset.UtcNow, "CHARGING", "AC", new BatteryStatus(150000, 65)),
                new ChargingSettings(80, 80, "MANUAL", [], "DEACTIVATED", "PERMANENT", "MAXIMUM", 10),
                DateTimeOffset.UtcNow),
            null);

        var client = new Mock<ISkodaClient>();
        client.Setup(c => c.GetVehicleAsync(ApiKey, Vin, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new VehicleResponse(
                  vehicle,
                  [new VehicleError("SOME_ERROR", "Something could not be retrieved.")]));

        var result = await ActionTestHarness.For<GetVehicleStatusAction>()
            .WithService(client.Object)
            .WithSettings(new GetVehicleStatusSettings())
            .WithConnection("community.skoda", new SkodaConnectionSettings { ApiKey = ApiKey, Vin = Vin })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (GetVehicleStatusOutput)result.OutputData!;
        output.Vin.ShouldBe(Vin);
        output.Name.ShouldBe("My Car");
        output.LicensePlate.ShouldBe("AB123CD");
        output.MileageInKm.ShouldBe(12345);
        output.StateOfChargeInPercent.ShouldBe(65);
        output.RemainingCruisingRangeInMeters.ShouldBe(150000);
        output.ChargingState.ShouldBe("CHARGING");
        output.AirConditioningState.ShouldBe("COOLING");
        output.ParkingState.ShouldBe("PARKED");
        output.Latitude.ShouldBe(52.1);
        output.Longitude.ShouldBe(5.1);
        output.FormattedAddress.ShouldBe("Some Street 1, Prague");
        output.Errors.ShouldHaveSingleItem();
        output.Errors.Single().Type.ShouldBe("SOME_ERROR");
        output.Errors.Single().Description.ShouldBe("Something could not be retrieved.");
    }

    [Fact]
    public async Task ExecuteAsync_maps_unsupported_parts_to_null_instead_of_throwing()
    {
        var vehicle = new Vehicle(
            Vin,
            "My Car",
            "AB123CD",
            new Uri("https://example.com"),
            null, null, null, null, null, null, null, null, null);

        var client = new Mock<ISkodaClient>();
        client.Setup(c => c.GetVehicleAsync(ApiKey, Vin, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new VehicleResponse(vehicle, []));

        var result = await ActionTestHarness.For<GetVehicleStatusAction>()
            .WithService(client.Object)
            .WithSettings(new GetVehicleStatusSettings())
            .WithConnection("community.skoda", new SkodaConnectionSettings { ApiKey = ApiKey, Vin = Vin })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (GetVehicleStatusOutput)result.OutputData!;
        output.MileageInKm.ShouldBeNull();
        output.StateOfChargeInPercent.ShouldBeNull();
        output.RemainingCruisingRangeInMeters.ShouldBeNull();
        output.ChargingState.ShouldBeNull();
        output.AirConditioningState.ShouldBeNull();
        output.ParkingState.ShouldBeNull();
        output.Latitude.ShouldBeNull();
        output.Longitude.ShouldBeNull();
        output.FormattedAddress.ShouldBeNull();
        output.Errors.ShouldBeEmpty();
    }
}
