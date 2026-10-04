# Full connection: everything a package can have

Every file for a fully featured package, the shape of `Packages/_Development/KitchenSink/`: a connection with a shared API client, an action with an output, an action with outcomes, a trigger, a custom icon, a backoffice field editor, and tests for all of it.

Most packages need far less. Start from [simple.md](simple.md) (an API key and an action) unless the package needs something here, then take only the sections you need: each one says when it's worth having. Folders you don't need don't exist.

Replace `Example` / `example` with the area name (PascalCase / camelCase / lowercase as shown) and adjust to the service. If the real code in `Packages/_Development/KitchenSink/` disagrees with this file, the real code wins.

## Contents

- [Files](#files)
- [Package project](#package-project)
- [Directory.Build.props](#directorybuildprops)
- [Configuration](#configuration)
- [Connection settings, validator and type](#connection-settings-validator-and-type)
- [Api client and models](#api-client-and-models)
- [Action with an output](#action-with-an-output)
- [Action with outcomes](#action-with-outcomes)
- [Trigger](#trigger)
- [Composer](#composer)
- [Icons and umbraco-package.json](#icons-and-umbraco-packagejson)
- [Backoffice front end (Client/)](#backoffice-front-end-client)
- [Test project](#test-project)
- [Tests](#tests)
- [Wiring into the repo](#wiring-into-the-repo)
- [README skeleton](#readme-skeleton)

## Files

```
Packages/Example/
  Umbraco.Community.Automate.Example/
    Actions/
      CreatePostAction.cs, CreatePostSettings.cs, CreatePostOutput.cs     action with an output
      FindPostAction.cs, FindPostSettings.cs, FindPostOutput.cs           action with outcomes
    Api/
      ExampleClient.cs                    the only class that talks HTTP; maps errors to categories
      ExampleApiException.cs
    Client/                               backoffice front end source (Vite + Lit, npm)
      public/
        umbraco-package.json              registers the icons and the Vite bundle
        icons/icons.js, icons/example.icon.js
      src/
        manifests.ts
        message-editor/manifests.ts, message-editor.element.ts, message-editor.element.test.ts
        __mocks__/lit-element.js
      package.json, tsconfig.json, vite.config.ts, web-test-runner.config.mjs
    Composers/ExampleComposer.cs
    Configuration/ExampleConfiguration.cs
    Connections/
      ExampleConnectionSettings.cs, ExampleConnectionSettingsValidator.cs, ExampleConnectionType.cs
    Models/ExampleUser.cs, ExamplePost.cs
    Triggers/
      DictionaryItemSavedTrigger.cs, DictionaryItemSavedSettings.cs, DictionaryItemSavedOutput.cs
    wwwroot/                              build output of Client/ (gitignored)
    Directory.Build.props, README.md, community-automate-128.png
    Umbraco.Community.Automate.Example.csproj
  Umbraco.Community.Automate.Example.Tests/
    Actions/, Api/, Connections/, Triggers/, Fakes/   same folders as the package
    ExampleIconTests.cs
    Umbraco.Community.Automate.Example.Tests.csproj
```

## Package project

`Packages/Example/Umbraco.Community.Automate.Example/Umbraco.Community.Automate.Example.csproj`. The Razor SDK serves `wwwroot/` as static web assets under `/App_Plugins/UmbracoCommunityAutomateExample/`, which is how the icons and editor reach the backoffice.

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Title>Umbraco Community Automate Example</Title>
    <Description>Example connection, actions and trigger for Umbraco Automate. One or two sentences on what it lets automations do.</Description>
    <PackageTags>umbraco automate automation example umbraco-marketplace</PackageTags>
    <PackageIcon>community-automate-128.png</PackageIcon>
    <!-- Serves wwwroot/ (built from Client/ by npm) as static web assets under this path. -->
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    <StaticWebAssetBasePath>App_Plugins/UmbracoCommunityAutomateExample</StaticWebAssetBasePath>
  </PropertyGroup>

  <ItemGroup>
    <!-- Client/ is front-end source; only its build output in wwwroot/ ships. -->
    <Content Remove="Client\**" />
    <None Include="Client\public\umbraco-package.json" Pack="false" />
    <Folder Include="wwwroot\" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Umbraco.Automate.Core" />
    <PackageReference Include="Umbraco.Cms.Core" />
    <!-- Versions the package from its git tag (see Directory.Build.props). Without it every
         release packs as 1.0.0 and NuGet skips it as a duplicate. -->
    <PackageReference Include="MinVer" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Umbraco.Community.Automate.Example.Tests" />
  </ItemGroup>

  <ItemGroup>
    <!-- README.md is the NuGet / Umbraco Marketplace description. -->
    <None Include="README.md" Pack="true" PackagePath="" />
    <None Include="community-automate-128.png" Pack="true" PackagePath="" />
  </ItemGroup>

</Project>
```

Variations:
- **Custom icon but no `Client/`** (Mastodon, Skoda): drop the `Client\` item group, and put `umbraco-package.json` and `icons/` straight in `wwwroot/`, committed.
- **No static files at all** (WeatherApi, the Simple example): use `Microsoft.NET.Sdk` and drop `AddRazorSupportForMvc`, `StaticWebAssetBasePath` and the `Client\` item group.

Don't set `EnableDefaultContentItems` to `false`: the static web assets that carry `wwwroot/` into the package are found through default content items, so the package would ship without its icons and editors. `Content Remove="Client\**"` is what keeps the front-end source out. If you also ship a hand-written `buildTransitive` file, name it `<PackageId>.targets`: the build generates its own `<PackageId>.props` there and would replace yours.

No `Version` attributes anywhere: versions live in the root `Directory.Packages.props`. Copy `community-automate-128.png` from another package.

## Directory.Build.props

`Packages/Example/Umbraco.Community.Automate.Example/Directory.Build.props`. Besides the package metadata, this file is what makes CI find the package, and its tag prefix is how `release.yml` finds it.

```xml
<Project>
  <PropertyGroup>
    <Authors>Your Name, Umbraco Community</Authors>
    <PackageProjectUrl>https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Packages/Example/Umbraco.Community.Automate.Example</PackageProjectUrl>
    <RepositoryUrl>https://github.com/umbraco-community/Umbraco.Community.Automate</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <PropertyGroup>
    <!-- Releases come from example-v* tags only; height-based pre-release
         versions would react to other packages' commits in this monorepo. -->
    <MinVerTagPrefix>example-v</MinVerTagPrefix>
    <MinVerIgnoreHeight>true</MinVerIgnoreHeight>
  </PropertyGroup>

  <PropertyGroup>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>
</Project>
```

## Configuration

`Configuration/ExampleConfiguration.cs`. The one place the configuration paths, each credential's default reference and the service's fixed values are defined. Include `VariablesPath` only if the package has non-secret values in configuration (an instance URL, say).

```csharp
namespace Umbraco.Community.Automate.Example.Configuration;

/// <summary>
/// Where this package's values live in configuration. They sit under Umbraco Automate's shared
/// <c>Umbraco:Automate:Secrets</c> and <c>Umbraco:Automate:Variables</c> sections, which Automate
/// resolves <c>$</c> references from by default, so nothing needs registering.
/// </summary>
public static class ExampleConfiguration
{
    public const string VariablesPath = "Umbraco:Automate:Variables:Example";
    public const string SecretsPath = "Umbraco:Automate:Secrets:Example";

    /// <summary>The reference new connections start with, so a key in configuration is used without any typing.</summary>
    public const string ApiKeyReference = "$" + SecretsPath + ":ApiKey";

    // Fixed values: they're the same for every site, so they're constants here, not configuration.

    /// <summary>The service's API.</summary>
    public const string BaseUrl = "https://api.example.com/";
}
```

## Connection settings, validator and type

`Connections/ExampleConnectionSettings.cs`. The credential defaults to its configuration reference, so a new connection opens pre-filled.

```csharp
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Example.Configuration;

namespace Umbraco.Community.Automate.Example.Connections;

public sealed class ExampleConnectionSettings
{
    [Field(
        Label = "API key",
        Description = "Generate a key under Settings → API in Example.",
        IsSensitive = true,
        SortOrder = 0)]
    public string ApiKey { get; set; } = ExampleConfiguration.ApiKeyReference;
}
```

`Connections/ExampleConnectionSettingsValidator.cs`. Worth having once more than one action checks the connection: the connection type and every action share it, so they all report the same message.

```csharp
using Umbraco.Community.Automate.Example.Configuration;

namespace Umbraco.Community.Automate.Example.Connections;

public static class ExampleConnectionSettingsValidator
{
    /// <summary>Returns the first problem found, or null when the settings are valid.</summary>
    public static string? Validate(ExampleConnectionSettings? settings)
    {
        if (string.IsNullOrWhiteSpace(settings?.ApiKey))
            return "An API key is required.";

        // Automate resolves $-references before settings reach this code, and reports a missing
        // key itself; this is a safety net for a reference that arrives unresolved anyway.
        if (settings.ApiKey.TrimStart().StartsWith('$'))
            return $"The API key reference '{settings.ApiKey}' could not be resolved. Add the key to configuration at {ExampleConfiguration.SecretsPath}:ApiKey, or enter the key itself on the connection.";

        return null;
    }
}
```

`Connections/ExampleConnectionType.cs`

```csharp
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Example.Api;

namespace Umbraco.Community.Automate.Example.Connections;

[ConnectionType("community.example", "Example",
    Description = "Connects Umbraco Automate to Example.",
    Group = "Productivity",
    Icon = "icon-automate-example")]
public sealed class ExampleConnectionType(ConnectionTypeInfrastructure infrastructure, ExampleClient client)
    : ConnectionTypeBase<ExampleConnectionSettings>(infrastructure)
{
    public override async Task<ConnectionValidationResult> ValidateAsync(object? settings, CancellationToken cancellationToken)
    {
        var exampleSettings = settings as ExampleConnectionSettings;

        // Check the settings first, without calling the service.
        if (ExampleConnectionSettingsValidator.Validate(exampleSettings) is { } error)
            return ConnectionValidationResult.Failure(error);

        // Then make one cheap authenticated call.
        try
        {
            var account = await client.GetCurrentUserAsync(exampleSettings!, cancellationToken);
            return ConnectionValidationResult.Success($"Connected as @{account.Username}.");
        }
        catch (ExampleApiException ex)
        {
            return ConnectionValidationResult.Failure(ex.Message);
        }
    }
}
```

If each check costs the user API quota, make the live call opt-in with a boolean setting (see Skoda's `ValidateConnection`).

## Api client and models

Worth having once two or more places call the service: one class sends every request, so authentication, timeouts and the mapping from HTTP failures to `StepRunErrorCategory` live in one place, and every action reports errors the same way.

`Api/ExampleApiException.cs`

```csharp
using System.Net;
using Umbraco.Automate.Core.Actions;

namespace Umbraco.Community.Automate.Example.Api;

/// <summary>A failed call to the service, carrying the category actions pass to ActionResult.Failed.</summary>
public sealed class ExampleApiException(string message, StepRunErrorCategory category, HttpStatusCode? statusCode = null, Exception? inner = null)
    : Exception(message, inner)
{
    public StepRunErrorCategory Category { get; } = category;

    /// <summary>The HTTP status code, when the service responded at all.</summary>
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
```

`Api/ExampleClient.cs`

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Example.Configuration;
using Umbraco.Community.Automate.Example.Connections;
using Umbraco.Community.Automate.Example.Models;

namespace Umbraco.Community.Automate.Example.Api;

public sealed class ExampleClient(IHttpClientFactory httpClientFactory)
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    public Task<ExampleUser> GetCurrentUserAsync(ExampleConnectionSettings settings, CancellationToken cancellationToken)
        => SendAsync<ExampleUser>(settings, new HttpRequestMessage(HttpMethod.Get, "v1/me"), cancellationToken);

    /// <summary>Creates a post. The idempotency key stops a retried step from posting twice.</summary>
    public Task<ExamplePost> CreatePostAsync(ExampleConnectionSettings settings, string text, string idempotencyKey, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/posts") { Content = JsonContent.Create(new { text }) };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return SendAsync<ExamplePost>(settings, request, cancellationToken);
    }

    /// <summary>Gets a post, or null when there isn't one: a normal answer for a "find" action.</summary>
    public async Task<ExamplePost?> FindPostAsync(ExampleConnectionSettings settings, string id, CancellationToken cancellationToken)
    {
        try
        {
            return await SendAsync<ExamplePost>(settings, new HttpRequestMessage(HttpMethod.Get, $"v1/posts/{Uri.EscapeDataString(id)}"), cancellationToken);
        }
        catch (ExampleApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<T> SendAsync<T>(ExampleConnectionSettings settings, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(ExampleConfiguration.BaseUrl);
        client.Timeout = RequestTimeout;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        try
        {
            using (request)
            using (var response = await client.SendAsync(request, cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new ExampleApiException($"Example returned {(int)response.StatusCode}: {body}", Classify(response.StatusCode), response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                    ?? throw new ExampleApiException("Example returned an empty response.", StepRunErrorCategory.InvalidResponse, response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            throw new ExampleApiException($"Could not reach Example: {ex.Message}", StepRunErrorCategory.ServiceUnavailable, inner: ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExampleApiException("The request to Example timed out.", StepRunErrorCategory.Timeout, inner: ex);
        }
    }

    /// <summary>
    /// Automate decides whether to retry a step from its category: rate limits, timeouts and
    /// outages are temporary; the rest need the user to act.
    /// </summary>
    public static StepRunErrorCategory Classify(HttpStatusCode statusCode) => (int)statusCode switch
    {
        401 or 403 => StepRunErrorCategory.Authentication,
        400 or 404 or 422 => StepRunErrorCategory.Validation,
        408 => StepRunErrorCategory.Timeout,
        429 => StepRunErrorCategory.RateLimiting,
        >= 500 => StepRunErrorCategory.ServiceUnavailable,
        _ => StepRunErrorCategory.InvalidResponse,
    };
}
```

Request and response models go in the root `Models/` folder, one file each.

`Models/ExampleUser.cs`

```csharp
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Example.Models;

public sealed class ExampleUser
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}
```

`Models/ExamplePost.cs`

```csharp
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Example.Models;

public sealed class ExamplePost
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
```

## Action with an output

The "create something" shape: post to a channel, add a row, publish an article. It returns a typed output later steps can use.

`Actions/CreatePostSettings.cs`. `EditorUiAlias` swaps the default text box for the custom editor from [Backoffice front end](#backoffice-front-end-client); leave it out to keep the default.

```csharp
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Example.Actions;

public sealed class CreatePostSettings
{
    [Field(
        Label = "Text",
        Description = "The post text. Supports ${ binding } expressions.",
        SupportsBindings = true,
        EditorUiAlias = "UmbracoCommunityAutomateExample.PropertyEditorUi.Message",
        SortOrder = 0)]
    public string Text { get; set; } = string.Empty;
}
```

`Actions/CreatePostOutput.cs`. Properties reach later steps in camelCase: `${ steps.<alias>.url }`.

```csharp
namespace Umbraco.Community.Automate.Example.Actions;

public sealed class CreatePostOutput
{
    public string Id { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}
```

`Actions/CreatePostAction.cs`

```csharp
using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Example.Api;
using Umbraco.Community.Automate.Example.Connections;

namespace Umbraco.Community.Automate.Example.Actions;

[Action("community.example.createPost", "Create Example Post",
    Description = "Creates a post on Example.",
    ConnectionTypeAlias = "community.example",
    Group = "Productivity",
    Icon = "icon-automate-example")]
public sealed class CreatePostAction(
    ActionInfrastructure infrastructure,
    ExampleClient client,
    ILogger<CreatePostAction> logger)
    : ActionBase<CreatePostSettings, CreatePostOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        // 1. Validate the connection and settings first, failing fast with a message the user can act on.
        var connection = context.Connection?.GetSettings<ExampleConnectionSettings>();
        if (ExampleConnectionSettingsValidator.Validate(connection) is { } connectionError)
            return ActionResult.Failed(new InvalidOperationException(connectionError), StepRunErrorCategory.ConfigurationError);

        var settings = context.GetSettings<CreatePostSettings>();
        if (string.IsNullOrWhiteSpace(settings.Text))
            return ActionResult.Failed(new InvalidOperationException("Text is required."), StepRunErrorCategory.Validation);

        // 2. Call the service.
        try
        {
            var post = await client.CreatePostAsync(connection!, settings.Text, $"{context.RunId}:{context.StepId}", cancellationToken);

            // Structured placeholders, and never the API key.
            logger.LogInformation("Run {RunId}: created Example post {PostId}", context.RunId, post.Id);

            // 3. Return a typed output for later steps.
            return Success(new CreatePostOutput { Id = post.Id, Url = post.Url });
        }
        catch (ExampleApiException ex)
        {
            // The client already chose the category, which decides whether Automate retries.
            return ActionResult.Failed(ex, ex.Category);
        }
    }
}
```

## Action with outcomes

The "look something up" shape. When the answer can legitimately be "no" (not found, already exists), succeed with an **outcome** that later steps branch on, rather than failing. Outcome names are part of the action's contract: keep them as constants, document each in the README, and never rename them once released.

`Actions/FindPostSettings.cs`

```csharp
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Example.Actions;

public sealed class FindPostSettings
{
    [Field(
        Label = "Post ID",
        Description = "The ID of the post to find. Supports ${ binding } expressions, e.g. ${ steps.createPost.id }.",
        SupportsBindings = true,
        SortOrder = 0)]
    public string PostId { get; set; } = string.Empty;
}
```

`Actions/FindPostOutput.cs`

```csharp
namespace Umbraco.Community.Automate.Example.Actions;

public sealed class FindPostOutput
{
    public string Id { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
}
```

`Actions/FindPostAction.cs`

```csharp
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Example.Api;
using Umbraco.Community.Automate.Example.Connections;

namespace Umbraco.Community.Automate.Example.Actions;

[Action("community.example.findPost", "Find Example Post",
    Description = "Looks up a post on Example and branches on whether it exists.",
    ConnectionTypeAlias = "community.example",
    Group = "Productivity",
    Icon = "icon-automate-example")]
public sealed class FindPostAction(ActionInfrastructure infrastructure, ExampleClient client)
    : ActionBase<FindPostSettings, FindPostOutput>(infrastructure)
{
    /// <summary>The post exists; its details are in the output.</summary>
    public const string OutcomeFound = "found";

    /// <summary>There's no such post: a normal result to branch on, not an error.</summary>
    public const string OutcomeNotFound = "notFound";

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var connection = context.Connection?.GetSettings<ExampleConnectionSettings>();
        if (ExampleConnectionSettingsValidator.Validate(connection) is { } connectionError)
            return ActionResult.Failed(new InvalidOperationException(connectionError), StepRunErrorCategory.ConfigurationError);

        var settings = context.GetSettings<FindPostSettings>();
        if (string.IsNullOrWhiteSpace(settings.PostId))
            return ActionResult.Failed(new InvalidOperationException("A post ID is required."), StepRunErrorCategory.Validation);

        try
        {
            var post = await client.FindPostAsync(connection!, settings.PostId, cancellationToken);

            return post is null
                ? SuccessWithOutcome(OutcomeNotFound, new FindPostOutput())
                : SuccessWithOutcome(OutcomeFound, new FindPostOutput { Id = post.Id, Url = post.Url, Text = post.Text });
        }
        catch (ExampleApiException ex)
        {
            return ActionResult.Failed(ex, ex.Category);
        }
    }
}
```

## Trigger

A trigger starts an automation. The simplest kind is a **notification trigger**: Umbraco raises a notification, `MapEvent` turns it into trigger events, and `CanHandle` lets each automation filter them with its own settings. Automate finds the trigger from its attribute and subscribes to the notification for you, so nothing needs registering. A trigger needs no connection, so a trigger-only package has no `Connections/`.

Other kinds exist (`ScheduledTriggerBase` for CRON, `WebhookTriggerBase`, or raising events yourself with `ITriggerDispatcher`); reach for them only when a notification trigger can't do the job.

`Triggers/DictionaryItemSavedSettings.cs`. Chosen per automation.

```csharp
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Example.Triggers;

public sealed class DictionaryItemSavedSettings
{
    [Field(
        Label = "Key starts with",
        Description = "Only run for dictionary items whose key starts with this, e.g. \"Footer.\". Leave blank to run for every item.",
        SortOrder = 0)]
    public string? KeyStartsWith { get; set; }
}
```

`Triggers/DictionaryItemSavedOutput.cs`. Reaches the automation's steps in camelCase: `${ trigger.key }`.

```csharp
namespace Umbraco.Community.Automate.Example.Triggers;

public sealed class DictionaryItemSavedOutput
{
    /// <summary>The dictionary item's key, e.g. "Footer.Copyright".</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>The dictionary item's unique id.</summary>
    public Guid ItemId { get; init; }
}
```

`Triggers/DictionaryItemSavedTrigger.cs`. Note the generic order: settings, output, then the notification.

```csharp
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Community.Automate.Example.Triggers;

[Trigger("community.example.dictionaryItemSaved", "Dictionary Item Saved (Example)",
    Description = "Runs when a dictionary item is saved.",
    Group = "Productivity",
    Icon = "icon-automate-example")]
public sealed class DictionaryItemSavedTrigger(TriggerInfrastructure infrastructure)
    : NotificationTriggerBase<DictionaryItemSavedSettings, DictionaryItemSavedOutput, DictionaryItemSavedNotification>(infrastructure)
{
    /// <summary>One save can include several items, so map each to its own event.</summary>
    public override IEnumerable<TriggerEvent> MapEvent(DictionaryItemSavedNotification notification)
        => notification.SavedEntities.Select(item => new TriggerEvent<DictionaryItemSavedOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = TriggerInitiatorType.User,
            Output = new DictionaryItemSavedOutput { Key = item.ItemKey, ItemId = item.Key },
            // The same save always produces the same key, so a duplicate notification is dropped
            // instead of running the automation twice.
            IdempotencyKey = GenerateIdempotencyKey(item.Key, 0, item.UpdateDate),
        });

    /// <summary>Called for each subscribed automation with its own settings (which can be null).</summary>
    protected override bool CanHandle(DictionaryItemSavedOutput output, DictionaryItemSavedSettings? settings)
        => string.IsNullOrWhiteSpace(settings?.KeyStartsWith)
           || output.Key.StartsWith(settings.KeyStartsWith, StringComparison.OrdinalIgnoreCase);
}
```

## Composer

`Composers/ExampleComposer.cs`. Registers the package's services. Umbraco finds the composer on its own, and Automate finds the connection type, actions and triggers from their attributes.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Automate.Example.Api;

namespace Umbraco.Community.Automate.Example.Composers;

public class ExampleComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<ExampleClient>();

        // No connection type, action or trigger registration: Automate discovers classes with
        // [ConnectionType], [Action] and [Trigger] attributes on its own. Only services go here.
        // No icon or editor registration: Umbraco finds umbraco-package.json on its own.
        // No configuration registration: the default references live under Automate's shared
        // Umbraco:Automate:Secrets / Variables sections, which it resolves out of the box.
    }
}
```

## Icons and umbraco-package.json

Used for a commercial vendor's logo, found on the web (see Icons in SKILL.md for where to look and how to prepare the SVG); utility packages use a built-in Umbraco icon instead. Every package registers a custom icon the same way, with hand-written files and no C#: Umbraco discovers `umbraco-package.json` under `App_Plugins/` on its own.

```
Client/public/              or wwwroot/ in a package without a Client/ (then committed as-is)
  umbraco-package.json      the package manifest: the icons, plus the front-end bundle or other extensions
  icons/
    icons.js                lists the icons
    example.icon.js         one file per icon: the SVG markup
```

`Client/public/umbraco-package.json`. The `bundle` extension loads whatever Vite builds from `Client/src/`; leave it out in a package without a `Client/`. Other hand-written extensions (modals, localization; see DevTo) go in the same array.

```json
{
  "$schema": "https://json.schemastore.org/umbraco-package.json",
  "id": "Umbraco.Community.Automate.Example",
  "name": "Umbraco Community Automate Example",
  "allowTelemetry": true,
  "extensions": [
    {
      "type": "icons",
      "alias": "UmbracoCommunityAutomateExample.Icons",
      "name": "Example Icons",
      "js": "/App_Plugins/UmbracoCommunityAutomateExample/icons/icons.js"
    },
    {
      "type": "bundle",
      "alias": "UmbracoCommunityAutomateExample.Bundle",
      "name": "Umbraco Community Automate Example",
      "js": "/App_Plugins/UmbracoCommunityAutomateExample/umbraco-community-automate-example-manifests.js"
    }
  ]
}
```

The `/App_Plugins/UmbracoCommunityAutomateExample` prefix must match `StaticWebAssetBasePath` in the csproj. Leave out `version`, since nothing would keep it in step with the package version.

`Client/public/icons/icons.js`

```js
// Plain JS copied as-is by Vite to wwwroot/icons/, served at /App_Plugins/UmbracoCommunityAutomateExample/icons/.
// Registered by public/umbraco-package.json. Icon names must match the C# attributes' Icon.
export default [
    {
        name: "icon-automate-example",
        path: () => import("./example.icon.js"),
        keywords: ["example"],
    },
];
```

`Client/public/icons/example.icon.js` exports the SVG markup as a string. Icon SVGs are inlined into the backoffice page, so prefix any `id` inside the SVG (gradients, clip paths) with the area name; otherwise another package's icon with the same id can hijack it.

```js
export default `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="9"/></svg>`;
```

A wrong path or icon name fails silently (the icon is just blank), so add the icon test from [Tests](#tests).

## Backoffice front end (Client/)

Worth having only when a setting needs a better editor than the built-in fields (Google Sheets' column list, the Kitchen Sink's character count). `Client/` is always the npm front end: CI gives a package front-end jobs when `Client/package.json` exists. Vite builds `src/` into `../wwwroot/` and copies `public/` there unchanged; `wwwroot/` is gitignored.

`Client/package.json`. Pin Playwright to the version every other `Client/` uses, so they share one downloaded browser.

```json
{
    "name": "@umbraco-community-automate/example",
    "version": "1.0.0",
    "private": true,
    "type": "module",
    "scripts": {
        "watch": "tsc && vite build --watch",
        "build": "tsc && vite build",
        "test": "web-test-runner",
        "test:watch": "web-test-runner --watch"
    },
    "dependencies": {
        "@umbraco-cms/backoffice": "^17.4.0"
    },
    "devDependencies": {
        "@open-wc/testing": "^4.0.0",
        "@types/mocha": "^10.0.10",
        "@web/dev-server-esbuild": "^1.0.0",
        "@web/dev-server-import-maps": "^0.2.0",
        "@web/test-runner": "^0.18.0",
        "@web/test-runner-playwright": "^0.11.0",
        "playwright": "1.61.0",
        "typescript": "^5.9.2",
        "vite": "^7.3.2"
    },
    "overrides": {
        "playwright": "1.61.0",
        "playwright-core": "1.61.0"
    }
}
```

Run `npm install` once to create `package-lock.json`, and commit it: CI runs `npm ci`.

`Client/vite.config.ts`

```ts
import { defineConfig } from "vite";
import { resolve } from "path";

// Builds src/ into ../wwwroot, which the package serves at /App_Plugins/UmbracoCommunityAutomateExample/.
// public/ is copied over as-is: umbraco-package.json tells Umbraco to load the built bundle.
export default defineConfig({
    build: {
        lib: {
            entry: {
                "umbraco-community-automate-example-manifests": resolve(__dirname, "src/manifests.ts"),
            },
            formats: ["es"],
        },
        outDir: "../wwwroot",
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            // Umbraco provides these at runtime; never bundle them.
            external: [/^@umbraco/],
        },
    },
});
```

`Client/tsconfig.json`

```json
{
    "compilerOptions": {
        "target": "ES2020",
        "experimentalDecorators": true,
        "useDefineForClassFields": false,
        "module": "ESNext",
        "lib": ["ES2020", "DOM", "DOM.Iterable"],
        "skipLibCheck": true,

        "moduleResolution": "bundler",
        "allowImportingTsExtensions": true,
        "isolatedModules": true,
        "moduleDetection": "force",
        "noEmit": true,

        "strict": true,
        "noUnusedLocals": true,
        "noUnusedParameters": true,
        "noFallthroughCasesInSwitch": true,

        "types": ["@umbraco-cms/backoffice/extension-types", "mocha"]
    },
    "include": ["src"]
}
```

`Client/src/manifests.ts`. Everything Vite builds, loaded by the `bundle` extension. Icons stay as plain files in `public/icons/`, not here.

```ts
import { messageEditorManifests } from "./message-editor/manifests.js";

export const manifests: Array<UmbExtensionManifest> = [...messageEditorManifests];
```

`Client/src/message-editor/manifests.ts`. The alias is what the C# setting names as its `EditorUiAlias`.

```ts
const messageEditor: UmbExtensionManifest = {
    type: "propertyEditorUi",
    alias: "UmbracoCommunityAutomateExample.PropertyEditorUi.Message",
    name: "Example Message Editor",
    element: () => import("./message-editor.element.js"),
    meta: {
        label: "Example Message",
        icon: "icon-autofill",
        group: "Automate",
    },
};

export const messageEditorManifests: UmbExtensionManifest[] = [messageEditor];
```

`Client/src/message-editor/message-editor.element.ts`. The smallest useful editor: take `value`, render it, and raise `UmbChangeEvent` when it changes so the step's settings update. Prefix custom element names with `ua-<area>-`.

```ts
import { css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";

@customElement("ua-example-message-editor")
export class UaExampleMessageEditorElement extends UmbLitElement implements UmbPropertyEditorUiElement {
    @property({ type: String })
    value = "";

    #onInput(event: Event) {
        this.value = (event.target as HTMLTextAreaElement).value;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        const length = this.value?.length ?? 0;
        return html`
            <uui-textarea label="Message" .value=${this.value ?? ""} @input=${this.#onInput}></uui-textarea>
            <div class="count">${length} ${length === 1 ? "character" : "characters"}</div>
        `;
    }

    static override styles = css`
        :host {
            display: block;
        }
        .count {
            margin-top: var(--uui-size-space-2, 6px);
            color: var(--uui-color-text-alt, #68676b);
            font-size: var(--uui-type-small-size, 12px);
        }
    `;
}

export default UaExampleMessageEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "ua-example-message-editor": UaExampleMessageEditorElement;
    }
}
```

Front-end tests run in a real browser with web-test-runner.

`Client/web-test-runner.config.mjs`. Umbraco's backoffice modules are mapped to the real files where they're plain re-exports, and to small mocks where they need the whole backoffice running.

```js
import { esbuildPlugin } from "@web/dev-server-esbuild";
import { importMapsPlugin } from "@web/dev-server-import-maps";
import { playwrightLauncher } from "@web/test-runner-playwright";

export default {
    files: "src/**/*.test.ts",
    nodeResolve: true,
    browsers: [playwrightLauncher({ product: "chromium" })],
    plugins: [
        importMapsPlugin({
            inject: {
                importMap: {
                    imports: {
                        "@umbraco-cms/backoffice/external/lit":
                            "/node_modules/@umbraco-cms/backoffice/dist-cms/external/lit/index.js",
                        "@umbraco-cms/backoffice/event":
                            "/node_modules/@umbraco-cms/backoffice/dist-cms/packages/core/event/index.js",
                        "@umbraco-cms/backoffice/lit-element": "/src/__mocks__/lit-element.js",
                    },
                },
            },
        }),
        esbuildPlugin({ ts: true, tsconfig: "./tsconfig.json", target: "auto" }),
    ],
};
```

`Client/src/__mocks__/lit-element.js`

```js
import { LitElement } from "lit";

/** Test stand-in for UmbLitElement, whose context system needs the whole backoffice running. */
export class UmbLitElement extends LitElement {}
```

`Client/src/message-editor/message-editor.element.test.ts`

```ts
import { expect, fixture } from "@open-wc/testing";
import { html } from "lit";
import "./message-editor.element.js";
import type { UaExampleMessageEditorElement } from "./message-editor.element.js";

function count(element: UaExampleMessageEditorElement): string {
    return element.shadowRoot!.querySelector(".count")!.textContent!.trim();
}

describe("UaExampleMessageEditorElement", () => {
    let element: UaExampleMessageEditorElement;

    beforeEach(async () => {
        element = await fixture(html`<ua-example-message-editor></ua-example-message-editor>`);
    });

    it("counts the characters in the current value", async () => {
        element.value = "Hello";
        await element.updateComplete;

        expect(count(element)).to.equal("5 characters");
    });

    it("updates the value and raises a change event when the user types", async () => {
        let changes = 0;
        element.addEventListener("change", () => changes++);

        const textarea = element.shadowRoot!.querySelector("uui-textarea") as HTMLElement & { value: string };
        textarea.value = "Typed";
        textarea.dispatchEvent(new Event("input"));
        await element.updateComplete;

        expect(element.value).to.equal("Typed");
        expect(changes).to.equal(1);
    });
});
```

Build and test with `npm ci && npm run build && npm test` in `Client/`. The built `wwwroot/` is not committed, so add `Packages/Example/Umbraco.Community.Automate.Example/wwwroot/` to the root `.gitignore`, and build the front end before `dotnet pack` or running the Demo site.

## Test project

`Packages/Example/Umbraco.Community.Automate.Example.Tests/Umbraco.Community.Automate.Example.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <!-- Umbraco's own harness for running actions in tests (ActionTestHarness). -->
    <PackageReference Include="Umbraco.Automate.Testing" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Umbraco.Community.Automate.Example\Umbraco.Community.Automate.Example.csproj" />
  </ItemGroup>

</Project>
```

## Tests

xUnit only: `Assert` for assertions and hand-written fakes instead of a mocking library. Test each action's success path, each outcome, missing or invalid settings, and how API errors map to categories. Never call the real service. Tests use the same folders as the package.

`Fakes/StubHttp.cs`. Returns canned responses in order and records each request, and each body as it's sent (the code under test disposes requests straight after).

```csharp
using System.Net;
using System.Text;

namespace Umbraco.Community.Automate.Example.Tests.Fakes;

public sealed class StubHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new(responses);

    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>Each request's body, read as it's sent: callers dispose requests straight after.</summary>
    public List<string?> Bodies { get; } = [];

    public static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
        return _responses.Dequeue();
    }
}

public sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
```

`Fakes/UnusedModelResolver.cs`. Connection types and triggers need an `IEditableModelResolver` to construct, but the code under test never uses it. Explicit members avoid repeating the interface's generic constraint.

```csharp
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Example.Tests.Fakes;

public sealed class UnusedModelResolver : IEditableModelResolver
{
    object IEditableModelResolver.ResolveModel(string alias, Type modelType, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
    TModel IEditableModelResolver.ResolveModel<TModel>(string alias, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
}
```

`Connections/ExampleConnectionSettingsValidatorTests.cs`

```csharp
using Umbraco.Community.Automate.Example.Connections;
using Xunit;

namespace Umbraco.Community.Automate.Example.Tests.Connections;

public class ExampleConnectionSettingsValidatorTests
{
    [Fact]
    public void New_connections_default_to_the_configuration_reference()
        => Assert.Equal("$Umbraco:Automate:Secrets:Example:ApiKey", new ExampleConnectionSettings().ApiKey);

    [Fact]
    public void Missing_api_key_fails()
        => Assert.Equal("An API key is required.", ExampleConnectionSettingsValidator.Validate(new ExampleConnectionSettings { ApiKey = "" }));

    [Fact]
    public void Unresolved_reference_fails_with_a_hint()
        => Assert.Contains("could not be resolved",
            ExampleConnectionSettingsValidator.Validate(new ExampleConnectionSettings { ApiKey = "$Umbraco:Automate:Secrets:Example:ApiKey" }));

    [Fact]
    public void Valid_settings_pass()
        => Assert.Null(ExampleConnectionSettingsValidator.Validate(new ExampleConnectionSettings { ApiKey = "key" }));
}
```

`Connections/ExampleConnectionTypeTests.cs`

```csharp
using System.Net;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Example.Api;
using Umbraco.Community.Automate.Example.Connections;
using Umbraco.Community.Automate.Example.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Example.Tests.Connections;

public class ExampleConnectionTypeTests
{
    [Fact]
    public async Task Valid_key_connects_and_names_the_account()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "username": "ada" }"""));

        var result = await CreateSut(handler).ValidateAsync(new ExampleConnectionSettings { ApiKey = "key" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Success, result.Status);
        Assert.Contains("@ada", result.Message);
    }

    [Fact]
    public async Task Invalid_settings_fail_without_calling_the_service()
    {
        var handler = new StubHttpMessageHandler();

        var result = await CreateSut(handler).ValidateAsync(new ExampleConnectionSettings { ApiKey = "" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Empty(handler.Requests);
    }

    private static ExampleConnectionType CreateSut(HttpMessageHandler handler)
        => new(new ConnectionTypeInfrastructure(new UnusedModelResolver()), new ExampleClient(new StubHttpClientFactory(handler)));
}
```

`Api/ExampleClientTests.cs`

```csharp
using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Example.Api;
using Umbraco.Community.Automate.Example.Connections;
using Umbraco.Community.Automate.Example.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Example.Tests.Api;

public class ExampleClientTests
{
    private static readonly ExampleConnectionSettings Settings = new() { ApiKey = "key" };

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.TooManyRequests, StepRunErrorCategory.RateLimiting)]
    [InlineData(HttpStatusCode.BadGateway, StepRunErrorCategory.ServiceUnavailable)]
    public async Task Failures_are_classified(HttpStatusCode status, StepRunErrorCategory expected)
    {
        var client = new ExampleClient(new StubHttpClientFactory(new StubHttpMessageHandler(StubHttpMessageHandler.Json(status, "{}"))));

        var ex = await Assert.ThrowsAsync<ExampleApiException>(() => client.GetCurrentUserAsync(Settings, CancellationToken.None));

        Assert.Equal(expected, ex.Category);
    }

    [Fact]
    public async Task Missing_post_is_null_not_an_error()
    {
        var client = new ExampleClient(new StubHttpClientFactory(new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.NotFound, "{}"))));

        Assert.Null(await client.FindPostAsync(Settings, "42", CancellationToken.None));
    }
}
```

`Actions/CreatePostActionTests.cs`. `ActionTestHarness` runs an action end to end; give it every service the action's constructor asks for (an action that takes `IHttpClientFactory` directly gets `.WithService<IHttpClientFactory>(...)`).

```csharp
using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Example.Actions;
using Umbraco.Community.Automate.Example.Api;
using Umbraco.Community.Automate.Example.Connections;
using Umbraco.Community.Automate.Example.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Example.Tests.Actions;

public class CreatePostActionTests
{
    [Fact]
    public async Task Creates_the_post_and_returns_its_url()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "id": "1", "url": "https://example.com/1" }"""));

        var result = await Run(handler, "Hello");

        Assert.Equal(ActionResultStatus.Success, result.Status);
        Assert.Equal("https://example.com/1", Assert.IsType<CreatePostOutput>(result.OutputData).Url);
        Assert.True(handler.Requests.Single().Headers.Contains("Idempotency-Key"));
        Assert.Contains("Hello", handler.Bodies.Single());
    }

    [Fact]
    public async Task Missing_text_is_a_validation_error_and_calls_nothing()
    {
        var handler = new StubHttpMessageHandler();

        var result = await Run(handler, "");

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
        Assert.Empty(handler.Requests);
    }

    private static Task<ActionResult> Run(StubHttpMessageHandler handler, string text)
        => ActionTestHarness.For<CreatePostAction>()
            .WithService(new ExampleClient(new StubHttpClientFactory(handler)))
            .WithSettings(new CreatePostSettings { Text = text })
            .WithConnection("community.example", new ExampleConnectionSettings { ApiKey = "key" })
            .ExecuteAsync();
}
```

`Actions/FindPostActionTests.cs`. One test per outcome.

```csharp
using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Example.Actions;
using Umbraco.Community.Automate.Example.Api;
using Umbraco.Community.Automate.Example.Connections;
using Umbraco.Community.Automate.Example.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Example.Tests.Actions;

public class FindPostActionTests
{
    [Fact]
    public async Task Existing_post_gives_the_found_outcome()
    {
        var result = await Run(StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "id": "1", "text": "Hi" }"""));

        Assert.Equal(ActionResultStatus.Success, result.Status);
        Assert.Equal(FindPostAction.OutcomeFound, result.Outcome);
        Assert.Equal("Hi", Assert.IsType<FindPostOutput>(result.OutputData).Text);
    }

    [Fact]
    public async Task Missing_post_is_an_outcome_not_a_failure()
    {
        var result = await Run(StubHttpMessageHandler.Json(HttpStatusCode.NotFound, "{}"));

        Assert.Equal(ActionResultStatus.Success, result.Status);
        Assert.Equal(FindPostAction.OutcomeNotFound, result.Outcome);
    }

    [Fact]
    public async Task Server_error_is_a_retryable_failure()
    {
        var result = await Run(StubHttpMessageHandler.Json(HttpStatusCode.ServiceUnavailable, "{}"));

        Assert.Equal(ActionResultStatus.Failed, result.Status);
        Assert.Equal(StepRunErrorCategory.ServiceUnavailable, result.ErrorCategory);
    }

    private static Task<ActionResult> Run(HttpResponseMessage response)
        => ActionTestHarness.For<FindPostAction>()
            .WithService(new ExampleClient(new StubHttpClientFactory(new StubHttpMessageHandler(response))))
            .WithSettings(new FindPostSettings { PostId = "1" })
            .WithConnection("community.example", new ExampleConnectionSettings { ApiKey = "key" })
            .ExecuteAsync();
}
```

`Triggers/DictionaryItemSavedTriggerTests.cs`. `CanHandle` is protected; call it through the `ITrigger` interface, as Automate does.

```csharp
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.Automate.Example.Tests.Fakes;
using Umbraco.Community.Automate.Example.Triggers;
using Xunit;

namespace Umbraco.Community.Automate.Example.Tests.Triggers;

public class DictionaryItemSavedTriggerTests
{
    private readonly DictionaryItemSavedTrigger _trigger = new(new TriggerInfrastructure(new UnusedModelResolver()));

    [Fact]
    public void Each_saved_item_becomes_an_event()
    {
        var events = _trigger.MapEvent(new DictionaryItemSavedNotification(
            [new DictionaryItem("Footer.Copyright"), new DictionaryItem("Header.Title")], new EventMessages())).ToList();

        Assert.Equal(2, events.Count);
        Assert.Equal("Footer.Copyright", Assert.IsType<TriggerEvent<DictionaryItemSavedOutput>>(events[0]).Output.Key);
        Assert.All(events, e => Assert.Equal("community.example.dictionaryItemSaved", e.TriggerAlias));
    }

    [Fact]
    public void The_same_save_always_gives_the_same_idempotency_key()
    {
        var notification = new DictionaryItemSavedNotification(new DictionaryItem("Footer.Copyright"), new EventMessages());

        Assert.Equal(_trigger.MapEvent(notification).Single().IdempotencyKey, _trigger.MapEvent(notification).Single().IdempotencyKey);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("Footer.", true)]
    [InlineData("Header.", false)]
    public void Key_filter_decides_which_automations_run(string? keyStartsWith, bool expected)
    {
        ITrigger trigger = _trigger;

        Assert.Equal(expected, trigger.CanHandle(
            new DictionaryItemSavedOutput { Key = "Footer.Copyright" },
            new DictionaryItemSavedSettings { KeyStartsWith = keyStartsWith }));
    }
}
```

`ExampleIconTests.cs`. Nothing else checks that the manifest path, the icon files and the attributes' `Icon` agree. In a package without a `Client/`, read `wwwroot` instead of `Client/public`.

```csharp
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Community.Automate.Example.Connections;
using Xunit;

namespace Umbraco.Community.Automate.Example.Tests;

public class ExampleIconTests
{
    private static readonly string Public = Path.Combine(ProjectDirectory(), "Client", "public");

    [Fact]
    public void Manifest_registers_icons_from_files_that_exist()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Public, "umbraco-package.json")));
        var icons = Assert.Single(doc.RootElement.GetProperty("extensions").EnumerateArray(), e => e.GetProperty("type").GetString() == "icons");

        // Must match StaticWebAssetBasePath in the csproj, or the backoffice 404s on the module.
        Assert.Equal("/App_Plugins/UmbracoCommunityAutomateExample/icons/icons.js", icons.GetProperty("js").GetString());
        Assert.True(File.Exists(Path.Combine(Public, "icons", "icons.js")));
        Assert.True(File.Exists(Path.Combine(Public, "icons", "example.icon.js")));
    }

    [Fact]
    public void Every_connection_type_action_and_trigger_uses_a_registered_icon()
    {
        var registered = Regex.Matches(File.ReadAllText(Path.Combine(Public, "icons", "icons.js")), @"name:\s*""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .ToHashSet();

        var used = typeof(ExampleConnectionType).Assembly.GetTypes()
            .Select(t => t.GetCustomAttribute<ConnectionTypeAttribute>()?.Icon
                         ?? t.GetCustomAttribute<ActionAttribute>()?.Icon
                         ?? t.GetCustomAttribute<TriggerAttribute>()?.Icon)
            .OfType<string>()
            .Distinct()
            .ToList();

        Assert.NotEmpty(used);
        Assert.All(used, icon => Assert.Contains(icon, registered));
    }

    // The front-end source isn't copied to the test output, so read it from the project.
    private static string ProjectDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Umbraco.Community.Automate.Example");
            if (Directory.Exists(Path.Combine(candidate, "Client")))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate the Example project directory.");
    }
}
```

`ExampleFieldTests.cs`. Every package has this test: a setting without a `Label` or `Description` doesn't fail anywhere, it just shows a raw `#uaFields_...` localization key in the backoffice.

```csharp
using System.Reflection;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Example.Connections;
using Xunit;

namespace Umbraco.Community.Automate.Example.Tests;

/// <summary>
/// A setting without a Label or Description shows a raw localization key in the backoffice
/// (e.g. #uaFields_...Description) instead of failing anywhere, so this test catches it.
/// </summary>
public class ExampleFieldTests
{
    [Fact]
    public void Every_setting_has_a_label_and_description()
    {
        var fields = typeof(ExampleConnectionType).Assembly.GetTypes()
            .SelectMany(t => t.GetProperties())
            .Select(p => (Name: $"{p.DeclaringType!.Name}.{p.Name}", Field: p.GetCustomAttribute<EditableModelFieldAttribute>()))
            .Where(x => x.Field is not null)
            .ToList();

        Assert.NotEmpty(fields);
        Assert.All(fields, x =>
        {
            Assert.False(string.IsNullOrWhiteSpace(x.Field!.Label), $"{x.Name} has no Label.");
            Assert.False(string.IsNullOrWhiteSpace(x.Field.Description), $"{x.Name} has no Description.");
        });
    }
}
```

## Wiring into the repo

The same for every package, simple or full.

`Umbraco.Community.Automate.Demo.slnx`, alongside the other packages:

```xml
  <Folder Name="/Packages/Example/">
    <Project Path="Packages/Example/Umbraco.Community.Automate.Example.Tests/Umbraco.Community.Automate.Example.Tests.csproj" />
    <Project Path="Packages/Example/Umbraco.Community.Automate.Example/Umbraco.Community.Automate.Example.csproj" />
  </Folder>
```

`Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj`:

```xml
    <ProjectReference Include="..\Packages\Example\Umbraco.Community.Automate.Example\Umbraco.Community.Automate.Example.csproj" />
```

Every configuration key the package reads goes into the Demo site twice, under the same paths as `ExampleConfiguration`.

`Umbraco.Community.Automate.Demo/appsettings.Development.json`, under `Umbraco:Automate:Secrets` (and `Variables` for any non-secret values): obviously fake placeholders, so the site boots and new connections' references resolve without real credentials:

```json
"Example": {
  "ApiKey": "e2e-test"
}
```

`Umbraco.Community.Automate.Demo/appsettings.Local.example.json`, in the same place: the template contributors copy to the git-ignored `appsettings.Local.json` to try the package with real credentials. Use a non-empty `your-...` value, since an empty one would override the placeholder:

```json
"Example": { "ApiKey": "your-example-api-key" }
```

Root `README.md` packages table:

```markdown
| [Example](Packages/Example/Umbraco.Community.Automate.Example/README.md) | What it does, in one or two sentences. | Not yet on NuGet: [build from source](#using-a-package-before-its-on-nuget) |
```

With a `Client/`, also add its `wwwroot/` to the root `.gitignore` and the `npm ci && npm run build` step to the Demo site's first-run instructions in the README and CONTRIBUTING.

## README skeleton

The package README ships inside the NuGet package; write it for site developers installing it, with absolute URLs for anything outside the package folder.

```markdown
# Umbraco.Community.Automate.Example

An Example connection, actions and trigger for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate).

One or two sentences on what automations can do with it, e.g. "Create a post on Example when a blog post is published."

## Installation

    dotnet add package Umbraco.Community.Automate.Example

There's nothing to register in code: Umbraco loads the package on its own. Then follow **Setup** below.

## Setup

### 1. Get an API key
Where to find it in the service, step by step.

### 2. Store it in configuration
appsettings.json under Umbraco:Automate:Secrets:Example (and Variables:Example for anything non-secret),
the environment-variable equivalents (Umbraco__Automate__Secrets__Example__ApiKey), and user secrets.

### 3. Create the connection
1. Go to **Automation → Settings → Connections** and create a new **Example** connection.
2. The API key field is pre-filled with `$Umbraco:Automate:Secrets:Example:ApiKey`; keep it, or enter the key itself.
3. Click **Test connection**.

## Actions
A table of every action and every setting.

## Triggers
A table of every trigger, its settings and its output (${ trigger.<property> }).

## Outcomes and outputs
Each outcome, and each output as ${ steps.<alias>.<property> } with an example value.

## Troubleshooting
The error messages users will actually see, including Automate's "Configuration key '...' not found"
for a missing key, and what to do about each.

## Compatibility
| Package version | Umbraco Automate | Umbraco CMS |
|---|---|---|
| 1.x | 17.x – 18.x | 17.4 – 18.x |

One build supports both Umbraco 17 and 18: it's compiled against 17, and every change is tested on both, including running the 17 build on 18. Umbraco 19 isn't supported until it has been tested.

## Links
- [Source code](https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Packages/Example/Umbraco.Community.Automate.Example)
- [Report an issue](https://github.com/umbraco-community/Umbraco.Community.Automate/issues)
```
