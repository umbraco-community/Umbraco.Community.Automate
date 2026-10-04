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

public class StartAirConditioningActionTests
{
    private const string ApiKey = "test-api-key";
    private const string Vin = "TMBJB9NY5RF999999";

    [Fact]
    public async Task ExecuteAsync_builds_configuration_from_settings()
    {
        var client = new Mock<ISkodaClient>();
        StartAirConditioningConfiguration? sentConfiguration = null;
        client.Setup(c => c.StartAirConditioningAsync(ApiKey, Vin, It.IsAny<StartAirConditioningConfiguration>(), It.IsAny<CancellationToken>()))
              .Callback<string, string, StartAirConditioningConfiguration, CancellationToken>((_, _, config, _) => sentConfiguration = config)
              .Returns(Task.CompletedTask);

        var result = await ActionTestHarness.For<StartAirConditioningAction>()
            .WithService(client.Object)
            .WithSettings(new StartAirConditioningSettings
            {
                TargetTemperature = 22.5,
                TemperatureUnit = "FAHRENHEIT",
                WithoutExternalPower = true,
            })
            .WithConnection("community.skoda", new SkodaConnectionSettings { ApiKey = ApiKey, Vin = Vin })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Success);
        sentConfiguration.ShouldNotBeNull();
        sentConfiguration.TargetTemperature.Value.ShouldBe(22.5);
        sentConfiguration.TargetTemperature.Unit.ShouldBe("FAHRENHEIT");
        sentConfiguration.AirConditioningWithoutExternalPower.ShouldBeTrue();
        ((VehicleCommandOutput)result.OutputData!).Vin.ShouldBe(Vin);
    }
}
