using System.ComponentModel.DataAnnotations;
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Skoda.Actions;

public sealed class SetChargingLimitSettings
{
    [Range(50, 100, ErrorMessage = "The target state of charge must be between 50 and 100 percent.")]
    [Field(
        Label = "Target state of charge",
        Description = "Target state of charge in percent, from 50 to 100 in steps of 10. Vehicles reject other values.",
        EditorUiAlias = "Umb.PropertyEditorUi.Integer",
        EditorConfig = """[{ "alias": "min", "value": 50 }, { "alias": "max", "value": 100 }, { "alias": "step", "value": 10 }]""")]
    public int TargetStateOfChargeInPercent { get; set; } = 80;
}
