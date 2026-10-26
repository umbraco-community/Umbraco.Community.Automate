using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Skoda.Actions;

public sealed class UpdateChargingProfileSettings
{
    [Field(
        Label = "Charging profile ID",
        Description = "Identifier of the charging profile to update, as returned by Get Vehicle Status.",
        SupportsBindings = true)]
    public long ProfileId { get; set; }

    [Field(Label = "Name", Description = "Leave empty to keep the profile's current name.", SupportsBindings = true)]
    public string? Name { get; set; }

    [Field(
        Label = "Target state of charge",
        Description = "Target state of charge in percent. Leave empty to keep the profile's current value.",
        SupportsBindings = true)]
    public int? TargetStateOfChargeInPercent { get; set; }

    [Field(
        Label = "Max charging current (AC)",
        Description = "REDUCED or MAXIMUM. Leave empty to keep the profile's current value.",
        SupportsBindings = true)]
    public string? MaxChargingCurrent { get; set; }

    [Field(
        Label = "Auto unlock plug when charged",
        Description = "PERMANENT or OFF. Leave empty to keep the profile's current value.",
        SupportsBindings = true)]
    public string? AutoUnlockPlugWhenCharged { get; set; }
}
