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

public class StartAuxiliaryHeatingActionTests
{
    private const string ApiKey = "test-api-key";
    private const string Vin = "TMBJB9NY5RF999999";

    [Fact]
    public void New_steps_read_the_S_PIN_from_configuration()
        => new StartAuxiliaryHeatingSettings().Spin.ShouldBe("$Umbraco:Automate:Secrets:Skoda:Spin");

    [Fact]
    public async Task ExecuteAsync_builds_configuration_from_settings()
    {
        var client = new Mock<ISkodaClient>();
        StartAuxiliaryHeatingConfiguration? sentConfiguration = null;
        client.Setup(c => c.StartAuxiliaryHeatingAsync(ApiKey, Vin, It.IsAny<StartAuxiliaryHeatingConfiguration>(), It.IsAny<CancellationToken>()))
              .Callback<string, string, StartAuxiliaryHeatingConfiguration, CancellationToken>((_, _, config, _) => sentConfiguration = config)
              .Returns(Task.CompletedTask);

        var result = await ActionTestHarness.For<StartAuxiliaryHeatingAction>()
            .WithService(client.Object)
            .WithSettings(new StartAuxiliaryHeatingSettings
            {
                TargetTemperature = 23,
                TemperatureUnit = "CELSIUS",
                Spin = "1234",
                DurationInSeconds = 900,
                StartMode = "VENTILATION",
            })
            .WithConnection("community.skoda", new SkodaConnectionSettings { ApiKey = ApiKey, Vin = Vin })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Success);
        sentConfiguration.ShouldNotBeNull();
        sentConfiguration.Spin.ShouldBe("1234");
        sentConfiguration.DurationInSeconds.ShouldBe(900);
        sentConfiguration.StartMode.ShouldBe("VENTILATION");
        sentConfiguration.TargetTemperature.Value.ShouldBe(23);
        ((VehicleCommandOutput)result.OutputData!).Vin.ShouldBe(Vin);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_without_calling_the_api_when_spin_is_missing()
    {
        var client = new Mock<ISkodaClient>();

        var result = await ActionTestHarness.For<StartAuxiliaryHeatingAction>()
            .WithService(client.Object)
            .WithSettings(new StartAuxiliaryHeatingSettings { Spin = "" })
            .WithConnection("community.skoda", new SkodaConnectionSettings { ApiKey = ApiKey, Vin = Vin })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        client.Verify(
            c => c.StartAuxiliaryHeatingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StartAuxiliaryHeatingConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
