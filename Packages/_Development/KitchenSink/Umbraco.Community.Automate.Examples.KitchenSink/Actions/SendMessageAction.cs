using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;
using Umbraco.Community.Automate.Examples.KitchenSink.Connections;
using Umbraco.Community.Automate.Examples.KitchenSink.Models;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Actions;

/// <summary>
/// Sends a message to the service and returns what it received: the shape of most "create
/// something" actions (post to a channel, add a row, publish an article).
/// </summary>
[Action("community.examples.kitchenSink.sendMessage", "Send Example Message",
    Description = "Sends a message to httpbin.org and returns what it received.",
    ConnectionTypeAlias = "community.examples.kitchenSink",
    Group = "Examples",
    Icon = "icon-automate-kitchensink")]
public sealed class SendMessageAction(
    ActionInfrastructure infrastructure,
    KitchenSinkClient client,
    ILogger<SendMessageAction> logger)
    : ActionBase<SendMessageSettings, SendMessageOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        // 1. Validate the connection and settings first, failing fast with a message the user can act on.
        var connection = context.Connection?.GetSettings<KitchenSinkConnectionSettings>();
        if (KitchenSinkConnectionSettingsValidator.Validate(connection) is { } connectionError)
            return ActionResult.Failed(new InvalidOperationException(connectionError), StepRunErrorCategory.ConfigurationError);

        var settings = context.GetSettings<SendMessageSettings>();
        if (string.IsNullOrWhiteSpace(settings.Message))
            return ActionResult.Failed(new InvalidOperationException("A message is required."), StepRunErrorCategory.Validation);

        // 2. Call the service. The idempotency key is the same on a retry of this step, so the
        //    service can ignore the duplicate instead of posting twice.
        try
        {
            var response = await client.SendMessageAsync(
                connection!.ApiKey,
                new SendMessageRequest { Message = settings.Message },
                idempotencyKey: $"{context.RunId}:{context.StepId}",
                cancellationToken);

            // Structured placeholders, and never the API key.
            logger.LogInformation("Run {RunId}: sent a {Length}-character Example message", context.RunId, settings.Message.Length);

            // 3. Return a typed output for later steps.
            return Success(new SendMessageOutput { Message = response.Json?.Message ?? string.Empty, Url = response.Url });
        }
        catch (KitchenSinkApiException ex)
        {
            // The client already chose the category, which decides whether Automate retries.
            return ActionResult.Failed(ex, ex.Category);
        }
    }
}
