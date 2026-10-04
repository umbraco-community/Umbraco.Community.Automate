namespace Umbraco.Community.Automate.Examples.Simple.Actions;

/// <summary>
/// What <see cref="SendMessageAction"/> gives later steps, in camelCase:
/// <c>${ steps.&lt;alias&gt;.message }</c>, <c>${ steps.&lt;alias&gt;.url }</c> and
/// <c>${ steps.&lt;alias&gt;.receivedAt }</c>.
/// </summary>
public sealed class SendMessageOutput
{
    /// <summary>The message as the service received it (httpbin.org echoes it back).</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>The URL the message was posted to.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>When the service received it, from the response's Date header.</summary>
    public DateTimeOffset? ReceivedAt { get; init; }
}
