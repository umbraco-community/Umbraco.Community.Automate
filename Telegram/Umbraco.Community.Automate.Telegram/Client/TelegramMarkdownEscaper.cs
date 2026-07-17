using System.Text;

namespace Umbraco.Community.Automate.Telegram.Client;

/// <summary>
/// Escapes Telegram MarkdownV2 reserved characters in text this package constructs
/// (e.g. automation names, error messages interpolated into notification text).
/// </summary>
public static class TelegramMarkdownEscaper
{
    private const string ReservedCharacters = "_*[]()~`>#+-=|{}.!\\";

    public static string Escape(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (ReservedCharacters.Contains(c))
                builder.Append('\\');
            builder.Append(c);
        }
        return builder.ToString();
    }
}
