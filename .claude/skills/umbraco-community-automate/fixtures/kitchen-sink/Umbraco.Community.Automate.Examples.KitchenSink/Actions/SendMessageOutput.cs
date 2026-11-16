namespace Umbraco.Community.Automate.Examples.KitchenSink.Actions;

/// <summary>
/// What <see cref="SendMessageAction"/> gives later steps, in camelCase:
/// <c>${ steps.&lt;alias&gt;.message }</c> and <c>${ steps.&lt;alias&gt;.url }</c>.
/// </summary>
public sealed class SendMessageOutput
{
    /// <summary>The message as the service received it.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>The URL the message was sent to.</summary>
    public string Url { get; init; } = string.Empty;
}
