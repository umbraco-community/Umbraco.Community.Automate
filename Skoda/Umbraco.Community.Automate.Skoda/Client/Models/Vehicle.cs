using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Skoda.Client.Models;

public sealed record Vehicle(
    [property: JsonPropertyName("vin")] string Vin,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("licensePlate")] string LicensePlate,
    [property: JsonPropertyName("renderUrl")] Uri RenderUrl,
    [property: JsonPropertyName("status")] VehicleStatus? Status,
    [property: JsonPropertyName("fuelStatus")] FuelStatus? FuelStatus,
    [property: JsonPropertyName("odometer")] Odometer? Odometer,
    [property: JsonPropertyName("parkingPosition")] ParkingPosition? ParkingPosition,
    [property: JsonPropertyName("airConditioning")] AirConditioning? AirConditioning,
    [property: JsonPropertyName("auxiliaryHeating")] AuxiliaryHeating? AuxiliaryHeating,
    [property: JsonPropertyName("activeVentilation")] ActiveVentilation? ActiveVentilation,
    [property: JsonPropertyName("charging")] Charging? Charging,
    [property: JsonPropertyName("chargingProfiles")] ChargingProfiles? ChargingProfiles)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}