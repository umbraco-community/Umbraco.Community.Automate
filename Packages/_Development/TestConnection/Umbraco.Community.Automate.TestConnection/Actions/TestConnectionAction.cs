using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;

namespace Umbraco.Community.Automate.TestConnection.Actions;

/// <summary>
/// Runs a connection's own Test connection check from an automation, so an automation can tell
/// the editor whether a connection works. Always succeeds, with <see cref="TestConnectionOutput.Connected"/>
/// for an If step to branch on, because a failed check is the answer asked for, not an error.
/// </summary>
[Action("community.testConnection", "Test Connection",
    Description = "Runs a connection's Test connection check. Branch on its connected output with an If step.",
    Group = "Development",
    Icon = "icon-link")]
public sealed class TestConnectionAction(ActionInfrastructure infrastructure, IConnectionService connectionService)
    : ActionBase<TestConnectionSettings, TestConnectionOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<TestConnectionSettings>();
        var connection = await connectionService.GetConnectionByAliasAsync(settings.ConnectionAlias, cancellationToken);
        if (connection is null)
            return Success(new TestConnectionOutput { Message = $"There's no connection with the alias '{settings.ConnectionAlias}'." });

        var result = await connectionService.TestConnectionAsync(connection.Id, cancellationToken);
        if (result is null)
            return Success(new TestConnectionOutput { Message = "The connection couldn't be tested." });

        // A warning still counts as connected (e.g. a live check that's switched off); the message says why.
        return Success(new TestConnectionOutput
        {
            Connected = result.Status != ConnectionValidationStatus.Failure,
            Message = result.Message ?? result.Status.ToString(),
        });
    }
}
