using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Skoda.Actions;

public sealed class StartAuxiliaryHeatingSettings
{
    [Field(Label = "Target temperature", SupportsBindings = true)]
    public double TargetTemperature { get; set; } = 21;

    [Field(Label = "Temperature unit", Description = "CELSIUS or FAHRENHEIT.", SupportsBindings = true)]
    public string TemperatureUnit { get; set; } = "CELSIUS";

    [Field(Label = "S-PIN", Description = "Security PIN code.", SupportsBindings = true)]
    public string Spin { get; set; } = string.Empty;

    [Field(Label = "Duration in seconds", SupportsBindings = true)]
    public int DurationInSeconds { get; set; } = 1800;

    [Field(Label = "Start mode", Description = "HEATING or VENTILATION.", SupportsBindings = true)]
    public string StartMode { get; set; } = "HEATING";
}
