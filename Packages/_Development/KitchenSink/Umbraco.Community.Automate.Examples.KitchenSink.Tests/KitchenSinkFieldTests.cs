using System.Reflection;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Examples.KitchenSink.Connections;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests;

/// <summary>
/// A setting without a Label or Description shows a raw localization key in the backoffice
/// (e.g. #uaFields_...Description) instead of failing anywhere, so this test catches it.
/// </summary>
public class KitchenSinkFieldTests
{
    [Fact]
    public void Every_setting_has_a_label_and_description()
    {
        var fields = typeof(KitchenSinkConnectionType).Assembly.GetTypes()
            .SelectMany(t => t.GetProperties())
            .Select(p => (Name: $"{p.DeclaringType!.Name}.{p.Name}", Field: p.GetCustomAttribute<EditableModelFieldAttribute>()))
            .Where(x => x.Field is not null)
            .ToList();

        Assert.NotEmpty(fields);
        Assert.All(fields, x =>
        {
            Assert.False(string.IsNullOrWhiteSpace(x.Field!.Label), $"{x.Name} has no Label.");
            Assert.False(string.IsNullOrWhiteSpace(x.Field.Description), $"{x.Name} has no Description.");
        });
    }
}
