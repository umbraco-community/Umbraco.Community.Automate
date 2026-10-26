using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.Community.Automate.DevTo.Content;

/// <summary>
/// Converts a Block List or Block Grid block to Markdown. Register implementations as singletons
/// to control how your own blocks appear on DEV:
/// <code>builder.Services.AddSingleton&lt;IDevToBlockConverter, MyCodeBlockConverter&gt;();</code>
/// Converters run in registration order, before the built-in conversion.
/// </summary>
public interface IDevToBlockConverter
{
    /// <summary>
    /// Returns the block as Markdown, an empty string to leave the block out, or <c>null</c>
    /// to let the next converter (and finally the built-in conversion) handle it.
    /// </summary>
    string? Convert(IPublishedElement content, IPublishedElement? settings, DevToConversionContext context);
}
