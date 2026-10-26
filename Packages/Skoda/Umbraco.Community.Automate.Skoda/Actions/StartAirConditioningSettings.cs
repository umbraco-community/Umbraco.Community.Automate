using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Skoda.Actions;

public sealed class StartAirConditioningSettings
{
    [Field(Label = "Target temperature", Description = "Target cabin temperature.", SupportsBindings = true)]
    public double TargetTemperature { get; set; } = 21;

    [Field(Label = "Temperature unit", Description = "CELSIUS or FAHRENHEIT.", SupportsBindings = true)]
    public string TemperatureUnit { get; set; } = "CELSIUS";

    [Field(Label = "Allow without external power", Description = "Allow air conditioning when no external power connection is available.", SupportsBindings = true)]
    public bool WithoutExternalPower { get; set; }
}