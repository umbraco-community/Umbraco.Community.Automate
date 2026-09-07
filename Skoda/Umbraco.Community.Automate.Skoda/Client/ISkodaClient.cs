using Umbraco.Community.Automate.Skoda.Client.Models;

namespace Umbraco.Community.Automate.Skoda.Client;

public interface ISkodaClient
{
    Task<VehicleResponse> GetVehicleAsync(string apiKey, string vin, CancellationToken cancellationToken = default);
    Task StartChargingAsync(string apiKey, string vin, CancellationToken cancellationToken = default);
    Task StopChargingAsync(string apiKey, string vin, CancellationToken cancellationToken = default);
    Task StartAirConditioningAsync(string apiKey, string vin, StartAirConditioningConfiguration configuration, CancellationToken cancellationToken = default);
    Task StopAirConditioningAsync(string apiKey, string vin, CancellationToken cancellationToken = default);
    Task StartAuxiliaryHeatingAsync(string apiKey, string vin, StartAuxiliaryHeatingConfiguration configuration, CancellationToken cancellationToken = default);
    Task StopAuxiliaryHeatingAsync(string apiKey, string vin, CancellationToken cancellationToken = default);
    Task StartActiveVentilationAsync(string apiKey, string vin, CancellationToken cancellationToken = default);
    Task StopActiveVentilationAsync(string apiKey, string vin, CancellationToken cancellationToken = default);
}