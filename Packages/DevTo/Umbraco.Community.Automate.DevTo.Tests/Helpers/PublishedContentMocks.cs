using Moq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.Community.Automate.DevTo.Tests.Helpers;

internal static class PublishedContentMocks
{
    public static IPublishedProperty Property(
        string alias, string editorAlias, object? value, object? sourceValue = null, ContentVariation variation = ContentVariation.Nothing)
    {
        var propertyType = new Mock<IPublishedPropertyType>();
        propertyType.SetupGet(t => t.Alias).Returns(alias);
        propertyType.SetupGet(t => t.EditorAlias).Returns(editorAlias);
        propertyType.SetupGet(t => t.Variations).Returns(variation);

        var property = new Mock<IPublishedProperty>();
        property.SetupGet(p => p.Alias).Returns(alias);
        property.SetupGet(p => p.PropertyType).Returns(propertyType.Object);
        property.Setup(p => p.GetValue(It.IsAny<string?>(), It.IsAny<string?>())).Returns(value);
        property.Setup(p => p.GetSourceValue(It.IsAny<string?>(), It.IsAny<string?>())).Returns(sourceValue ?? value);
        return property.Object;
    }

    public static IPublishedProperty TextBox(string alias, string value) => Property(alias, "Umbraco.TextBox", value);

    public static IPublishedProperty TextArea(string alias, string value) => Property(alias, "Umbraco.TextArea", value);

    public static IPublishedElement Element(params IPublishedProperty[] properties)
        => Setup(new Mock<IPublishedElement>(), "element", properties).Object;

    public static IPublishedElement Element(string contentTypeAlias, params IPublishedProperty[] properties)
        => Setup(new Mock<IPublishedElement>(), contentTypeAlias, properties).Object;

    public static Mock<IPublishedContent> Document(params IPublishedProperty[] properties)
    {
        var content = Setup(new Mock<IPublishedContent>(), "blogPost", properties, PublishedItemType.Content);
        content.SetupGet(c => c.ItemType).Returns(PublishedItemType.Content);
        content.SetupGet(c => c.Key).Returns(Guid.NewGuid());
        content.SetupGet(c => c.Name).Returns("My Blog Post");
        content.SetupGet(c => c.Cultures).Returns(new Dictionary<string, PublishedCultureInfo>());
        return content;
    }

    public static IPublishedContent Media(string name, params IPublishedProperty[] properties)
    {
        var media = Setup(new Mock<IPublishedContent>(), "Image", properties, PublishedItemType.Media);
        media.SetupGet(m => m.ItemType).Returns(PublishedItemType.Media);
        media.SetupGet(m => m.Key).Returns(Guid.NewGuid());
        media.SetupGet(m => m.Name).Returns(name);
        return media.Object;
    }

    private static Mock<T> Setup<T>(
        Mock<T> element, string contentTypeAlias, IPublishedProperty[] properties, PublishedItemType itemType = PublishedItemType.Element)
        where T : class, IPublishedElement
    {
        var contentType = new Mock<IPublishedContentType>();
        contentType.SetupGet(t => t.Alias).Returns(contentTypeAlias);
        contentType.SetupGet(t => t.ItemType).Returns(itemType);
        contentType.SetupGet(t => t.Variations).Returns(ContentVariation.Nothing);

        element.SetupGet(e => e.ContentType).Returns(contentType.Object);
        element.SetupGet(e => e.Properties).Returns(properties);
        element.Setup(e => e.GetProperty(It.IsAny<string>()))
            .Returns<string>(alias => properties.FirstOrDefault(p => string.Equals(p.Alias, alias, StringComparison.OrdinalIgnoreCase)));
        return element;
    }
}
