namespace Umbraco.Community.Automate.Pushover.Actions;

/// <summary>
/// What <see cref="SendNotificationAction"/> gives later steps, in camelCase:
/// <c>${ steps.&lt;alias&gt;.status }</c> and <c>${ steps.&lt;alias&gt;.request }</c>.
/// The names are unchanged from SA.Automate.Pushover, so existing bindings keep working.
/// </summary>
public sealed class SendNotificationOutput
{
    /// <summary>Pushover's status: "1" when the notification was accepted.</summary>
    public string? Status { get; init; }

    /// <summary>Pushover's ID for the request, useful when contacting its support.</summary>
    public string? Request { get; init; }
}
