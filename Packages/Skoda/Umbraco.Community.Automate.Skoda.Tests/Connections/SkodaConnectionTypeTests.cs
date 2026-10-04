using System.Net;
using Moq;
using Shouldly;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Skoda.Connections;
using Xunit;
using Umbraco.Community.Automate.Skoda.Api;
using Umbraco.Community.Automate.Skoda.Models;

namespace Umbraco.Community.Automate.Skoda.Tests.Connections;

public class SkodaConnectionTypeTests
{
    [Fact]
    public async Task ValidateAsync_fails_when_settings_are_the_wrong_type()
    {
        var sut = CreateSut(out _);

        var result = await sut.ValidateAsync(new object(), CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Failure);
    }

    [Fact]
    public async Task ValidateAsync_fails_when_api_key_is_missing()
    {
        var sut = CreateSut(out _);

        var result = await sut.ValidateAsync(
            new SkodaConnectionSettings { ApiKey = "", Vin = Vin },
            CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Failure);
    }

    [Fact]
    public async Task ValidateAsync_fails_when_vin_is_missing()
    {
        var sut = CreateSut(out _);

        var result = await sut.ValidateAsync(
            new SkodaConnectionSettings { ApiKey = ApiKey, Vin = "" },
            CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Failure);
    }

    [Fact]
    public void New_connections_default_to_the_configuration_reference()
        => Assert.Equal("$Umbraco:Automate:Secrets:Skoda:ApiKey", new SkodaConnectionSettings().ApiKey);

    [Fact]
    public async Task ValidateAsync_explains_an_unresolved_api_key_reference_without_calling_the_api()
    {
        var sut = CreateSut(out var client);

        // A reference arriving unresolved (Automate normally resolves it, or reports the missing key, first).
        var result = await sut.ValidateAsync(
            new SkodaConnectionSettings { Vin = Vin, ValidateConnection = true },
            CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Contains("Umbraco:Automate:Secrets:Skoda:ApiKey", result.Message);
        Assert.Empty(client.Invocations);
    }

    [Fact]
    public async Task ValidateAsync_returns_warning_without_calling_the_api_when_validation_is_disabled()
    {
        var sut = CreateSut(out var client);

        var result = await sut.ValidateAsync(
            new SkodaConnectionSettings { ApiKey = ApiKey, Vin = Vin, ValidateConnection = false },
            CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Warning);
        client.Verify(
            c => c.GetVehicleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_succeeds_when_the_api_returns_the_vehicle()
    {
        var sut = CreateSut(out var client);
        client.Setup(c => c.GetVehicleAsync(ApiKey, Vin, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new VehicleResponse(
                  new Vehicle(Vin, "My Car", "AB123CD", new Uri("https://example.com"), null, null, null, null, null, null, null, null, null),
                  []));

        var result = await sut.ValidateAsync(
            new SkodaConnectionSettings { ApiKey = ApiKey, Vin = Vin, ValidateConnection = true },
            CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Success);
    }

    [Fact]
    public async Task ValidateAsync_passes_the_cancellation_token_through_to_the_client()
    {
        using var cts = new CancellationTokenSource();
        var sut = CreateSut(out var client);
        client.Setup(c => c.GetVehicleAsync(ApiKey, Vin, cts.Token))
              .ReturnsAsync(new VehicleResponse(
                  new Vehicle(Vin, "My Car", "AB123CD", new Uri("https://example.com"), null, null, null, null, null, null, null, null, null),
                  []));

        await sut.ValidateAsync(
            new SkodaConnectionSettings { ApiKey = ApiKey, Vin = Vin, ValidateConnection = true },
            cts.Token);

        client.Verify(c => c.GetVehicleAsync(ApiKey, Vin, cts.Token), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_fails_with_friendly_message_when_the_api_call_throws()
    {
        var sut = CreateSut(out var client);
        client.Setup(c => c.GetVehicleAsync(ApiKey, Vin, It.IsAny<CancellationToken>()))
              .ThrowsAsync(new SkodaClientException("Vehicle not found", HttpStatusCode.NotFound, null, null));

        var result = await sut.ValidateAsync(
            new SkodaConnectionSettings { ApiKey = ApiKey, Vin = Vin, ValidateConnection = true },
            CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Failure);
        result.Message!.ShouldContain("Vehicle not found");
    }

    private const string ApiKey = "test-api-key";
    private const string Vin = "TMBJB9NY5RF999999";

    private static SkodaConnectionType CreateSut(out Mock<ISkodaClient> client)
    {
        client = new Mock<ISkodaClient>();
        var infrastructure = new ConnectionTypeInfrastructure(Mock.Of<IEditableModelResolver>());
        return new SkodaConnectionType(infrastructure, client.Object);
    }
}
