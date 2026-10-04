using Moq;
using Shouldly;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Skoda.Actions;
using Umbraco.Community.Automate.Skoda.Connections;
using Xunit;
using Umbraco.Community.Automate.Skoda.Api;

namespace Umbraco.Community.Automate.Skoda.Tests.Actions;

public class StopChargingActionTests
{
    private const string ApiKey = "test-api-key";
    private const string Vin = "TMBJB9NY5RF999999";

    [Fact]
    public async Task ExecuteAsync_stops_charging_for_the_connections_vehicle()
    {
        var client = new Mock<ISkodaClient>();

        var result = await ActionTestHarness.For<StopChargingAction>()
            .WithService(client.Object)
            .WithSettings(new StopChargingSettings())
            .WithConnection("community.skoda", new SkodaConnectionSettings { ApiKey = ApiKey, Vin = Vin })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Success);
        client.Verify(c => c.StopChargingAsync(ApiKey, Vin, It.IsAny<CancellationToken>()), Times.Once);
        ((VehicleCommandOutput)result.OutputData!).Vin.ShouldBe(Vin);
    }
}
