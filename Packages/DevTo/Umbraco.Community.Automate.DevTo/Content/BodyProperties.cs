using System.Text.Json;

namespace Umbraco.Community.Automate.DevTo.Content;

/// <summary>
/// The Body Properties setting. The picker stores the document type the aliases were chosen from,
/// <c>{"documentType":"…","aliases":["intro","contentRows"]}</c>, so it can show their names and flag
/// renamed properties; typed values are a plain alias list, <c>intro, contentRows</c>.
/// </summary>
public sealed record BodyProperties(Guid? DocumentType, IReadOnlyList<string> Aliases)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static BodyProperties Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new(null, []);

        value = value.Trim();
        if (!value.StartsWith('{'))
            return new(null, Distinct(value.Split([',', ';', ' ', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));

        try
        {
            var stored = JsonSerializer.Deserialize<Stored>(value, JsonOptions);
            return new(stored?.DocumentType, Distinct((stored?.Aliases ?? []).Select(a => a?.Trim() ?? "")));
        }
        catch (JsonException)
        {
            return new(null, []);
        }
    }

    private static string[] Distinct(IEnumerable<string> aliases)
        => aliases.Where(a => a.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private sealed record Stored(Guid? DocumentType, string?[]? Aliases);
}
