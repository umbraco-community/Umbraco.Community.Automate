using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Skoda.Actions;

public sealed class SetChargeModeSettings
{
    [Field(
        Label = "Charge mode",
        Description = "MANUAL, TIMER, TIMER_CHARGING_WITH_CLIMATISATION, PREFERRED_CHARGING_TIMES, ONLY_OWN_CURRENT, IMMEDIATE_DISCHARGING or HOME_STORAGE_CHARGING.",
        SupportsBindings = true)]
    public string ChargeMode { get; set; } = "MANUAL";
}
