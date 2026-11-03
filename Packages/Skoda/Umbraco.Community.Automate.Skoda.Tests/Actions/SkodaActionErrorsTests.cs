using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Skoda.Actions;
using Umbraco.Community.Automate.Skoda.Api;
using Umbraco.Community.Automate.Skoda.Connections;
using Umbraco.Community.Automate.Skoda.Models;
using Xunit;

namespace Umbraco.Community.Automate.Skoda.Tests.Actions;

/// <summary>
/// Every action shares the same connection check and error mapping; these run it through one
/// action, so Automate can tell failures worth retrying from ones the user must fix.
/// </summary>
public class SkodaActionErrorsTests
{
    private static readonly SkodaConnectionSettings Connection = new() { ApiKey = "key", Vin = "TMBJB9NY5RF999999" };

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.Forbidden, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.NotFound, StepRunErrorCategory.Validation)]
    [InlineData(HttpStatusCode.TooManyRequests, StepRunErrorCategory.RateLimiting)]
    [InlineData(HttpStatusCode.ServiceUnavailable, StepRunErrorCategory.ServiceUnavailable)]
    public async Task Api_failures_are_categorised(HttpStatusCode status, StepRunErrorCategory expected)
    {
        var result = await Run(new SkodaClientException("Failed", status, null, null), Connection);

        Assert.Equal(ActionResultStatus.Failed, result.Status);
        Assert.Equal(expected, result.ErrorCategory);
    }

    [Fact]
    public async Task Unreachable_api_is_service_unavailable()
        => Assert.Equal(StepRunErrorCategory.ServiceUnavailable, (await Run(new HttpRequestException("No such host"), Connection)).ErrorCategory);

    [Fact]
    public async Task Timeout_is_a_timeout()
        => Assert.Equal(StepRunErrorCategory.Timeout, (await Run(new TaskCanceledException("Timed out"), Connection)).ErrorCategory);

    [Fact]
    public async Task Missing_connection_is_a_configuration_error()
        => Assert.Equal(StepRunErrorCategory.ConfigurationError, (await Run((Exception?)null, connection: null)).ErrorCategory);

    [Theory]
    [InlineData("", "TMBJB9NY5RF999999")]
    [InlineData("$Umbraco:Automate:Secrets:Skoda:ApiKey", "TMBJB9NY5RF999999")]
    [InlineData("key", "")]
    public async Task Unusable_connection_is_a_configuration_error_and_calls_nothing(string apiKey, string vin)
    {
        var client = new FakeSkodaClient(null);

        var result = await Run(client, new SkodaConnectionSettings { ApiKey = apiKey, Vin = vin });

        Assert.Equal(StepRunErrorCategory.ConfigurationError, result.ErrorCategory);
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public void Classify_maps_status_codes()
    {
        Assert.Equal(StepRunErrorCategory.InvalidResponse, SkodaClient.Classify(HttpStatusCode.OK));
        Assert.Equal(StepRunErrorCategory.Validation, SkodaClient.Classify(HttpStatusCode.BadRequest));
        Assert.Equal(StepRunErrorCategory.InvalidResponse, SkodaClient.Classify(null));
    }

    private static Task<ActionResult> Run(Exception? exception, SkodaConnectionSettings? connection)
        => Run(new FakeSkodaClient(exception), connection);

    private static Task<ActionResult> Run(FakeSkodaClient client, SkodaConnectionSettings? connection)
    {
        var harness = ActionTestHarness.For<StartChargingAction>()
            .WithService<ISkodaClient>(client)
            .WithSettings(new StartChargingSettings());

        return (connection is null ? harness : harness.WithConnection("community.skoda", connection)).ExecuteAsync();
    }

    /// <summary>Throws the given exception from every call, or succeeds when there's none.</summary>
    private sealed class FakeSkodaClient(Exception? exception) : ISkodaClient
    {
        public int Calls { get; private set; }

        private Task Call()
        {
            Calls++;
            return exception is null ? Task.CompletedTask : Task.FromException(exception);
        }

        public Task<VehicleResponse> GetVehicleAsync(string apiKey, string vin, CancellationToken cancellationToken = default)
            => Call().ContinueWith(_ => default(VehicleResponse)!, cancellationToken);
        public Task StartChargingAsync(string apiKey, string vin, CancellationToken cancellationToken = default) => Call();
        public Task StopChargingAsync(string apiKey, string vin, CancellationToken cancellationToken = default) => Call();
        public Task SetChargingLimitAsync(string apiKey, string vin, int targetStateOfChargeInPercent, CancellationToken cancellationToken = default) => Call();
        public Task SetChargeModeAsync(string apiKey, string vin, string chargeMode, CancellationToken cancellationToken = default) => Call();
        public Task UpdateChargingProfileAsync(string apiKey, string vin, long profileId, ChargingProfile profile, CancellationToken cancellationToken = default) => Call();
        public Task StartAirConditioningAsync(string apiKey, string vin, StartAirConditioningConfiguration configuration, CancellationToken cancellationToken = default) => Call();
        public Task StopAirConditioningAsync(string apiKey, string vin, CancellationToken cancellationToken = default) => Call();
        public Task StartAuxiliaryHeatingAsync(string apiKey, string vin, StartAuxiliaryHeatingConfiguration configuration, CancellationToken cancellationToken = default) => Call();
        public Task StopAuxiliaryHeatingAsync(string apiKey, string vin, CancellationToken cancellationToken = default) => Call();
        public Task StartActiveVentilationAsync(string apiKey, string vin, CancellationToken cancellationToken = default) => Call();
        public Task StopActiveVentilationAsync(string apiKey, string vin, CancellationToken cancellationToken = default) => Call();
    }
}
