using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.Automate.Examples.KitchenSink.Triggers;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Triggers;

public class DictionaryItemSavedTriggerTests
{
    private readonly DictionaryItemSavedTrigger _trigger = new(new TriggerInfrastructure(new UnusedModelResolver()));

    [Fact]
    public void Each_saved_item_becomes_an_event()
    {
        var first = new DictionaryItem("Footer.Copyright");
        var second = new DictionaryItem("Header.Title");

        var events = _trigger.MapEvent(new DictionaryItemSavedNotification([first, second], new EventMessages())).ToList();

        Assert.Equal(2, events.Count);
        var output = Assert.IsType<TriggerEvent<DictionaryItemSavedOutput>>(events[0]).Output;
        Assert.Equal("Footer.Copyright", output.Key);
        Assert.Equal(first.Key, output.ItemId);
        Assert.All(events, e => Assert.Equal("community.examples.kitchenSink.dictionaryItemSaved", e.TriggerAlias));
    }

    [Fact]
    public void The_same_save_always_gives_the_same_idempotency_key()
    {
        var item = new DictionaryItem("Footer.Copyright");
        var notification = new DictionaryItemSavedNotification(item, new EventMessages());

        var first = _trigger.MapEvent(notification).Single().IdempotencyKey;
        var second = _trigger.MapEvent(notification).Single().IdempotencyKey;

        Assert.False(string.IsNullOrEmpty(first));
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(null, "Footer.Copyright", true)]
    [InlineData("", "Footer.Copyright", true)]
    [InlineData("Footer.", "Footer.Copyright", true)]
    [InlineData("footer.", "Footer.Copyright", true)]
    [InlineData("Header.", "Footer.Copyright", false)]
    public void Key_filter_decides_which_automations_run(string? keyStartsWith, string key, bool expected)
    {
        // CanHandle is what Automate calls per automation, through the ITrigger interface.
        ITrigger trigger = _trigger;

        var handled = trigger.CanHandle(
            new DictionaryItemSavedOutput { Key = key },
            new DictionaryItemSavedSettings { KeyStartsWith = keyStartsWith });

        Assert.Equal(expected, handled);
    }

    /// <summary>The trigger never resolves models in these tests; this only satisfies the constructor.</summary>
    private sealed class UnusedModelResolver : IEditableModelResolver
    {
        object IEditableModelResolver.ResolveModel(string alias, Type modelType, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
        TModel IEditableModelResolver.ResolveModel<TModel>(string alias, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
    }
}
