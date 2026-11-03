# Full connection: everything a package can have

Every file for a fully featured package, the shape of the Kitchen Sink fixture at
`.claude/skills/umbraco-community-automate/fixtures/kitchen-sink/`: a connection with a shared
API client, an action with an output, an action with outcomes, a trigger, a custom icon, and
tests for all of it.

Most packages need far less. Start from [simple.md](simple.md) (an API key and an action) unless
the package needs something here, then take only the sections you need: each one says when it's
worth having. Folders you don't need don't exist.

Every code block below marked with a file path is generated from that real file in the fixture
(or, for the backoffice front end, from a real package's `Client/`) — see [Working
notes](../SKILL.md#working-notes) for how. Replace `KitchenSink` / `kitchenSink` (as shown) with
your own area name, and adjust to your service: if the real code disagrees with this file, the
real code wins, since this file is generated from it.

The fixture no longer has a custom backoffice editor (its one text field now uses a built-in
editor instead), so [Backoffice front end](#backoffice-front-end-client) points at a real
package's `Client/` instead of a fixture one.

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
Packages/<Area>/
  Umbraco.Community.Automate.<Area>/
    Actions/
      SendMessageAction.cs, SendMessageSettings.cs, SendMessageOutput.cs   action with an output
      CheckStatusAction.cs, CheckStatusSettings.cs, CheckStatusOutput.cs   action with outcomes
    Api/
      KitchenSinkClient.cs                the only class that talks HTTP; maps errors to categories
      KitchenSinkApiException.cs
    Composers/KitchenSinkComposer.cs
    Configuration/KitchenSinkConfiguration.cs
    Connections/
      KitchenSinkConnectionSettings.cs, KitchenSinkConnectionSettingsValidator.cs, KitchenSinkConnectionType.cs
    Models/AnythingResponse.cs, BearerResponse.cs, SendMessageRequest.cs
    Triggers/
      DictionaryItemSavedTrigger.cs, DictionaryItemSavedSettings.cs, DictionaryItemSavedOutput.cs
    wwwroot/                              umbraco-package.json and icons/ (committed, hand-written — no Client/ here)
    README.md, community-automate-128.png
    Umbraco.Community.Automate.<Area>.csproj
  Umbraco.Community.Automate.<Area>.Tests/
    Actions/, Api/, Connections/, Triggers/, Fakes/   same folders as the package
    <Area>FieldTests.cs, <Area>IconTests.cs
    Umbraco.Community.Automate.<Area>.Tests.csproj
```

A package with a custom backoffice editor adds `Client/` too (npm front-end source) and
`Directory.Build.props` (a real, releasable package needs one; the fixture doesn't — see
[Directory.Build.props](#directorybuildprops)). See [Backoffice front end](#backoffice-front-end-client).

## Package project

`Packages/<Area>/Umbraco.Community.Automate.<Area>/Umbraco.Community.Automate.<Area>.csproj`. A
real, releasable package needs metadata the fixture's own project file skips, since it's never
packed: `MinVer` (versions the package from its git tag), `InternalsVisibleTo` (for its test
project), and the `README.md`/icon `Pack` items (what ships inside the `.nupkg`). Use this as the
template for a new package:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Title>Umbraco Community Automate &lt;Area&gt;</Title>
    <Description>What it connects to and what automations can do with it, in one or two sentences.</Description>
    <PackageTags>umbraco automate automation umbraco-marketplace</PackageTags>
    <PackageIcon>community-automate-128.png</PackageIcon>
    <!-- Serves wwwroot/ as static web assets under this path. -->
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    <StaticWebAssetBasePath>App_Plugins/UmbracoCommunityAutomate&lt;Area&gt;</StaticWebAssetBasePath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Umbraco.Automate.Core" />
    <PackageReference Include="Umbraco.Cms.Core" />
    <!-- Versions the package from its git tag (see Directory.Build.props). Without it every
         release packs as 1.0.0 and NuGet skips it as a duplicate. -->
    <PackageReference Include="MinVer" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Umbraco.Community.Automate.&lt;Area&gt;.Tests" />
  </ItemGroup>

  <ItemGroup>
    <!-- README.md is the NuGet / Umbraco Marketplace description. -->
    <None Include="README.md" Pack="true" PackagePath="" />
    <None Include="community-automate-128.png" Pack="true" PackagePath="" />
  </ItemGroup>

</Project>
```

With a `Client/`, also add `<Content Remove="Client\**" />` and `<None Include="Client\public\umbraco-package.json" Pack="false" />` so the front-end source stays out of the package while its build output (`wwwroot/`) ships — see [Backoffice front end](#backoffice-front-end-client).

Variations:
- **Custom icon but no `Client/`** (the fixture, Mastodon, Skoda): drop the `Client\` item group, and put `umbraco-package.json` and `icons/` straight in `wwwroot/`, committed. This is what the fixture's own project file looks like:

`Umbraco.Community.Automate.Examples.KitchenSink/Umbraco.Community.Automate.Examples.KitchenSink.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Title>Umbraco Community Automate Kitchen Sink Example</Title>
    <Description>A reference connection for Umbraco Automate showing conventions this repo's other packages don't: outcomes and a trigger. Talks to httpbin.org.</Description>
    <!-- Serves wwwroot/ (umbraco-package.json and the icon) as static web assets under this path. -->
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    <StaticWebAssetBasePath>App_Plugins/UmbracoCommunityAutomateExamplesKitchenSink</StaticWebAssetBasePath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Umbraco.Automate.Core" />
    <PackageReference Include="Umbraco.Cms.Core" />
  </ItemGroup>

</Project>
```

- **No static files at all** (WeatherApi, [simple.md](simple.md)): use `Microsoft.NET.Sdk` and drop `AddRazorSupportForMvc`, `StaticWebAssetBasePath` and the `Client\` item group.

Don't set `EnableDefaultContentItems` to `false`: the static web assets that carry `wwwroot/` into the package are found through default content items, so the package would ship without its icons and editors. No `Version` attributes anywhere: versions live in the root `Directory.Packages.props`. Copy `community-automate-128.png` from another package.

## Directory.Build.props

`Packages/<Area>/Umbraco.Community.Automate.<Area>/Directory.Build.props`. Besides the package
metadata, this file is what makes CI find the package, and its tag prefix is how `release.yml`
finds it. (The fixture has none: it's never packed or released, and living outside `Packages/`
at a path CI's package discovery doesn't check anyway means it needs no marker to stay out of it.)

```xml
<Project>
  <PropertyGroup>
    <Authors>Your Name, Umbraco Community</Authors>
    <PackageProjectUrl>https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Packages/&lt;Area&gt;/Umbraco.Community.Automate.&lt;Area&gt;</PackageProjectUrl>
    <RepositoryUrl>https://github.com/umbraco-community/Umbraco.Community.Automate</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <PropertyGroup>
    <!-- Releases come from <area>-v* tags only; height-based pre-release
         versions would react to other packages' commits in this monorepo. -->
    <MinVerTagPrefix>&lt;area&gt;-v</MinVerTagPrefix>
    <MinVerIgnoreHeight>true</MinVerIgnoreHeight>
  </PropertyGroup>

  <PropertyGroup>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>
</Project>
```

## Configuration

`Configuration/KitchenSinkConfiguration.cs`. The one place the configuration paths, each
credential's default reference and the service's fixed values are defined. Include a
`VariablesPath` constant too if the package has non-secret values in configuration (an instance
URL, say) — the fixture doesn't need one.

`Umbraco.Community.Automate.Examples.KitchenSink/Configuration/KitchenSinkConfiguration.cs`
```csharp
namespace Umbraco.Community.Automate.Examples.KitchenSink.Configuration;

/// <summary>
/// The one place this package's configuration paths are defined. Values sit under Umbraco
/// Automate's shared <c>Umbraco:Automate:Secrets</c> (sensitive) section, which Automate resolves
/// <c>$</c> references from by default, so nothing needs registering. Nesting under the area
/// name keeps the package's keys together.
/// </summary>
public static class KitchenSinkConfiguration
{
    public const string SecretsPath = "Umbraco:Automate:Secrets:KitchenSink";

    /// <summary>
    /// The value a new connection's API key field starts with, so a key stored in configuration
    /// (appsettings, environment variables, user secrets) is used without typing anything.
    /// </summary>
    public const string ApiKeyReference = "$" + SecretsPath + ":ApiKey";

    // Fixed value: it's the same for every site, so it's a constant here rather than a setting
    // in appsettings. Keeping it in this class still gives one place to change it.

    /// <summary>The service's API. httpbin.org echoes requests and accepts any bearer token.</summary>
    public const string BaseUrl = "https://httpbin.org/";
}
```

## Connection settings, validator and type

`Connections/KitchenSinkConnectionSettings.cs`. The credential defaults to its configuration
reference, so a new connection opens pre-filled.

`Umbraco.Community.Automate.Examples.KitchenSink/Connections/KitchenSinkConnectionSettings.cs`
```csharp
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Examples.KitchenSink.Configuration;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Connections;

/// <summary>The settings stored on a Kitchen Sink example connection.</summary>
public sealed class KitchenSinkConnectionSettings
{
    // Defaults to a reference to configuration, so a new connection opens pre-filled and a key
    // stored in appsettings just works. The description doesn't repeat the path: the field
    // already shows it.
    [Field(
        Label = "API key",
        Description = "Any token works with httpbin.org; a real service would say where to get one.",
        IsSensitive = true,
        SortOrder = 0)]
    public string ApiKey { get; set; } = KitchenSinkConfiguration.ApiKeyReference;
}
```

`Connections/KitchenSinkConnectionSettingsValidator.cs`. Worth having once more than one action
checks the connection: the connection type and every action share it, so they all report the
same message.

`Umbraco.Community.Automate.Examples.KitchenSink/Connections/KitchenSinkConnectionSettingsValidator.cs`
```csharp
using Umbraco.Community.Automate.Examples.KitchenSink.Configuration;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Connections;

/// <summary>
/// Shared validation for <see cref="KitchenSinkConnectionSettings"/>, used by the connection type
/// (Test connection) and every action, so they all report the same message.
/// </summary>
public static class KitchenSinkConnectionSettingsValidator
{
    /// <summary>Returns the first problem found, or <c>null</c> when the settings are valid.</summary>
    public static string? Validate(KitchenSinkConnectionSettings? settings)
    {
        if (string.IsNullOrWhiteSpace(settings?.ApiKey))
            return "An API key is required.";

        // Automate resolves $-references before settings reach this code, and reports a missing
        // key itself; this is a safety net for a reference that arrives unresolved anyway.
        if (settings.ApiKey.TrimStart().StartsWith('$'))
            return $"The API key reference '{settings.ApiKey}' could not be resolved. Add the key to configuration at {KitchenSinkConfiguration.SecretsPath}:ApiKey, or enter it on the connection.";

        return null;
    }
}
```

`Umbraco.Community.Automate.Examples.KitchenSink/Connections/KitchenSinkConnectionType.cs`
```csharp
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Connections;

/// <summary>
/// The Kitchen Sink example connection: stores an API key and checks it when the user clicks
/// <b>Test connection</b>. The alias ("community.examples.kitchenSink") is stored in saved automations and set as each
/// action's ConnectionTypeAlias, so once released it must never change.
/// </summary>
[ConnectionType("community.examples.kitchenSink", "Kitchen Sink Example (httpbin.org)",
    Description = "A reference connection that talks to httpbin.org.",
    Group = "Examples",
    Icon = "icon-automate-kitchensink")]
public sealed class KitchenSinkConnectionType(ConnectionTypeInfrastructure infrastructure, KitchenSinkClient client)
    : ConnectionTypeBase<KitchenSinkConnectionSettings>(infrastructure)
{
    public override async Task<ConnectionValidationResult> ValidateAsync(object? settings, CancellationToken cancellationToken)
    {
        var typed = settings as KitchenSinkConnectionSettings;

        // Check the settings first, without calling the service.
        if (KitchenSinkConnectionSettingsValidator.Validate(typed) is { } error)
            return ConnectionValidationResult.Failure(error);

        // Then make one cheap authenticated call.
        try
        {
            var response = await client.CheckApiKeyAsync(typed!.ApiKey, cancellationToken);
            return response.Authenticated
                ? ConnectionValidationResult.Success("Connected to httpbin.org.")
                : ConnectionValidationResult.Failure("httpbin.org didn't accept the API key.");
        }
        catch (KitchenSinkApiException ex)
        {
            return ConnectionValidationResult.Failure(ex.Message);
        }
    }
}
```

If each check costs the user API quota, make the live call opt-in with a boolean setting (see Skoda's `ValidateConnection`).

## Api client and models

Worth having once two or more places call the service: one class sends every request, so
authentication, timeouts and the mapping from HTTP failures to `StepRunErrorCategory` live in
one place, and every action reports errors the same way.

`Umbraco.Community.Automate.Examples.KitchenSink/Api/KitchenSinkApiException.cs`
```csharp
using System.Net;
using Umbraco.Automate.Core.Actions;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Api;

/// <summary>
/// A failed call to the service, carrying the <see cref="StepRunErrorCategory"/> that actions
/// pass straight to <c>ActionResult.Failed</c>, so every action reports errors the same way.
/// </summary>
public sealed class KitchenSinkApiException(string message, StepRunErrorCategory category, HttpStatusCode? statusCode = null, Exception? inner = null)
    : Exception(message, inner)
{
    public StepRunErrorCategory Category { get; } = category;

    /// <summary>The HTTP status code, when the service responded at all.</summary>
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
```

`Umbraco.Community.Automate.Examples.KitchenSink/Api/KitchenSinkClient.cs`
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Examples.KitchenSink.Configuration;
using Umbraco.Community.Automate.Examples.KitchenSink.Models;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Api;

/// <summary>
/// The one place that talks HTTP to the service. Every failure becomes an
/// <see cref="KitchenSinkApiException"/> with a <see cref="StepRunErrorCategory"/>, so the connection
/// type and all actions classify errors the same way.
/// </summary>
public sealed class KitchenSinkClient(IHttpClientFactory httpClientFactory)
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    /// <summary>Checks the API key. httpbin.org accepts any bearer token, so this always succeeds with one.</summary>
    public Task<BearerResponse> CheckApiKeyAsync(string apiKey, CancellationToken cancellationToken)
        => SendAsync<BearerResponse>(apiKey, new HttpRequestMessage(HttpMethod.Get, "bearer"), cancellationToken);

    /// <summary>Posts a message. The idempotency key stops a retried step from posting twice.</summary>
    public Task<AnythingResponse> SendMessageAsync(string apiKey, SendMessageRequest message, string idempotencyKey, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "anything") { Content = JsonContent.Create(message) };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return SendAsync<AnythingResponse>(apiKey, request, cancellationToken);
    }

    /// <summary>Asks httpbin.org to answer with the given status code.</summary>
    public async Task<HttpStatusCode> GetStatusAsync(string apiKey, int statusCode, CancellationToken cancellationToken)
    {
        using var response = await SendRawAsync(apiKey, new HttpRequestMessage(HttpMethod.Get, $"status/{statusCode}"), cancellationToken);
        return response.StatusCode;
    }

    private async Task<T> SendAsync<T>(string apiKey, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await SendRawAsync(apiKey, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new KitchenSinkApiException(
                $"The service returned {(int)response.StatusCode}: {body}",
                Classify(response.StatusCode),
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new KitchenSinkApiException("The service returned an empty response.", StepRunErrorCategory.InvalidResponse, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendRawAsync(string apiKey, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(KitchenSinkConfiguration.BaseUrl);
        client.Timeout = RequestTimeout;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            return await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new KitchenSinkApiException($"Could not reach the service: {ex.Message}", StepRunErrorCategory.ServiceUnavailable, inner: ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new KitchenSinkApiException("The request to the service timed out.", StepRunErrorCategory.Timeout, inner: ex);
        }
    }

    /// <summary>
    /// Maps a failed status code to the category Umbraco Automate uses to decide whether to retry
    /// the step: rate limits, timeouts and outages are temporary; the rest need the user to act.
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

`Models/BearerResponse.cs`. Used to check the connection.

`Umbraco.Community.Automate.Examples.KitchenSink/Models/BearerResponse.cs`
```csharp
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Models;

/// <summary>httpbin.org's response to <c>GET /bearer</c>, used to test a connection.</summary>
public sealed class BearerResponse
{
    [JsonPropertyName("authenticated")]
    public bool Authenticated { get; set; }
}
```

`Models/SendMessageRequest.cs`. The body an action sends.

`Umbraco.Community.Automate.Examples.KitchenSink/Models/SendMessageRequest.cs`
```csharp
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Models;

/// <summary>The body the Send Message action posts to httpbin.org.</summary>
public sealed class SendMessageRequest
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
```

`Models/AnythingResponse.cs`. What the service sends back.

`Umbraco.Community.Automate.Examples.KitchenSink/Models/AnythingResponse.cs`
```csharp
using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Models;

/// <summary>
/// httpbin.org's response to <c>/anything</c>: it echoes the request back, which stands in for
/// a real service's "created" response.
/// </summary>
public sealed class AnythingResponse
{
    [JsonPropertyName("json")]
    public SendMessageRequest? Json { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
```

## Action with an output

The "create something" shape: post to a channel, add a row, publish an article. It returns a
typed output later steps can use.

`Actions/SendMessageSettings.cs`. `EditorUiAlias` here uses the built-in `Umb.PropertyEditorUi.TextArea`;
swap it for a custom editor's alias from [Backoffice front end](#backoffice-front-end-client) when one is needed.

`Umbraco.Community.Automate.Examples.KitchenSink/Actions/SendMessageSettings.cs`
```csharp
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Actions;

/// <summary>Settings for <see cref="SendMessageAction"/>.</summary>
public sealed class SendMessageSettings
{
    // SupportsBindings lets the value come from the trigger or an earlier step, e.g.
    // "${ trigger.key } was saved".
    [Field(
        Label = "Message",
        Description = "The message to send. Supports ${ binding } expressions.",
        SupportsBindings = true,
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = """[{ "alias": "rows", "value": 4 }]""",
        SortOrder = 0)]
    public string Message { get; set; } = string.Empty;
}
```

`Actions/SendMessageOutput.cs`. Properties reach later steps in camelCase: `${ steps.<alias>.url }`.

`Umbraco.Community.Automate.Examples.KitchenSink/Actions/SendMessageOutput.cs`
```csharp
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
```

`Umbraco.Community.Automate.Examples.KitchenSink/Actions/SendMessageAction.cs`
```csharp
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
```

## Action with outcomes

The "look something up" shape. When the answer can legitimately be "no" (not found, already
exists), succeed with an **outcome** that later steps branch on, rather than failing. Outcome
names are part of the action's contract: keep them as constants, document each in the README,
and never rename them once released.

`Umbraco.Community.Automate.Examples.KitchenSink/Actions/CheckStatusSettings.cs`
```csharp
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Actions;

/// <summary>Settings for <see cref="CheckStatusAction"/>.</summary>
public sealed class CheckStatusSettings
{
    [Field(
        Label = "Status code",
        Description = "The HTTP status httpbin.org should answer with, e.g. 200 or 404, to try each outcome.",
        SortOrder = 0)]
    public int StatusCode { get; set; } = 200;
}
```

`Umbraco.Community.Automate.Examples.KitchenSink/Actions/CheckStatusOutput.cs`
```csharp
namespace Umbraco.Community.Automate.Examples.KitchenSink.Actions;

/// <summary>What <see cref="CheckStatusAction"/> gives later steps: <c>${ steps.&lt;alias&gt;.statusCode }</c>.</summary>
public sealed class CheckStatusOutput
{
    public int StatusCode { get; init; }
}
```

`Umbraco.Community.Automate.Examples.KitchenSink/Actions/CheckStatusAction.cs`
```csharp
using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;
using Umbraco.Community.Automate.Examples.KitchenSink.Connections;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Actions;

/// <summary>
/// Shows outcomes: instead of failing when something isn't there, the action succeeds with an
/// outcome that later steps can branch on, like a "find" or "look up" action would.
/// </summary>
[Action("community.examples.kitchenSink.checkStatus", "Check Example Status",
    Description = "Asks httpbin.org for a status code and branches on the answer.",
    ConnectionTypeAlias = "community.examples.kitchenSink",
    Group = "Examples",
    Icon = "icon-automate-kitchensink")]
public sealed class CheckStatusAction(ActionInfrastructure infrastructure, KitchenSinkClient client)
    : ActionBase<CheckStatusSettings, CheckStatusOutput>(infrastructure)
{
    /// <summary>The service answered with a success status.</summary>
    public const string OutcomeAvailable = "available";

    /// <summary>The service answered 404: a normal result to branch on, not an error.</summary>
    public const string OutcomeNotFound = "notFound";

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var connection = context.Connection?.GetSettings<KitchenSinkConnectionSettings>();
        if (KitchenSinkConnectionSettingsValidator.Validate(connection) is { } connectionError)
            return ActionResult.Failed(new InvalidOperationException(connectionError), StepRunErrorCategory.ConfigurationError);

        var settings = context.GetSettings<CheckStatusSettings>();
        if (settings.StatusCode is < 100 or > 599)
            return ActionResult.Failed(new InvalidOperationException("Status code must be between 100 and 599."), StepRunErrorCategory.Validation);

        try
        {
            var status = await client.GetStatusAsync(connection!.ApiKey, settings.StatusCode, cancellationToken);
            var output = new CheckStatusOutput { StatusCode = (int)status };

            if ((int)status is >= 200 and < 300)
                return SuccessWithOutcome(OutcomeAvailable, output);

            if (status == HttpStatusCode.NotFound)
                return SuccessWithOutcome(OutcomeNotFound, output);

            // Anything else is a real failure, classified so Automate knows whether to retry.
            return ActionResult.Failed(
                new InvalidOperationException($"httpbin.org answered {(int)status}."),
                KitchenSinkClient.Classify(status));
        }
        catch (KitchenSinkApiException ex)
        {
            return ActionResult.Failed(ex, ex.Category);
        }
    }
}
```

## Trigger

A trigger starts an automation. The simplest kind is a **notification trigger**: Umbraco raises
a notification, `MapEvent` turns it into trigger events, and `CanHandle` lets each automation
filter them with its own settings. Automate finds the trigger from its attribute and subscribes
to the notification for you, so nothing needs registering. A trigger needs no connection, so a
trigger-only package has no `Connections/`.

Other kinds exist (`ScheduledTriggerBase` for CRON, `WebhookTriggerBase`, or raising events
yourself with `ITriggerDispatcher`); reach for them only when a notification trigger can't do the
job.

`Triggers/DictionaryItemSavedSettings.cs`. Chosen per automation.

`Umbraco.Community.Automate.Examples.KitchenSink/Triggers/DictionaryItemSavedSettings.cs`
```csharp
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Triggers;

/// <summary>Settings for <see cref="DictionaryItemSavedTrigger"/>, chosen per automation.</summary>
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

`Umbraco.Community.Automate.Examples.KitchenSink/Triggers/DictionaryItemSavedOutput.cs`
```csharp
namespace Umbraco.Community.Automate.Examples.KitchenSink.Triggers;

/// <summary>
/// What <see cref="DictionaryItemSavedTrigger"/> gives the automation's steps, in camelCase:
/// <c>${ trigger.key }</c> and <c>${ trigger.itemId }</c>.
/// </summary>
public sealed class DictionaryItemSavedOutput
{
    /// <summary>The dictionary item's key, e.g. "Footer.Copyright".</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>The dictionary item's unique id.</summary>
    public Guid ItemId { get; init; }
}
```

`Triggers/DictionaryItemSavedTrigger.cs`. Note the generic order: settings, output, then the notification.

`Umbraco.Community.Automate.Examples.KitchenSink/Triggers/DictionaryItemSavedTrigger.cs`
```csharp
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Triggers;

/// <summary>
/// Starts an automation when a dictionary item is saved. A notification trigger is the simplest
/// kind: Umbraco raises the notification, <see cref="MapEvent"/> turns it into trigger events,
/// and <see cref="CanHandle"/> lets each automation filter them with its own settings.
/// </summary>
[Trigger("community.examples.kitchenSink.dictionaryItemSaved", "Dictionary Item Saved (Kitchen Sink Example)",
    Description = "Runs when a dictionary item is saved.",
    Group = "Examples",
    Icon = "icon-automate-kitchensink")]
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

    /// <summary>Called for each subscribed automation with its own settings.</summary>
    protected override bool CanHandle(DictionaryItemSavedOutput output, DictionaryItemSavedSettings? settings)
        => string.IsNullOrWhiteSpace(settings?.KeyStartsWith)
           || output.Key.StartsWith(settings.KeyStartsWith, StringComparison.OrdinalIgnoreCase);
}
```

## Composer

`Composers/KitchenSinkComposer.cs`. Registers the package's services. Umbraco finds the composer
on its own, and Automate finds the connection type, actions and triggers from their attributes.

`Umbraco.Community.Automate.Examples.KitchenSink/Composers/KitchenSinkComposer.cs`
```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Composers;

/// <summary>
/// Registers the package's services. Umbraco finds this composer on its own, and Automate finds
/// the connection type, actions and trigger from their attributes, so sites only install the package.
/// </summary>
public class KitchenSinkComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<KitchenSinkClient>();

        // No connection type, action or trigger registration: Automate discovers classes with
        // [ConnectionType], [Action] and [Trigger] attributes on its own. Only services go here.
        // No configuration registration: the default API key reference lives under Automate's
        // shared Umbraco:Automate:Secrets section, which it resolves out of the box.
    }
}
```

## Icons and umbraco-package.json

Used for a commercial vendor's logo, found on the web (see Icons in SKILL.md for where to look
and how to prepare the SVG); utility packages use a built-in Umbraco icon instead. Every package
registers a custom icon the same way, with hand-written files and no C#: Umbraco discovers
`umbraco-package.json` under `App_Plugins/` on its own.

```
Client/public/              or wwwroot/ in a package without a Client/ (then committed as-is, as the fixture does)
  umbraco-package.json      the package manifest: the icons, plus the front-end bundle or other extensions
  icons/
    icons.js                lists the icons
    <area>.icon.js           one file per icon: the SVG markup
```

`wwwroot/umbraco-package.json`. No `bundle` extension here, since the fixture has no `Client/`;
add one (see [Backoffice front end](#backoffice-front-end-client)) when a package needs a custom editor.

`Umbraco.Community.Automate.Examples.KitchenSink/wwwroot/umbraco-package.json`
```json
{
  "$schema": "https://json.schemastore.org/umbraco-package.json",
  "id": "Umbraco.Community.Automate.Examples.KitchenSink",
  "name": "Umbraco Community Automate Kitchen Sink Example",
  "allowTelemetry": true,
  "extensions": [
    {
      "type": "icons",
      "alias": "UmbracoCommunityAutomateExamplesKitchenSink.Icons",
      "name": "Kitchen Sink Example Icons",
      "js": "/App_Plugins/UmbracoCommunityAutomateExamplesKitchenSink/icons/icons.js"
    }
  ]
}
```

The `/App_Plugins/UmbracoCommunityAutomateExamplesKitchenSink` prefix must match
`StaticWebAssetBasePath` in the csproj. Leave out `version`, since nothing would keep it in step
with the package version.

`Umbraco.Community.Automate.Examples.KitchenSink/wwwroot/icons/icons.js`
```js
// Hand-written static web asset, served at /App_Plugins/UmbracoCommunityAutomateExamplesKitchenSink/icons/.
// Registered by wwwroot/umbraco-package.json. Icon names must match the C# attributes' Icon.
export default [
    {
        name: "icon-automate-kitchensink",
        path: () => import("./kitchensink.icon.js"),
        keywords: ["example", "httpbin", "automate"],
    },
];
```

`wwwroot/icons/kitchensink.icon.js` exports the SVG markup as a string. Icon SVGs are inlined
into the backoffice page, so prefix any `id` inside the SVG (gradients, clip paths) with the area
name; otherwise another package's icon with the same id can hijack it. (This one has no ids.)

`Umbraco.Community.Automate.Examples.KitchenSink/wwwroot/icons/kitchensink.icon.js`
```js
// Icon SVGs are inlined into the backoffice page, so any id inside one (gradients, clip paths)
// must be prefixed with the area name to avoid clashing with another package's icon. This one has none.
export default `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M7 8l-4 4 4 4"/><path d="M17 8l4 4-4 4"/><path d="M14 4l-4 16"/></svg>`;
```

A wrong path or icon name fails silently (the icon is just blank), so add the icon test from [Tests](#tests).

## Backoffice front end (Client/)

Worth having only when a setting needs a better editor than the built-in fields. The fixture
used to demonstrate one here, but no longer does: its `SendMessageSettings.Message` field now
uses the built-in `Umb.PropertyEditorUi.TextArea` instead, so there's no custom editor left in
the fixture to generate this section from. **Google Sheets' `Client/`** (its column-list editor)
is the real, maintained package to copy for this instead — read its source directly rather than
a static copy here, so this section can't go stale.

`Client/` is always the npm front end: CI gives a package front-end jobs when `Client/package.json`
exists. Vite builds `src/` into `../wwwroot/` and copies `public/` there unchanged; `wwwroot/` is
gitignored. Pin Playwright to the version every other `Client/` uses (`"overrides": {
"playwright": "1.61.0", "playwright-core": "1.61.0" }` in `package.json`), so they share one
downloaded browser. Build and test with `npm ci && npm run build && npm test` in `Client/`. The
built `wwwroot/` is not committed, so add the package's `wwwroot/` to the root `.gitignore`, and
build the front end before `dotnet pack` or running the Demo site.

## Test project

`Packages/<Area>/Umbraco.Community.Automate.<Area>.Tests/Umbraco.Community.Automate.<Area>.Tests.csproj`.
This is what the fixture's own test project file looks like — a real package's is the same shape.

`Umbraco.Community.Automate.Examples.KitchenSink.Tests/Umbraco.Community.Automate.Examples.KitchenSink.Tests.csproj`
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
    <ProjectReference Include="..\Umbraco.Community.Automate.Examples.KitchenSink\Umbraco.Community.Automate.Examples.KitchenSink.csproj" />
  </ItemGroup>

</Project>
```

## Tests

xUnit only: `Assert` for assertions and hand-written fakes instead of a mocking library. Test
each action's success path, each outcome, missing or invalid settings, and how API errors map to
categories. Never call the real service. Tests use the same folders as the package.

`Fakes/StubHttp.cs`. Returns canned responses in order and records each request sent.

`Umbraco.Community.Automate.Examples.KitchenSink.Tests/Fakes/StubHttp.cs`
```csharp
using System.Net;
using System.Text;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Fakes;

/// <summary>
/// Returns canned responses in order and records every request sent. Hand-written instead of a
/// mocking library: it's a few lines, and every contributor can read it.
/// </summary>
public sealed class StubHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new(responses);

    public List<HttpRequestMessage> Requests { get; } = [];

    public static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(_responses.Dequeue());
    }
}

/// <summary>Hands out clients that use the stub handler.</summary>
public sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
```

`Fakes/UnusedModelResolver.cs`. Connection types and triggers need an `IEditableModelResolver` to
construct, but the code under test never uses it. Explicit members avoid repeating the
interface's generic constraint.

`Umbraco.Community.Automate.Examples.KitchenSink.Tests/Fakes/UnusedModelResolver.cs`
```csharp
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Fakes;

/// <summary>
/// Connection types and triggers need an IEditableModelResolver to construct, but neither
/// ValidateAsync nor MapEvent/CanHandle in these tests uses it. Explicit members avoid repeating
/// the interface's generic constraint.
/// </summary>
public sealed class UnusedModelResolver : IEditableModelResolver
{
    object IEditableModelResolver.ResolveModel(string alias, Type modelType, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
    TModel IEditableModelResolver.ResolveModel<TModel>(string alias, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
}
```

`Umbraco.Community.Automate.Examples.KitchenSink.Tests/Connections/KitchenSinkConnectionSettingsValidatorTests.cs`
```csharp
using Umbraco.Community.Automate.Examples.KitchenSink.Connections;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Connections;

public class KitchenSinkConnectionSettingsValidatorTests
{
    [Fact]
    public void New_connections_default_to_the_configuration_reference()
        => Assert.Equal("$Umbraco:Automate:Secrets:KitchenSink:ApiKey", new KitchenSinkConnectionSettings().ApiKey);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_api_key_fails(string apiKey)
        => Assert.Equal("An API key is required.", KitchenSinkConnectionSettingsValidator.Validate(new KitchenSinkConnectionSettings { ApiKey = apiKey }));

    [Fact]
    public void Missing_settings_fail()
        => Assert.Equal("An API key is required.", KitchenSinkConnectionSettingsValidator.Validate(null));

    [Fact]
    public void Unresolved_reference_names_the_configuration_key()
    {
        // A reference arriving unresolved (Automate normally resolves it, or reports the missing key, first).
        var error = KitchenSinkConnectionSettingsValidator.Validate(new KitchenSinkConnectionSettings());

        Assert.NotNull(error);
        Assert.Contains("Umbraco:Automate:Secrets:KitchenSink:ApiKey", error);
    }

    [Fact]
    public void Real_key_passes()
        => Assert.Null(KitchenSinkConnectionSettingsValidator.Validate(new KitchenSinkConnectionSettings { ApiKey = "token" }));
}
```

`Umbraco.Community.Automate.Examples.KitchenSink.Tests/Connections/KitchenSinkConnectionTypeTests.cs`
```csharp
using System.Net;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;
using Umbraco.Community.Automate.Examples.KitchenSink.Connections;
using Umbraco.Community.Automate.Examples.KitchenSink.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Connections;

public class KitchenSinkConnectionTypeTests
{
    [Fact]
    public async Task Valid_key_connects()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "authenticated": true }"""));

        var result = await CreateSut(handler).ValidateAsync(new KitchenSinkConnectionSettings { ApiKey = "token" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Success, result.Status);
        var request = handler.Requests.Single();
        Assert.EndsWith("/bearer", request.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer token", request.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task Invalid_settings_fail_without_calling_the_service()
    {
        var handler = new StubHttpMessageHandler();

        var result = await CreateSut(handler).ValidateAsync(new KitchenSinkConnectionSettings { ApiKey = "" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Rejected_key_fails_with_the_service_message()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, "denied"));

        var result = await CreateSut(handler).ValidateAsync(new KitchenSinkConnectionSettings { ApiKey = "token" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Contains("401", result.Message);
    }

    private static KitchenSinkConnectionType CreateSut(HttpMessageHandler handler)
        => new(new ConnectionTypeInfrastructure(new UnusedModelResolver()), new KitchenSinkClient(new StubHttpClientFactory(handler)));
}
```

`Umbraco.Community.Automate.Examples.KitchenSink.Tests/Api/KitchenSinkClientTests.cs`
```csharp
using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;
using Umbraco.Community.Automate.Examples.KitchenSink.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Api;

public class KitchenSinkClientTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.Forbidden, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.BadRequest, StepRunErrorCategory.Validation)]
    [InlineData(HttpStatusCode.TooManyRequests, StepRunErrorCategory.RateLimiting)]
    [InlineData(HttpStatusCode.RequestTimeout, StepRunErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.BadGateway, StepRunErrorCategory.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Conflict, StepRunErrorCategory.InvalidResponse)]
    public void Status_codes_map_to_retry_categories(HttpStatusCode status, StepRunErrorCategory expected)
        => Assert.Equal(expected, KitchenSinkClient.Classify(status));

    [Fact]
    public async Task Unreachable_service_is_retryable()
    {
        var client = new KitchenSinkClient(new StubHttpClientFactory(new ThrowingHandler()));

        var ex = await Assert.ThrowsAsync<KitchenSinkApiException>(() => client.CheckApiKeyAsync("token", CancellationToken.None));

        Assert.Equal(StepRunErrorCategory.ServiceUnavailable, ex.Category);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("No such host is known.");
    }
}
```

`Actions/SendMessageActionTests.cs`. `ActionTestHarness` runs an action end to end; give it every
service the action's constructor asks for (an action that takes `IHttpClientFactory` directly
gets `.WithService<IHttpClientFactory>(...)`).

`Umbraco.Community.Automate.Examples.KitchenSink.Tests/Actions/SendMessageActionTests.cs`
```csharp
using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Examples.KitchenSink.Actions;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;
using Umbraco.Community.Automate.Examples.KitchenSink.Connections;
using Umbraco.Community.Automate.Examples.KitchenSink.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Actions;

public class SendMessageActionTests
{
    [Fact]
    public async Task Sends_the_message_and_returns_what_the_service_received()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK,
            """{ "json": { "message": "Hello" }, "url": "https://httpbin.org/anything" }"""));

        var result = await Run(handler, new SendMessageSettings { Message = "Hello" });

        Assert.Equal(ActionResultStatus.Success, result.Status);
        var output = Assert.IsType<SendMessageOutput>(result.OutputData);
        Assert.Equal("Hello", output.Message);
        Assert.Equal("https://httpbin.org/anything", output.Url);

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.True(request.Headers.Contains("Idempotency-Key"));
    }

    [Fact]
    public async Task Blank_message_is_a_validation_error_and_calls_nothing()
    {
        var handler = new StubHttpMessageHandler();

        var result = await Run(handler, new SendMessageSettings { Message = " " });

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Rate_limiting_is_reported_so_Automate_can_retry()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, "slow down"));

        var result = await Run(handler, new SendMessageSettings { Message = "Hello" });

        Assert.Equal(StepRunErrorCategory.RateLimiting, result.ErrorCategory);
    }

    private static Task<ActionResult> Run(StubHttpMessageHandler handler, SendMessageSettings settings)
        => ActionTestHarness.For<SendMessageAction>()
            .WithService(new KitchenSinkClient(new StubHttpClientFactory(handler)))
            .WithSettings(settings)
            .WithConnection("community.examples.kitchenSink", new KitchenSinkConnectionSettings { ApiKey = "token" })
            .ExecuteAsync();
}
```

`Actions/CheckStatusActionTests.cs`. One test per outcome.

`Umbraco.Community.Automate.Examples.KitchenSink.Tests/Actions/CheckStatusActionTests.cs`
```csharp
using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Examples.KitchenSink.Actions;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;
using Umbraco.Community.Automate.Examples.KitchenSink.Connections;
using Umbraco.Community.Automate.Examples.KitchenSink.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Actions;

public class CheckStatusActionTests
{
    [Fact]
    public async Task Success_status_gives_the_available_outcome()
    {
        var result = await Run(HttpStatusCode.OK, 200);

        Assert.Equal(ActionResultStatus.Success, result.Status);
        Assert.Equal(CheckStatusAction.OutcomeAvailable, result.Outcome);
        Assert.Equal(200, Assert.IsType<CheckStatusOutput>(result.OutputData).StatusCode);
    }

    [Fact]
    public async Task Not_found_is_an_outcome_not_a_failure()
    {
        var result = await Run(HttpStatusCode.NotFound, 404);

        Assert.Equal(ActionResultStatus.Success, result.Status);
        Assert.Equal(CheckStatusAction.OutcomeNotFound, result.Outcome);
    }

    [Fact]
    public async Task Server_error_is_a_retryable_failure()
    {
        var result = await Run(HttpStatusCode.ServiceUnavailable, 503);

        Assert.Equal(ActionResultStatus.Failed, result.Status);
        Assert.Equal(StepRunErrorCategory.ServiceUnavailable, result.ErrorCategory);
    }

    [Fact]
    public async Task Out_of_range_status_code_is_a_validation_error()
    {
        var handler = new StubHttpMessageHandler();

        var result = await ActionTestHarness.For<CheckStatusAction>()
            .WithService(new KitchenSinkClient(new StubHttpClientFactory(handler)))
            .WithSettings(new CheckStatusSettings { StatusCode = 42 })
            .WithConnection("community.examples.kitchenSink", new KitchenSinkConnectionSettings { ApiKey = "token" })
            .ExecuteAsync();

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
        Assert.Empty(handler.Requests);
    }

    private static Task<ActionResult> Run(HttpStatusCode answer, int requested)
        => ActionTestHarness.For<CheckStatusAction>()
            .WithService(new KitchenSinkClient(new StubHttpClientFactory(new StubHttpMessageHandler(new HttpResponseMessage(answer)))))
            .WithSettings(new CheckStatusSettings { StatusCode = requested })
            .WithConnection("community.examples.kitchenSink", new KitchenSinkConnectionSettings { ApiKey = "token" })
            .ExecuteAsync();
}
```

`Triggers/DictionaryItemSavedTriggerTests.cs`. `CanHandle` is protected; call it through the `ITrigger` interface, as Automate does.

`Umbraco.Community.Automate.Examples.KitchenSink.Tests/Triggers/DictionaryItemSavedTriggerTests.cs`
```csharp
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.Automate.Examples.KitchenSink.Tests.Fakes;
using Umbraco.Community.Automate.Examples.KitchenSink.Triggers;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Triggers;

public class DictionaryItemSavedTriggerTests
{
    private readonly DictionaryItemSavedTrigger _trigger = new(new TriggerInfrastructure(new UnusedModelResolver()));

    [Fact]
    public void Each_saved_item_becomes_an_event()
    {
        var first = new DictionaryItem("Footer.Copyright");
        var second = new DictionaryItem("Header.Title");

        var events = _trigger.MapEvent(new DictionaryItemSavedNotification([first, second], new EventMessages())).ToList();

        Assert.Equal(2, events.Count);
        var output = Assert.IsType<TriggerEvent<DictionaryItemSavedOutput>>(events[0]).Output;
        Assert.Equal("Footer.Copyright", output.Key);
        Assert.Equal(first.Key, output.ItemId);
        Assert.All(events, e => Assert.Equal("community.examples.kitchenSink.dictionaryItemSaved", e.TriggerAlias));
    }

    [Fact]
    public void The_same_save_always_gives_the_same_idempotency_key()
    {
        var item = new DictionaryItem("Footer.Copyright");
        var notification = new DictionaryItemSavedNotification(item, new EventMessages());

        var first = _trigger.MapEvent(notification).Single().IdempotencyKey;
        var second = _trigger.MapEvent(notification).Single().IdempotencyKey;

        Assert.False(string.IsNullOrEmpty(first));
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(null, "Footer.Copyright", true)]
    [InlineData("", "Footer.Copyright", true)]
    [InlineData("Footer.", "Footer.Copyright", true)]
    [InlineData("footer.", "Footer.Copyright", true)]
    [InlineData("Header.", "Footer.Copyright", false)]
    public void Key_filter_decides_which_automations_run(string? keyStartsWith, string key, bool expected)
    {
        // CanHandle is what Automate calls per automation, through the ITrigger interface.
        ITrigger trigger = _trigger;

        var handled = trigger.CanHandle(
            new DictionaryItemSavedOutput { Key = key },
            new DictionaryItemSavedSettings { KeyStartsWith = keyStartsWith });

        Assert.Equal(expected, handled);
    }
}
```

`<Area>IconTests.cs`. Nothing else checks that the manifest path, the icon files and the
attributes' `Icon` agree. In a package with a `Client/`, read `Client/public` instead of `wwwroot`.

`Umbraco.Community.Automate.Examples.KitchenSink.Tests/KitchenSinkIconTests.cs`
```csharp
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Community.Automate.Examples.KitchenSink.Connections;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests;

/// <summary>
/// A wrong icon path or name gives no error, just a blank icon in the backoffice, so these tests
/// check the manifest, the icon files and the attributes agree.
/// </summary>
public class KitchenSinkIconTests
{
    private static readonly Assembly Package = typeof(KitchenSinkConnectionType).Assembly;
    private static readonly string Wwwroot = Path.Combine(ProjectDirectory(), "wwwroot");

    [Fact]
    public void Manifest_registers_icons_from_files_that_exist()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Wwwroot, "umbraco-package.json")));
        var icons = Assert.Single(doc.RootElement.GetProperty("extensions").EnumerateArray(), e => e.GetProperty("type").GetString() == "icons");

        // Must match StaticWebAssetBasePath in the csproj, or the backoffice 404s on the module.
        Assert.Equal("/App_Plugins/UmbracoCommunityAutomateExamplesKitchenSink/icons/icons.js", icons.GetProperty("js").GetString());
        Assert.True(File.Exists(Path.Combine(Wwwroot, "icons", "icons.js")));
        Assert.True(File.Exists(Path.Combine(Wwwroot, "icons", "kitchensink.icon.js")));
    }

    [Fact]
    public void Every_connection_type_action_and_trigger_uses_a_registered_icon()
    {
        var registered = Regex.Matches(File.ReadAllText(Path.Combine(Wwwroot, "icons", "icons.js")), @"name:\s*""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .ToHashSet();

        var used = Package.GetTypes()
            .Select(t => t.GetCustomAttribute<ConnectionTypeAttribute>()?.Icon
                         ?? t.GetCustomAttribute<ActionAttribute>()?.Icon
                         ?? t.GetCustomAttribute<TriggerAttribute>()?.Icon)
            .OfType<string>()
            .Distinct()
            .ToList();

        Assert.NotEmpty(used);
        Assert.All(used, icon => Assert.Contains(icon, registered));
    }

    // The static web assets aren't copied to the test output, so read them from the project.
    private static string ProjectDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Umbraco.Community.Automate.Examples.KitchenSink");
            if (Directory.Exists(Path.Combine(candidate, "wwwroot")))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate the Kitchen Sink example project directory.");
    }
}
```

`<Area>FieldTests.cs`. Every package has this test: a setting without a `Label` or `Description`
doesn't fail anywhere, it just shows a raw `#uaFields_...` localization key in the backoffice.

`Umbraco.Community.Automate.Examples.KitchenSink.Tests/KitchenSinkFieldTests.cs`
```csharp
using System.Reflection;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Examples.KitchenSink.Connections;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests;

/// <summary>
/// A setting without a Label or Description shows a raw localization key in the backoffice
/// (e.g. #uaFields_...Description) instead of failing anywhere, so this test catches it.
/// </summary>
public class KitchenSinkFieldTests
{
    [Fact]
    public void Every_setting_has_a_label_and_description()
    {
        var fields = typeof(KitchenSinkConnectionType).Assembly.GetTypes()
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

The same for every real package, simple or full — the fixture itself skips all of this, since
it's reference material, not a package.

`Umbraco.Community.Automate.slnx`, alongside the other packages:

```xml
  <Folder Name="/Packages/<Area>/">
    <Project Path="Packages/<Area>/Umbraco.Community.Automate.<Area>.Tests/Umbraco.Community.Automate.<Area>.Tests.csproj" />
    <Project Path="Packages/<Area>/Umbraco.Community.Automate.<Area>/Umbraco.Community.Automate.<Area>.csproj" />
  </Folder>
```

`Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj`:

```xml
    <ProjectReference Include="..\Packages\<Area>\Umbraco.Community.Automate.<Area>\Umbraco.Community.Automate.<Area>.csproj" />
```

Every configuration key the package reads goes into the Demo site twice, under the same paths
as `<Area>Configuration`.

`Umbraco.Community.Automate.Demo/appsettings.Development.json`, under `Umbraco:Automate:Secrets`
(and `Variables` for any non-secret values): obviously fake placeholders, so the site boots and
`DemoSetupHandler` can pre-fill the new connection's references without real credentials:

```json
"<Area>": {
  "ApiKey": "e2e-test"
}
```

`Umbraco.Community.Automate.Demo/appsettings.Local.example.json`, in the same place: the template
contributors copy to the git-ignored `appsettings.Local.json` to try the package with real
credentials. Use a non-empty `your-...` value, since an empty one would override the placeholder:

```json
"<Area>": { "ApiKey": "your-<area>-api-key" }
```

`DemoSetupHandler` then auto-creates the connection and a `Test: <Area>` automation on next
boot — see [SKILL.md's Demo setup](../SKILL.md#demo-setup); there's nothing else to wire by hand.

Root `README.md` packages table:

```markdown
| [<Area>](Packages/<Area>/Umbraco.Community.Automate.<Area>/README.md) | What it does, in one or two sentences. | Not yet on NuGet: [build from source](#using-a-package-before-its-on-nuget) |
```

With a `Client/`, also add its `wwwroot/` to the root `.gitignore` and the `npm ci && npm run build` step to the Demo site's first-run instructions in the README and CONTRIBUTING.

## README skeleton

The package README ships inside the NuGet package; write it for site developers installing it,
with absolute URLs for anything outside the package folder.

```markdown
# Umbraco.Community.Automate.<Area>

A <Area> connection, actions and trigger for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate).

One or two sentences on what automations can do with it, e.g. "Create a post on <Area> when a blog post is published."

## Installation

    dotnet add package Umbraco.Community.Automate.<Area>

There's nothing to register in code: Umbraco loads the package on its own. Then follow **Setup** below.

## Setup

### 1. Get an API key
Where to find it in the service, step by step.

### 2. Store it in configuration
appsettings.json under Umbraco:Automate:Secrets:<Area> (and Variables:<Area> for anything non-secret),
the environment-variable equivalents (Umbraco__Automate__Secrets__<Area>__ApiKey), and user secrets.

### 3. Create the connection
1. Go to **Automation → Settings → Connections** and create a new **<Area>** connection.
2. The API key field is pre-filled with `$Umbraco:Automate:Secrets:<Area>:ApiKey`; keep it, or enter the key itself.
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
- [Source code](https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Packages/<Area>/Umbraco.Community.Automate.<Area>)
- [Report an issue](https://github.com/umbraco-community/Umbraco.Community.Automate/issues)
```
