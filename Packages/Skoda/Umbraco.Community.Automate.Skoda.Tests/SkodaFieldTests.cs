using System.Reflection;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Skoda.Connections;
using Xunit;

namespace Umbraco.Community.Automate.Skoda.Tests;

/// <summary>
/// A setting without a Label or Description shows a raw localization key in the backoffice
/// (e.g. #uaFields_...Description) instead of failing anywhere, so these tests catch it.
/// </summary>
public class SkodaFieldTests
{
    public static TheoryData<string> Fields()
    {
        var data = new TheoryData<string>();
        foreach (var property in typeof(SkodaConnectionType).Assembly.GetTypes().SelectMany(t => t.GetProperties()))
        {
            if (property.GetCustomAttribute<EditableModelFieldAttribute>() is not null)
                data.Add($"{property.DeclaringType!.Name}.{property.Name}");
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Fields))]
    public void Every_setting_has_a_label_and_description(string field)
    {
        var (typeName, propertyName) = (field.Split('.')[0], field.Split('.')[1]);
        var property = typeof(SkodaConnectionType).Assembly.GetTypes().Single(t => t.Name == typeName).GetProperty(propertyName)!;
        var attribute = property.GetCustomAttribute<EditableModelFieldAttribute>()!;

        Assert.False(string.IsNullOrWhiteSpace(attribute.Label), $"{field} has no Label.");
        Assert.False(string.IsNullOrWhiteSpace(attribute.Description), $"{field} has no Description.");
    }
}
