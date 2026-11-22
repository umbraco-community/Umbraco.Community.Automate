using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Triggers;

/// <summary>
/// Starts an automation when a dictionary item is saved. A notification trigger is the simplest
/// kind: Umbraco raises the notification, <see cref="MapEvent"/> turns it into trigger events,
/// and <see cref="CanHandle"/> lets each automation filter them with its own settings.
/// </summary>
[Trigger("community.examples.kitchenSink.dictionaryItemSaved", "Dictionary Item Saved (Kitchen Sink Example)",
    Description = "Runs when a dictionary item is saved.",
    Group = "Examples",
    Icon = "icon-automate-kitchensink")]
public sealed class DictionaryItemSavedTrigger(TriggerInfrastructure infrastructure)
    : NotificationTriggerBase<DictionaryItemSavedSettings, DictionaryItemSavedOutput, DictionaryItemSavedNotification>(infrastructure)
{
    /// <summary>One save can include several items, so map each to its own event.</summary>
    public override IEnumerable<TriggerEvent> MapEvent(DictionaryItemSavedNotification notification)
        => notification.SavedEntities.Select(item => new TriggerEvent<DictionaryItemSavedOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = TriggerInitiatorType.User,
            Output = new DictionaryItemSavedOutput { Key = item.ItemKey, ItemId = item.Key },
            // The same save always produces the same key, so a duplicate notification is dropped
            // instead of running the automation twice.
            IdempotencyKey = GenerateIdempotencyKey(item.Key, 0, item.UpdateDate),
        });

    /// <summary>Called for each subscribed automation with its own settings.</summary>
    protected override bool CanHandle(DictionaryItemSavedOutput output, DictionaryItemSavedSettings? settings)
        => string.IsNullOrWhiteSpace(settings?.KeyStartsWith)
           || output.Key.StartsWith(settings.KeyStartsWith, StringComparison.OrdinalIgnoreCase);
}
