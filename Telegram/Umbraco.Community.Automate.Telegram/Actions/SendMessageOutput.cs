namespace Umbraco.Community.Automate.Telegram.Actions;

/// <summary>
/// Output produced by the <see cref="SendMessageAction"/>.
/// </summary>
public sealed class SendMessageOutput
{
    public int MessageId { get; init; }
    public DateTimeOffset SentAt { get; init; }
}
