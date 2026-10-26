using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Skoda.Actions;

public sealed class SetChargingLimitSettings
{
    [Field(
        Label = "Target state of charge",
        Description = "Target state of charge in percent. Vehicles typically accept values between 50 and 100 in steps of 10 and reject other values.",
        SupportsBindings = true)]
    public int TargetStateOfChargeInPercent { get; set; } = 80;
}
