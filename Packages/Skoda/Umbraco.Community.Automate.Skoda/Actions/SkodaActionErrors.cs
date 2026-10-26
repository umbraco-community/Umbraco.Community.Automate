using System.Text.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Skoda.Api;
using Umbraco.Community.Automate.Skoda.Configuration;
using Umbraco.Community.Automate.Skoda.Connections;

namespace Umbraco.Community.Automate.Skoda.Actions;

/// <summary>
/// The checks and error mapping every Škoda action shares, so they all fail the same way and
/// Automate can tell a failure worth retrying (rate limit, outage, timeout) from one the user must fix.
/// </summary>
internal static class SkodaActionErrors
{
    /// <summary>Returns a failure if the step has no usable Škoda connection, otherwise null.</summary>
    public static ActionResult? CheckConnection(ActionContext context, out SkodaConnectionSettings settings)
    {
        settings = context.Connection?.GetSettings<SkodaConnectionSettings>() ?? new SkodaConnectionSettings();

        var error = context.Connection is null ? "A Škoda connection is required."
            : string.IsNullOrWhiteSpace(settings.ApiKey) ? "The Škoda connection has no API key."
            // Automate resolves $-references before settings reach this code and reports a missing
            // key itself; this catches one that arrives unresolved anyway.
            : settings.ApiKey.TrimStart().StartsWith('$') ? $"The API key reference '{settings.ApiKey}' could not be resolved. Add the key to configuration at {SkodaConfiguration.SecretsPath}:ApiKey, or enter it on the connection."
            : string.IsNullOrWhiteSpace(settings.Vin) ? "The Škoda connection has no VIN."
            : null;

        return error is null ? null : ActionResult.Failed(new InvalidOperationException(error), StepRunErrorCategory.ConfigurationError);
    }

    /// <summary>Turns an exception from the Škoda API call into a categorised failure, or returns false to let it through.</summary>
    public static bool TryFail(Exception exception, CancellationToken cancellationToken, out ActionResult failure)
    {
        failure = exception switch
        {
            SkodaClientException ex => ActionResult.Failed(ex, SkodaClient.Classify(ex.StatusCode)),
            TaskCanceledException when !cancellationToken.IsCancellationRequested => ActionResult.Failed(exception, StepRunErrorCategory.Timeout),
            HttpRequestException => ActionResult.Failed(exception, StepRunErrorCategory.ServiceUnavailable),
            JsonException => ActionResult.Failed(exception, StepRunErrorCategory.InvalidResponse),
            _ => null!,
        };

        return failure is not null;
    }
}
