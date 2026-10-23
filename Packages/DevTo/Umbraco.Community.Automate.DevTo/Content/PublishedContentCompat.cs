using System.Linq.Expressions;
using System.Reflection;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.Community.Automate.DevTo.Content;

/// <summary>
/// Reads members Umbraco 18 moved from <see cref="IPublishedContent"/> to <see cref="IPublishedElement"/>.
/// A direct call binds to the interface the package was compiled against and throws
/// <see cref="MissingMethodException"/> on the other major, so these bind at runtime instead.
/// </summary>
internal static class PublishedContentCompat
{
    private static readonly Func<IPublishedContent, string?> NameGetter = CreateGetter<string?>("Name");

    private static readonly Func<IPublishedContent, IReadOnlyDictionary<string, PublishedCultureInfo>> CulturesGetter =
        CreateGetter<IReadOnlyDictionary<string, PublishedCultureInfo>>("Cultures");

    public static string? GetName(IPublishedContent content, string? culture = null)
        => culture is not null && GetCultures(content).TryGetValue(culture, out var info) ? info.Name : NameGetter(content);

    public static IReadOnlyDictionary<string, PublishedCultureInfo> GetCultures(IPublishedContent content)
        => CulturesGetter(content);

    private static Func<IPublishedContent, T> CreateGetter<T>(string propertyName)
    {
        // GetProperty on an interface only sees its own members: IPublishedContent's on 17,
        // IPublishedElement's on 18.
        var property = typeof(IPublishedContent).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                       ?? typeof(IPublishedElement).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                       ?? throw new MissingMemberException(nameof(IPublishedContent), propertyName);

        var content = Expression.Parameter(typeof(IPublishedContent), "content");
        var read = Expression.Property(Expression.Convert(content, property.DeclaringType!), property);

        return Expression.Lambda<Func<IPublishedContent, T>>(Expression.Convert(read, typeof(T)), content).Compile();
    }
}
