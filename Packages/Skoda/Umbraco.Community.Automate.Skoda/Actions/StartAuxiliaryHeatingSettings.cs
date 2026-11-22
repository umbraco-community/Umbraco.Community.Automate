using System.ComponentModel.DataAnnotations;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Skoda.Configuration;

namespace Umbraco.Community.Automate.Skoda.Actions;

public sealed class StartAuxiliaryHeatingSettings
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

    // Treated like a password: encrypted at rest, masked in run logs, and by default read from
    // configuration so it never sits in the automation itself.
    [Required(ErrorMessage = "The S-PIN is required to start auxiliary heating.")]
    [Field(
        Label = "S-PIN",
        Description = "The vehicle's security PIN, set in the MyŠkoda app.",
        IsSensitive = true,
        SortOrder = 2)]
    public string Spin { get; set; } = SkodaConfiguration.SpinReference;

    [Range(1, 3600, ErrorMessage = "The duration must be between 1 and 3,600 seconds.")]
    [Field(
        Label = "Duration in seconds",
        Description = "How long to run, from 1 to 3,600 seconds. Defaults to 1,800 (30 minutes).",
        EditorUiAlias = "Umb.PropertyEditorUi.Integer",
        EditorConfig = """[{ "alias": "min", "value": 1 }, { "alias": "max", "value": 3600 }]""",
        SortOrder = 3)]
    public int DurationInSeconds { get; set; } = 1800;

    [Field(
        Label = "Start mode",
        Description = "Heat the cabin, or only ventilate it.",
        EditorUiAlias = "Umb.PropertyEditorUi.RadioButtonList",
        EditorConfig = """[{ "alias": "items", "value": [{ "name": "Heating", "value": "HEATING" }, { "name": "Ventilation", "value": "VENTILATION" }] }]""",
        SortOrder = 4)]
    public string StartMode { get; set; } = "HEATING";
}
