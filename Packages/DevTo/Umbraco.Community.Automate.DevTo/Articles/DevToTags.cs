using System.Text;
using System.Text.Json;

namespace Umbraco.Community.Automate.DevTo.Articles;

/// <summary>
/// DEV tags are lowercase alphanumeric, and an article can have at most four.
/// </summary>
public static class DevToTags
{
    private const int MaxTags = 4;

    private static readonly char[] Separators = [',', ';', '#', '\n', '\r'];

    /// <summary>
    /// Reads tags from a bound value: free text (<c>"umbraco, dotnet"</c>), the JSON a Tags editor
    /// binds to (<c>["umbraco","dotnet"]</c>), content or tag pickers (each item's <c>name</c>), or
    /// any mix of them, e.g. <c>"umbraco, ${ steps.getContent.properties.tags }"</c>.
    /// </summary>
    public static IEnumerable<string> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var tags = new List<string>();
        var text = new StringBuilder();

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] is '[' or '{' && TryReadJson(value, i, out var end, out var jsonTags))
            {
                tags.AddRange(Split(text.ToString()));
                text.Clear();
                tags.AddRange(jsonTags);
                i = end;
                continue;
            }

            text.Append(value[i]);
        }

        tags.AddRange(Split(text.ToString()));
        return tags;
    }

    // Whitespace inside a tag isn't a separator: "Umbraco CMS" becomes umbracocms.
    private static IEnumerable<string> Split(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Normalises tags to DEV's rules, removes duplicates, and keeps the first <see cref="MaxTags"/>.
    /// </summary>
    public static IReadOnlyList<string> Normalise(IEnumerable<string?> tags)
        => tags
            .Select(NormaliseTag)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxTags)
            .ToArray();

    // Reads the JSON array or object starting at value[start], if it is one.
    private static bool TryReadJson(string value, int start, out int end, out List<string> tags)
    {
        end = -1;
        tags = [];

        var depth = 0;
        var inString = false;
        for (var i = start; i < value.Length; i++)
        {
            var c = value[i];
            if (inString)
            {
                if (c == '\\') i++;
                else if (c == '"') inString = false;
            }
            else if (c == '"') inString = true;
            else if (c is '[' or '{') depth++;
            else if (c is ']' or '}' && --depth == 0)
            {
                end = i;
                break;
            }
        }

        if (end < 0)
            return false; // Never closed, so it's just a bracket in the text.

        try
        {
            using var doc = JsonDocument.Parse(value.AsMemory(start, end - start + 1));
            Collect(doc.RootElement, tags);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void Collect(JsonElement element, List<string> tags)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                tags.Add(element.GetString()!);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    Collect(item, tags);
                break;
            case JsonValueKind.Object:
                var name = element.EnumerateObject()
                    .FirstOrDefault(p => p.Name.Equals("name", StringComparison.OrdinalIgnoreCase)).Value;
                if (name.ValueKind == JsonValueKind.String)
                    tags.Add(name.GetString()!);
                break;
        }
    }

    private static string NormaliseTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return string.Empty;

        var sb = new StringBuilder(tag.Length);
        foreach (var c in tag.Normalize(NormalizationForm.FormD))
        {
            var lower = char.ToLowerInvariant(c);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
                sb.Append(lower);
        }

        return sb.ToString();
    }
}
