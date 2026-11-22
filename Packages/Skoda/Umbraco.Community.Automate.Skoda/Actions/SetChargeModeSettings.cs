using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Skoda.Actions;

public sealed class SetChargeModeSettings
{
    // Labels for people, values for the Škoda API. The values are stored in saved automations,
    // so they must not change.
    [Field(
        Label = "Charge mode",
        Description = "How the vehicle decides when to charge.",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = """
            [{ "alias": "items", "value": [
                { "name": "Manual", "value": "MANUAL" },
                { "name": "Timer", "value": "TIMER" },
                { "name": "Timer, with climatisation", "value": "TIMER_CHARGING_WITH_CLIMATISATION" },
                { "name": "Preferred charging times", "value": "PREFERRED_CHARGING_TIMES" },
                { "name": "Only own current (e.g. solar)", "value": "ONLY_OWN_CURRENT" },
                { "name": "Immediate discharging", "value": "IMMEDIATE_DISCHARGING" },
                { "name": "Home storage charging", "value": "HOME_STORAGE_CHARGING" }
            ] }]
            """)]
    public string ChargeMode { get; set; } = "MANUAL";
}
