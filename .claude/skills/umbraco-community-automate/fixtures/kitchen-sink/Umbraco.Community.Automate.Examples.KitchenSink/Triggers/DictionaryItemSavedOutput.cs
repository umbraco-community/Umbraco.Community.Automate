namespace Umbraco.Community.Automate.Examples.KitchenSink.Triggers;

/// <summary>
/// What <see cref="DictionaryItemSavedTrigger"/> gives the automation's steps, in camelCase:
/// <c>${ trigger.key }</c> and <c>${ trigger.itemId }</c>.
/// </summary>
public sealed class DictionaryItemSavedOutput
{
    /// <summary>The dictionary item's key, e.g. "Footer.Copyright".</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>The dictionary item's unique id.</summary>
    public Guid ItemId { get; init; }
}
