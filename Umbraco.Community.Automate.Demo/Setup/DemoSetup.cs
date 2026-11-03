using System.Reflection;
using System.Text.Json;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Settings;
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
///   <item>an API user, for use once automations and workspaces are added here;</item>
///   <item>one connection per package in the repo, pre-filled with its configuration references.</item>
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
    ILogger<DemoSetupHandler> logger) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    /// <summary>Fixed, so a future workspace in uSync/ can name it as its service account.</summary>
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
        await EnsureConnectionsAsync(cancellationToken);
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
    private async Task EnsureConnectionsAsync(CancellationToken cancellationToken)
    {
        var existing = (await connectionService.GetAllConnectionsAsync(cancellationToken))
            .ToDictionary(c => c.Alias, StringComparer.OrdinalIgnoreCase);

        foreach (var type in connectionTypes.Where(t => t.Alias.StartsWith("community.", StringComparison.Ordinal)))
        {
            var alias = type.Alias["community.".Length..].Replace('.', '-');
            if (existing.TryGetValue(alias, out var connection))
            {
                await FillMissingRequiredSettingsAsync(connection, type.SettingsType, cancellationToken);
                continue;
            }

            await connectionService.CreateConnectionAsync(
                new Connection { Alias = alias, Name = type.Name, Type = type.Alias, Settings = type.SettingsType is { } settingsType ? DefaultSettings(settingsType) : [] },
                Constants.Security.SuperUserKey,
                cancellationToken);
            logger.LogInformation("Created the {Name} connection ({Alias})", type.Name, alias);
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
