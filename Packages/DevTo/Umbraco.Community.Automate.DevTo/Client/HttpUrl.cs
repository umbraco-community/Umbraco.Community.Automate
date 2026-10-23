namespace Umbraco.Community.Automate.DevTo.Client;

internal static class HttpUrl
{
    public static bool TryCreate(string? value, out Uri uri)
    {
        if (Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
        {
            uri = parsed;
            return true;
        }

        uri = null!;
        return false;
    }

    public static bool IsValid(string? value) => TryCreate(value, out _);
}
