using Microsoft.Extensions.Logging;
using Umbraco.Community.Automate.Mastodon.Factory;
using Umbraco.Community.Automate.Mastodon.Settings;
using System.Net.Http.Json;
using Umbraco.Automate.Core.Actions;

namespace Umbraco.Community.Automate.Mastodon.Actions;

[Action("mastodonSendPost", "Send Mastodon Post",
    ConnectionTypeAlias = "mastodon",
    Description = "Sends a Mastodon Post",
    Icon = "icon-flash",
    Group = "Social Networks")]
public class SendMastodonPostAction : ActionBase<MastodonPostSettings>
{
    private readonly MastodonClientFactory _clientFactory;
    private readonly ILogger<SendMastodonPostAction> _logger;

    public SendMastodonPostAction(
        ActionInfrastructure infrastructure,
        MastodonClientFactory clientFactory,
        ILogger<SendMastodonPostAction> logger)
        : base(infrastructure)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var connectionSettings = context.Connection?.GetSettings<MastodonSettings>();

        if (connectionSettings is null)
        {
            return ActionResult.Failed(
                new InvalidOperationException("No Mastodon connection configured."),
                StepRunErrorCategory.ConfigurationError);
        }

        var connectionError = MastodonSettingsValidator.Validate(connectionSettings);
        if (connectionError is not null)
        {
            return ActionResult.Failed(
                new InvalidOperationException(connectionError),
                StepRunErrorCategory.ConfigurationError);
        }

        var settings = context.GetSettings<MastodonPostSettings>();

        if (string.IsNullOrWhiteSpace(settings.Content))
            return ActionResult.Failed(
                new InvalidOperationException("Post content is required."),
                StepRunErrorCategory.Validation);

        var payload = MastodonStatusRequest.FromSettings(settings);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{connectionSettings.InstanceUrl.TrimEnd('/')}/api/v1/statuses")
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Add("Idempotency-Key", $"{context.RunId}:{context.StepId}");

        _logger.LogInformation(
            "Posting to Mastodon instance {InstanceUrl}: {StatusLength} characters",
            connectionSettings.InstanceUrl, payload.Status.Length);

        using var client = _clientFactory.CreateClient(connectionSettings);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return ActionResult.Failed(
                    new InvalidOperationException($"Mastodon API returned {response.StatusCode}: {error}"),
                    StepRunErrorCategory.InvalidResponse);
            }

            return Success();
        }
        catch (HttpRequestException ex)
        {
            return ActionResult.Failed(
                new InvalidOperationException($"Could not reach {connectionSettings.InstanceUrl}: {ex.Message}", ex),
                StepRunErrorCategory.InvalidResponse);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ActionResult.Failed(
                new InvalidOperationException($"The request to {connectionSettings.InstanceUrl} timed out."),
                StepRunErrorCategory.InvalidResponse);
        }
    }
}
