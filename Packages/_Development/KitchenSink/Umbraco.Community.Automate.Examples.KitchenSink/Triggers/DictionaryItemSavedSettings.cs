using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Triggers;

/// <summary>Settings for <see cref="DictionaryItemSavedTrigger"/>, chosen per automation.</summary>
public sealed class DictionaryItemSavedSettings
{
    [Field(
        Label = "Key starts with",
        Description = "Only run for dictionary items whose key starts with this, e.g. \"Footer.\". Leave blank to run for every item.",
        SortOrder = 0)]
    public string? KeyStartsWith { get; set; }
}
