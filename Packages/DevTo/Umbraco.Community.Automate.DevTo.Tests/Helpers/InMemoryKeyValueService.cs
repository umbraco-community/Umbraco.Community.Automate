using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.Automate.DevTo.Tests.Helpers;

internal sealed class InMemoryKeyValueService : IKeyValueService
{
    public Dictionary<string, string?> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string? GetValue(string key) => Values.GetValueOrDefault(key);

    public IReadOnlyDictionary<string, string?>? FindByKeyPrefix(string keyPrefix)
        => Values.Where(v => v.Key.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase)).ToDictionary(v => v.Key, v => v.Value);

    public void SetValue(string key, string value) => Values[key] = value;

    public void SetValue(string key, string originValue, string newValue)
    {
        if (!TrySetValue(key, originValue, newValue))
            throw new InvalidOperationException($"'{key}' has changed.");
    }

    public bool TrySetValue(string key, string originValue, string newValue)
    {
        if (GetValue(key) != originValue)
            return false;

        Values[key] = newValue;
        return true;
    }
}
