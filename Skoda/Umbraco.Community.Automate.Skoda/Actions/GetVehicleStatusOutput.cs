namespace Umbraco.Community.Automate.Skoda.Actions;

public sealed class GetVehicleStatusOutput
{
    public string Vin { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? LicensePlate { get; set; }
    public long? MileageInKm { get; set; }
    public int? StateOfChargeInPercent { get; set; }
    public int? RemainingCruisingRangeInMeters { get; set; }
    public string? ChargingState { get; set; }
    public string? AirConditioningState { get; set; }
    public string? ParkingState { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? FormattedAddress { get; set; }
    public IReadOnlyCollection<VehicleErrorOutput> Errors { get; set; } = [];
}

public sealed class VehicleErrorOutput
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}