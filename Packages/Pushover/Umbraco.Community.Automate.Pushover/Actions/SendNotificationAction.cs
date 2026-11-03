using System.Globalization;
using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Pushover.Api;
using Umbraco.Community.Automate.Pushover.Connections;

namespace Umbraco.Community.Automate.Pushover.Actions;

/// <summary>Sends a push notification through Pushover, with an optional title, link, sound and priority.</summary>
[Action("community.pushover.sendNotification", "Send Pushover Notification",
    Description = "Sends a push notification to a Pushover user or group.",
    ConnectionTypeAlias = "community.pushover",
    Group = "Notifications",
    Icon = "icon-automate-pushover")]
public sealed class SendNotificationAction(
    ActionInfrastructure infrastructure,
    PushoverClient client,
    ILogger<SendNotificationAction> logger)
    : ActionBase<SendNotificationSettings, SendNotificationOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        // 1. Check the inputs first and fail fast with a message the user can act on.
        var connection = context.Connection?.GetSettings<PushoverConnectionSettings>();
        if (PushoverConnectionSettings.Validate(connection) is { } connectionError)
            return ActionResult.Failed(new InvalidOperationException(connectionError), StepRunErrorCategory.ConfigurationError);

        var settings = context.GetSettings<SendNotificationSettings>();
        if (string.IsNullOrWhiteSpace(settings.Message))
            return ActionResult.Failed(new InvalidOperationException("A message is required."), StepRunErrorCategory.Validation);

        // 2. Call Pushover.
        try
        {
            var response = await client.SendMessageAsync(BuildFields(connection!, settings), cancellationToken);

            // Structured placeholders, and never the token or key.
            logger.LogInformation("Run {RunId}: sent a Pushover notification, request {RequestId}", context.RunId, response.Request);

            return Success(new SendNotificationOutput
            {
                Status = response.Status.ToString(CultureInfo.InvariantCulture),
                Request = response.Request ?? string.Empty,
            });
        }
        catch (PushoverApiException ex)
        {
            // The client already chose the category, which decides whether Automate retries.
            logger.LogWarning("Run {RunId}: Pushover rejected the notification: {Reason}", context.RunId, ex.Message);
            return ActionResult.Failed(ex, ex.Category);
        }
    }

    /// <summary>Pushover's form fields for the message, leaving out anything not set.</summary>
    internal static Dictionary<string, string> BuildFields(PushoverConnectionSettings connection, SendNotificationSettings settings)
    {
        var fields = new Dictionary<string, string>
        {
            ["token"] = connection.ApiToken,
            ["user"] = connection.UserKey,
            ["message"] = settings.Message,
        };

        if (!string.IsNullOrWhiteSpace(settings.Title))
            fields["title"] = settings.Title;

        // An uploaded custom sound takes priority over the Sound dropdown.
        var sound = !string.IsNullOrWhiteSpace(settings.CustomSound) ? settings.CustomSound : settings.Sound;
        if (!string.IsNullOrWhiteSpace(sound))
            fields["sound"] = sound;

        if (!string.IsNullOrWhiteSpace(settings.Url))
            fields["url"] = settings.Url;

        if (!string.IsNullOrWhiteSpace(settings.UrlTitle))
            fields["url_title"] = settings.UrlTitle;

        // Pushover's priority scale runs from -2 to 2; Default (0) is sent by leaving it out.
        var priority = settings.Priority switch
        {
            "Min" => -2,
            "Low" => -1,
            "High" => 1,
            "Max" => 2,
            _ => 0,
        };

        if (priority != 0)
            fields["priority"] = priority.ToString(CultureInfo.InvariantCulture);

        // Max (emergency) priority requires retry and expire.
        if (priority == 2)
        {
            fields["retry"] = connection.Retry.ToString(CultureInfo.InvariantCulture);
            fields["expire"] = connection.Expire.ToString(CultureInfo.InvariantCulture);
        }

        return fields;
    }
}
