using System.Reflection;
using System.Text.Json;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Conditions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.Automate.Demo.Setup;

/// <summary>
/// Gets a Demo database ready to use on startup, so a fresh one (or a reset) needs no setup:
/// <list type="number">
///   <item>an API user, as the demo workspace's service account;</item>
///   <item>one connection per package in the repo, pre-filled with its configuration references;</item>
///   <item>a "demo" workspace, allowed to use all of them;</item>
///   <item>
///   a "Test: X" automation per package, on a manual trigger — run it from the Automate
///   workspace ("Run now") to check its connection works, no content to publish.
///   </item>
/// </list>
/// Each step only adds what's missing, so it never touches anything you've changed.
/// </summary>
public class DemoSetupComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, DemoSetupHandler>();
}

public class DemoSetupHandler(
    IRuntimeState runtimeState,
    IUserService userService,
    ConnectionTypeCollection connectionTypes,
    IConnectionService connectionService,
    IWorkspaceService workspaceService,
    IAutomationService automationService,
    ILogger<DemoSetupHandler> logger) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    /// <summary>Fixed, so it's predictable in the database rather than a fresh GUID every reset.</summary>
    public static readonly Guid ApiUserKey = new("4fc20353-4d4f-4798-b936-bcb18008e152");

    private const string ApiUserName = "Automate API User";

    // Umbraco requires usernames to be email addresses by default.
    private const string ApiUserLogin = "automate-demo@example.com";

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        // Only once Umbraco is installed; on the first run, the unattended install finishes first.
        if (runtimeState.Level != RuntimeLevel.Run)
            return;

        await EnsureApiUserAsync();
        var connectionIds = await EnsureConnectionsAsync(cancellationToken);
        var workspace = await EnsureWorkspaceAsync(connectionIds, cancellationToken);
        await EnsureTestAutomationsAsync(workspace, connectionIds, cancellationToken);
    }

    private async Task EnsureApiUserAsync()
    {
        if (userService.GetByUsername(ApiUserLogin) is not null)
            return;

        var result = await userService.CreateAsync(
            Constants.Security.SuperUserKey,
            new UserCreateModel
            {
                Id = ApiUserKey,
                Kind = UserKind.Api,
                Name = ApiUserName,
                UserName = ApiUserLogin,
                Email = ApiUserLogin,
                UserGroupKeys = new HashSet<Guid> { Constants.Security.AdminGroupKey },
            },
            approveUser: true);

        if (result.Success)
            logger.LogInformation("Created the {Name} for Automate workspaces", ApiUserName);
        else
            logger.LogWarning("Couldn't create the Demo's API user for Automate workspaces: {Status}", result.Status);
    }

    /// <summary>
    /// One connection for each of the repo's connection types, aliased after the type (e.g.
    /// community.weatherApi becomes weatherApi) so it's easy to find under Automation → Settings
    /// → Connections. Its settings are the settings class's defaults: the configuration references
    /// the package pre-fills, which resolve to the placeholders in appsettings.Development.json, or
    /// your appsettings.Local.json. Required settings with no default (Skoda's VIN) get a
    /// placeholder, and existing connections missing one are given it: Automate validates every
    /// connection in a workspace when a step leaves it to pick the connection, so one invalid
    /// connection would fail unrelated steps.
    /// </summary>
    private async Task<Dictionary<string, Guid>> EnsureConnectionsAsync(CancellationToken cancellationToken)
    {
        var existing = (await connectionService.GetAllConnectionsAsync(cancellationToken))
            .ToDictionary(c => c.Alias, StringComparer.OrdinalIgnoreCase);

        var ids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in connectionTypes.Where(t => t.Alias.StartsWith("community.", StringComparison.Ordinal)))
        {
            var alias = type.Alias["community.".Length..].Replace('.', '-');
            if (existing.TryGetValue(alias, out var connection))
            {
                await FillMissingRequiredSettingsAsync(connection, type.SettingsType, cancellationToken);
                ids[alias] = connection.Id;
                continue;
            }

            var created = await connectionService.CreateConnectionAsync(
                new Connection { Alias = alias, Name = type.Name, Type = type.Alias, Settings = type.SettingsType is { } settingsType ? DefaultSettings(settingsType) : [] },
                Constants.Security.SuperUserKey,
                cancellationToken);
            ids[alias] = created.Id;
            logger.LogInformation("Created the {Name} connection ({Alias})", type.Name, alias);
        }

        return ids;
    }

    /// <summary>
    /// A single workspace for every automation in this Demo, running as the API user and allowed
    /// to use every connection above. Existing connections (e.g. from a package added later) are
    /// added to an already-created workspace without touching anything else about it.
    /// </summary>
    private async Task<Workspace> EnsureWorkspaceAsync(Dictionary<string, Guid> connectionIds, CancellationToken cancellationToken)
    {
        var workspace = await workspaceService.GetWorkspaceByAliasAsync(DemoWorkspaceAlias, cancellationToken);
        if (workspace is null)
        {
            workspace = await workspaceService.CreateWorkspaceAsync(
                new Workspace
                {
                    Alias = DemoWorkspaceAlias,
                    Name = "Demo",
                    ServiceAccountKey = ApiUserKey,
                    UserGroups = [Constants.Security.AdminGroupKey],
                    AllowedConnections = connectionIds.Values.ToList(),
                },
                Constants.Security.SuperUserKey,
                cancellationToken);
            logger.LogInformation("Created the {Name} workspace", workspace.Name);
            return workspace;
        }

        var missing = connectionIds.Values.Where(id => !workspace.AllowedConnections.Contains(id)).ToList();
        if (missing.Count == 0)
            return workspace;

        foreach (var id in missing)
            workspace.AllowedConnections.Add(id);
        workspace = await workspaceService.UpdateWorkspaceAsync(workspace, Constants.Security.SuperUserKey, cancellationToken);
        logger.LogInformation("Allowed {Count} new connection(s) in the {Name} workspace", missing.Count, workspace.Name);
        return workspace;
    }

    private const string DemoWorkspaceAlias = "demo";

    /// <summary>
    /// One "Test: X" automation per connection, so you can check your own credentials work from
    /// the backoffice: open the workspace, hit "Run now", then check the run's steps. A manual
    /// trigger is used on purpose — there's no real content event to tie this to, and a
    /// <c>contentPublished</c>-style trigger can't be run on demand (its payload has to carry a
    /// real node). Never updated once created, same as a connection, so your own edits stick.
    /// </summary>
    private async Task EnsureTestAutomationsAsync(Workspace workspace, Dictionary<string, Guid> connectionIds, CancellationToken cancellationToken)
    {
        foreach (var type in connectionTypes.Where(t => t.Alias.StartsWith("community.", StringComparison.Ordinal)))
        {
            var connectionAlias = type.Alias["community.".Length..].Replace('.', '-');
            if (!connectionIds.ContainsKey(connectionAlias))
                continue;

            var automationAlias = $"test-{connectionAlias}";
            if (await automationService.GetAutomationByAliasAsync(automationAlias, cancellationToken) is not null)
                continue;

            var testStepId = Guid.NewGuid();
            var ifStepId = Guid.NewGuid();
            var connectedStepId = Guid.NewGuid();
            var failedStepId = Guid.NewGuid();

            var automation = new Automation
            {
                Alias = automationAlias,
                Name = $"Test: {type.Name}",
                Description = $"Run it from here (\"Run now\") to check the {type.Name} connection works.",
                WorkspaceId = workspace.Id,
                Trigger = new TriggerConfiguration { TriggerAlias = "umbracoAutomate.manual" },
                Steps =
                [
                    new StepConfiguration
                    {
                        Id = testStepId,
                        ActionAlias = "community.testConnection",
                        Name = "Test Connection",
                        Alias = "testConnection",
                        ErrorBehavior = StepErrorBehavior.Terminate,
                        Position = new StepPosition { X = 250, Y = 230 },
                        Settings = new Dictionary<string, object?> { ["connectionAlias"] = connectionAlias },
                    },
                    new StepConfiguration
                    {
                        Id = ifStepId,
                        ActionAlias = "umbracoAutomate.if",
                        Name = "Did it connect?",
                        Alias = "didItConnect",
                        ErrorBehavior = StepErrorBehavior.Terminate,
                        Position = new StepPosition { X = 250, Y = 410 },
                        Settings = new Dictionary<string, object?>
                        {
                            ["conditions"] = new ConditionSet
                            {
                                Groups =
                                [
                                    new ConditionGroup
                                    {
                                        Conditions =
                                        [
                                            new Condition
                                            {
                                                LeftOperand = "${ steps.testConnection.connected }",
                                                Operator = ConditionOperator.Equals,
                                                RightOperand = "true",
                                            },
                                        ],
                                    },
                                ],
                            },
                        },
                    },
                    new StepConfiguration
                    {
                        Id = connectedStepId,
                        ActionAlias = "umbracoAutomate.logMessage",
                        Name = "Connection works",
                        Alias = "connectionWorks",
                        ErrorBehavior = StepErrorBehavior.Terminate,
                        Position = new StepPosition { X = 50, Y = 590 },
                        Settings = new Dictionary<string, object?>
                        {
                            ["message"] = "The connection works: ${ steps.testConnection.message }",
                            ["logLevel"] = "Information",
                        },
                    },
                    new StepConfiguration
                    {
                        Id = failedStepId,
                        ActionAlias = "umbracoAutomate.logMessage",
                        Name = "Connection failed",
                        Alias = "connectionFailed",
                        ErrorBehavior = StepErrorBehavior.Terminate,
                        Position = new StepPosition { X = 450, Y = 590 },
                        Settings = new Dictionary<string, object?>
                        {
                            ["message"] = "The connection didn't work: ${ steps.testConnection.message }",
                            ["logLevel"] = "Warning",
                        },
                    },
                ],
                Connections =
                [
                    new StepConnection { SourceStepId = Guid.Empty, TargetStepId = testStepId },
                    new StepConnection { SourceStepId = testStepId, TargetStepId = ifStepId },
                    new StepConnection { SourceStepId = ifStepId, SourceHandle = "true", TargetStepId = connectedStepId, Outcome = "true" },
                    new StepConnection { SourceStepId = ifStepId, SourceHandle = "false", TargetStepId = failedStepId, Outcome = "false" },
                ],
            };

            var created = await automationService.CreateAutomationAsync(automation, Constants.Security.SuperUserKey, cancellationToken);
            await automationService.PublishAutomationAsync(created.Id, Constants.Security.SuperUserKey, cancellationToken);
            logger.LogInformation("Created the {Name} automation", automation.Name);
        }
    }

    private async Task FillMissingRequiredSettingsAsync(Connection connection, Type? settingsType, CancellationToken cancellationToken)
    {
        if (settingsType is null)
            return;

        var missing = Fields(settingsType)
            .Where(IsRequiredText)
            .Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name))
            .Where(key => connection.Settings.GetValueOrDefault(key)?.ToString() is null or "")
            .ToList();
        if (missing.Count == 0)
            return;

        foreach (var key in missing)
            connection.Settings[key] = RequiredPlaceholder;
        await connectionService.UpdateConnectionAsync(connection, Constants.Security.SuperUserKey, cancellationToken);
        logger.LogInformation("Gave the {Alias} connection placeholder values for {Settings}", connection.Alias, string.Join(", ", missing));
    }

    private static Dictionary<string, object?> DefaultSettings(Type settingsType)
    {
        var defaults = Activator.CreateInstance(settingsType);
        var settings = new Dictionary<string, object?>();
        foreach (var property in Fields(settingsType))
        {
            var value = property.GetValue(defaults);
            var unset = value is null or "" || (property.PropertyType.IsValueType && value.Equals(Activator.CreateInstance(property.PropertyType)));
            if (!unset)
                settings[JsonNamingPolicy.CamelCase.ConvertName(property.Name)] = value;
            else if (IsRequiredText(property))
                settings[JsonNamingPolicy.CamelCase.ConvertName(property.Name)] = RequiredPlaceholder;
        }

        return settings;
    }

    // The same placeholder as appsettings.Development.json, so it's obvious it isn't a real value.
    private const string RequiredPlaceholder = "e2e-test";

    private static IEnumerable<PropertyInfo> Fields(Type settingsType)
        => settingsType.GetProperties().Where(p => p.GetCustomAttribute<EditableModelFieldAttribute>() is not null);

    // Automate treats a non-nullable string setting as required, as if it had [Required].
    private static bool IsRequiredText(PropertyInfo property)
        => property.PropertyType == typeof(string) && new NullabilityInfoContext().Create(property).WriteState != NullabilityState.Nullable;
}
