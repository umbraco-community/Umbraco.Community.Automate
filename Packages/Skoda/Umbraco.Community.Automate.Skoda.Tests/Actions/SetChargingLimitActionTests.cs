using Moq;
using Shouldly;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Skoda.Actions;
using Umbraco.Community.Automate.Skoda.Connections;
using Xunit;
using Umbraco.Community.Automate.Skoda.Api;

namespace Umbraco.Community.Automate.Skoda.Tests.Actions;

public class SetChargingLimitActionTests
{
    private const string ApiKey = "test-api-key";
    private const string Vin = "TMBJB9NY5RF999999";

    [Fact]
    public async Task ExecuteAsync_sets_the_charging_limit_from_settings()
    {
        var client = new Mock<ISkodaClient>();

        var result = await ActionTestHarness.For<SetChargingLimitAction>()
            .WithService(client.Object)
            .WithSettings(new SetChargingLimitSettings { TargetStateOfChargeInPercent = 70 })
            .WithConnection("community.skoda", new SkodaConnectionSettings { ApiKey = ApiKey, Vin = Vin })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Success);
        client.Verify(c => c.SetChargingLimitAsync(ApiKey, Vin, 70, It.IsAny<CancellationToken>()), Times.Once);
        ((VehicleCommandOutput)result.OutputData!).Vin.ShouldBe(Vin);
    }
}
