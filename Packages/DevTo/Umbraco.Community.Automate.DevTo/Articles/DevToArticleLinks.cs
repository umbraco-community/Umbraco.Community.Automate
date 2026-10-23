using System.Text.Json;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.Automate.DevTo.Articles;

/// <param name="Published">Whether the article was published on DEV when it was last posted or checked.</param>
/// <param name="ConnectionId">The Automate connection it was posted with, used to check on it later.</param>
/// <param name="Deleted">A check found the article is no longer on DEV.</param>
public sealed record DevToArticleLink(
    string? Culture,
    long ArticleId,
    string? Url,
    bool Published,
    DateTime PostedUtc,
    Guid? ConnectionId = null,
    bool Deleted = false,
    DateTime? CheckedUtc = null);

/// <summary>
/// Which DEV article each content item (and culture) was last posted as, for the document's Info
/// tab. Kept in Umbraco's key-value store, so no document type needs a property for it.
/// </summary>
public sealed class DevToArticleLinks
{
    private const string KeyPrefix = "UmbracoCommunityAutomateDevTo.Article+";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IKeyValueService _keyValueService;

    public DevToArticleLinks(IKeyValueService keyValueService) => _keyValueService = keyValueService;

    public void Save(Guid contentKey, DevToArticleLink link)
        => _keyValueService.SetValue(Key(contentKey, link.Culture), JsonSerializer.Serialize(link, JsonOptions));

    // The key-value store can't delete, so a removed link is blanked, which Get skips.
    public void Remove(Guid contentKey, string? culture) => _keyValueService.SetValue(Key(contentKey, culture), string.Empty);

    public IReadOnlyList<DevToArticleLink> Get(Guid contentKey)
        => (_keyValueService.FindByKeyPrefix(Key(contentKey, null)) ?? new Dictionary<string, string?>())
            .Values
            .Select(Deserialize)
            .OfType<DevToArticleLink>()
            .OrderBy(link => link.Culture)
            .ToArray();

    // Keys are fixed-length up to the culture, so one content item's prefix never matches another's.
    private static string Key(Guid contentKey, string? culture)
        => string.IsNullOrEmpty(culture) ? $"{KeyPrefix}{contentKey:D}" : $"{KeyPrefix}{contentKey:D}+{culture.ToLowerInvariant()}";

    private static DevToArticleLink? Deserialize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        try
        {
            return JsonSerializer.Deserialize<DevToArticleLink>(value, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
