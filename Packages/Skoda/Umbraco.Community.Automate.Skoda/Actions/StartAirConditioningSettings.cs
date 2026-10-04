using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Skoda.Actions;

public sealed class StartAirConditioningSettings
{
    [Field(Label = "Target temperature", Description = "Target cabin temperature, in the unit below.", SortOrder = 0)]
    public double TargetTemperature { get; set; } = 21;

    [Field(
        Label = "Temperature unit",
        Description = "The unit the target temperature is in.",
        EditorUiAlias = "Umb.PropertyEditorUi.RadioButtonList",
        EditorConfig = """[{ "alias": "items", "value": [{ "name": "Celsius", "value": "CELSIUS" }, { "name": "Fahrenheit", "value": "FAHRENHEIT" }] }]""",
        SortOrder = 1)]
    public string TemperatureUnit { get; set; } = "CELSIUS";

    [Field(Label = "Allow without external power", Description = "Allow air conditioning when no external power connection is available.", SortOrder = 2)]
    public bool WithoutExternalPower { get; set; }
}
