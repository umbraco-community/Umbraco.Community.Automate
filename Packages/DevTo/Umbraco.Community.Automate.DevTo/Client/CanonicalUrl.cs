namespace Umbraco.Community.Automate.DevTo.Client;

/// <summary>
/// Compares canonical URLs the way a person would: scheme, host case and a trailing slash
/// don't make two URLs different pages.
/// </summary>
internal static class CanonicalUrl
{
    public static bool AreEquivalent(string? left, string? right)
    {
        var a = Normalise(left);
        return a is not null && a == Normalise(right);
    }

    public static string? Normalise(string? url)
    {
        if (!HttpUrl.TryCreate(url, out var uri))
            return null;

        var path = uri.AbsolutePath.TrimEnd('/');
        var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";

        return $"{uri.Host}{port}{path}{uri.Query}".ToLowerInvariant();
    }
}
