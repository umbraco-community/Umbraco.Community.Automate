# Simple connection: an API key and an action

Every file for the most common package: one connection with an API key, and one action that calls the service. It's `Packages/_Development/Simple/` set up as a real, releasable package. Most new connections start here, and you add more actions to it the same way.

Replace `Example` / `example` with the area name (PascalCase / camelCase / lowercase as shown) and adjust the calls to the service. If the real code in `Packages/_Development/Simple/` disagrees with this file, the real code wins.

Outgrow it when the package needs a trigger, outcomes, several actions sharing request and error handling, or a backoffice editor: then follow [full.md](full.md), which builds on the same files.

## Contents

- [Files](#files)
- [Package project](#package-project)
- [Directory.Build.props](#directorybuildprops)
- [Configuration](#configuration)
- [Connection settings and type](#connection-settings-and-type)
- [Action, settings and output](#action-settings-and-output)
- [Composer](#composer)
- [Test project](#test-project)
- [Tests](#tests)
- [Wiring into the repo](#wiring-into-the-repo)
- [Adding a second action](#adding-a-second-action)

## Files

```
Packages/Example/
  Umbraco.Community.Automate.Example/
    Actions/
      SendMessageAction.cs
      SendMessageOutput.cs
      SendMessageSettings.cs
    Composers/
      ExampleComposer.cs
    Configuration/
      ExampleConfiguration.cs
    Connections/
      ExampleConnectionSettings.cs
      ExampleConnectionType.cs
    Directory.Build.props
    README.md
    community-automate-128.png          copy from another package
    Umbraco.Community.Automate.Example.csproj
  Umbraco.Community.Automate.Example.Tests/
    Actions/SendMessageActionTests.cs
    Connections/ExampleConnectionTypeTests.cs
    Fakes/StubHttp.cs
    Umbraco.Community.Automate.Example.Tests.csproj
```

No `Api/`, `Models/`, `Client/` or `wwwroot/`: the connection type and the action each make their one HTTP call directly, and the icon is a built-in Umbraco one, because httpbin is a utility rather than a vendor. A package for a commercial service adds the vendor's logo (see Icons in SKILL.md).

## Package project

`Packages/Example/Umbraco.Community.Automate.Example/Umbraco.Community.Automate.Example.csproj`. Plain `Microsoft.NET.Sdk`: there are no static files to serve.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Title>Umbraco Community Automate Example</Title>
    <Description>Example connection and actions for Umbraco Automate. One or two sentences on what it lets automations do.</Description>
    <PackageTags>umbraco automate automation example umbraco-marketplace</PackageTags>
    <PackageIcon>community-automate-128.png</PackageIcon>
  </PropertyGroup>

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

No `Version` attributes: versions live in the root `Directory.Packages.props`.

## Directory.Build.props

`Packages/Example/Umbraco.Community.Automate.Example/Directory.Build.props`. Besides the package metadata, this file is what makes CI find the package.

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

`Configuration/ExampleConfiguration.cs`. The one place the configuration path, the default reference and the service's fixed values are defined.

```csharp
namespace Umbraco.Community.Automate.Example.Configuration;

/// <summary>
/// Where this package's values live in configuration. The API key sits under Umbraco Automate's
/// shared <c>Umbraco:Automate:Secrets</c> section, which Automate resolves <c>$</c> references
/// from by default, so nothing needs registering.
/// </summary>
public static class ExampleConfiguration
{
    public const string SecretsPath = "Umbraco:Automate:Secrets:Example";

    /// <summary>The reference new connections start with, so a key in configuration is used without any typing.</summary>
    public const string ApiKeyReference = "$" + SecretsPath + ":ApiKey";

    // Fixed values: they're the same for every site, so they're constants here, not configuration.

    /// <summary>The service's API.</summary>
    public const string BaseUrl = "https://api.example.com/";
}
```

## Connection settings and type

`Connections/ExampleConnectionSettings.cs`. The credential defaults to its configuration reference, so a new connection opens pre-filled. The description says what the value is and where to get it, not the path: the field already shows that.

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

`Connections/ExampleConnectionType.cs`. `ValidateAsync` runs when the user clicks **Test connection**: check the fields, then make one cheap authenticated call. The alias is stored in saved connections and automations, so it can never change once released.

```csharp
using System.Net.Http.Headers;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Example.Configuration;

namespace Umbraco.Community.Automate.Example.Connections;

[ConnectionType("community.example", "Example",
    Description = "Connects Umbraco Automate to Example.",
    Group = "Productivity",
    Icon = "icon-paper-plane")]
public sealed class ExampleConnectionType(ConnectionTypeInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
    : ConnectionTypeBase<ExampleConnectionSettings>(infrastructure)
{
    public override async Task<ConnectionValidationResult> ValidateAsync(object? settings, CancellationToken cancellationToken)
    {
        var apiKey = (settings as ExampleConnectionSettings)?.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return ConnectionValidationResult.Failure("An API key is required.");

        using var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(ExampleConfiguration.BaseUrl), "v1/me"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode
                ? ConnectionValidationResult.Success("Connected to Example.")
                : ConnectionValidationResult.Failure($"Example rejected the API key ({(int)response.StatusCode}).");
        }
        catch (HttpRequestException ex)
        {
            return ConnectionValidationResult.Failure($"Could not reach Example: {ex.Message}");
        }
    }
}
```

For a utility with no vendor, pick the icon from Umbraco's built-in set (`icon-paper-plane`, `icon-message`, `icon-chat`, `icon-partly-cloudy`...). For a commercial service, use the vendor's logo, found on the web (see Icons in SKILL.md), with the files in [full.md](full.md#icons-and-umbraco-packagejson). Either way, use the same icon on the action.

## Action, settings and output

`Actions/SendMessageSettings.cs`. The fields shown in the automation editor. `SupportsBindings` lets a value come from the trigger or an earlier step, and a text area suits a message better than the default one-line box. See [fields.md](fields.md) for choosing the editor for each setting.

```csharp
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Example.Actions;

public sealed class SendMessageSettings
{
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

`Actions/SendMessageOutput.cs`. What later steps can use, in camelCase: `${ steps.<alias>.statusCode }`. Drop it (and use `ActionBase<TSettings>`) if the action returns nothing useful.

```csharp
namespace Umbraco.Community.Automate.Example.Actions;

public sealed class SendMessageOutput
{
    public int StatusCode { get; init; }
}
```

`Actions/SendMessageAction.cs`. Check the inputs, call the service, and report the result. The failure category decides whether Automate retries the step: rate limits and outages are worth retrying, a rejected key isn't.

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Example.Configuration;
using Umbraco.Community.Automate.Example.Connections;

namespace Umbraco.Community.Automate.Example.Actions;

[Action("community.example.sendMessage", "Send Example Message",
    Description = "Posts a message to Example.",
    ConnectionTypeAlias = "community.example",
    Group = "Productivity",
    Icon = "icon-paper-plane")]
public sealed class SendMessageAction(ActionInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
    : ActionBase<SendMessageSettings, SendMessageOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        // 1. Check the inputs first and fail fast with a message the user can act on.
        var apiKey = context.Connection?.GetSettings<ExampleConnectionSettings>().ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return ActionResult.Failed(new InvalidOperationException("The connection has no API key."), StepRunErrorCategory.ConfigurationError);

        var settings = context.GetSettings<SendMessageSettings>();
        if (string.IsNullOrWhiteSpace(settings.Message))
            return ActionResult.Failed(new InvalidOperationException("A message is required."), StepRunErrorCategory.Validation);

        // 2. Call the service. The idempotency key is the same on a retry of this step, so a
        //    service that supports it ignores the duplicate instead of posting twice.
        using var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(ExampleConfiguration.BaseUrl), "v1/messages"))
        {
            Content = JsonContent.Create(new { message = settings.Message }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Add("Idempotency-Key", $"{context.RunId}:{context.StepId}");

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.ServiceUnavailable);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.Timeout);
        }

        // 3. Report the result.
        using (response)
        {
            if (response.IsSuccessStatusCode)
                return Success(new SendMessageOutput { StatusCode = (int)response.StatusCode });

            var category = (int)response.StatusCode switch
            {
                401 or 403 => StepRunErrorCategory.Authentication,
                400 or 404 or 422 => StepRunErrorCategory.Validation,
                429 => StepRunErrorCategory.RateLimiting,
                >= 500 => StepRunErrorCategory.ServiceUnavailable,
                _ => StepRunErrorCategory.InvalidResponse,
            };
            return ActionResult.Failed(new HttpRequestException($"Example returned {(int)response.StatusCode}."), category);
        }
    }
}
```

## Composer

`Composers/ExampleComposer.cs`. Registers the package's services. Umbraco finds composers on its own, and Automate finds the connection type and actions from their attributes, so a site only installs the package.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.Automate.Example.Composers;

public class ExampleComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient();

        // No connection type, action or trigger registration: Automate discovers classes with
        // [ConnectionType], [Action] and [Trigger] attributes on its own. Only services go here.
        // No configuration registration: the API key reference lives under Automate's shared
        // Umbraco:Automate:Secrets section, which it resolves out of the box.
    }
}
```

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

xUnit only, with a hand-written HTTP stub instead of a mocking library. Never call the real service.

`Fakes/StubHttp.cs`. Returns canned responses in order and records each request. It reads each body as the request goes out, because the code under test disposes its requests straight after sending.

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

`Actions/SendMessageActionTests.cs`. `ActionTestHarness` runs the action end to end; give it every service its constructor asks for.

```csharp
using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Example.Actions;
using Umbraco.Community.Automate.Example.Connections;
using Umbraco.Community.Automate.Example.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Example.Tests.Actions;

public class SendMessageActionTests
{
    [Fact]
    public async Task Posts_the_message_with_the_api_key()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, "{}"));

        var result = await Run(handler, "Hello");

        Assert.Equal(ActionResultStatus.Success, result.Status);
        Assert.Equal(200, Assert.IsType<SendMessageOutput>(result.OutputData).StatusCode);

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("Bearer token", request.Headers.Authorization!.ToString());
        Assert.True(request.Headers.Contains("Idempotency-Key"));
        Assert.Contains("Hello", handler.Bodies.Single());
    }

    [Fact]
    public async Task Blank_message_is_a_validation_error_and_calls_nothing()
    {
        var handler = new StubHttpMessageHandler();

        var result = await Run(handler, " ");

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.TooManyRequests, StepRunErrorCategory.RateLimiting)]
    [InlineData(HttpStatusCode.BadGateway, StepRunErrorCategory.ServiceUnavailable)]
    public async Task Failed_responses_are_categorised_so_Automate_knows_whether_to_retry(HttpStatusCode status, StepRunErrorCategory expected)
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(status, "{}"));

        var result = await Run(handler, "Hello");

        Assert.Equal(expected, result.ErrorCategory);
    }

    private static Task<ActionResult> Run(StubHttpMessageHandler handler, string message)
        => ActionTestHarness.For<SendMessageAction>()
            .WithService<IHttpClientFactory>(new StubHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Message = message })
            .WithConnection("community.example", new ExampleConnectionSettings { ApiKey = "token" })
            .ExecuteAsync();
}
```

`Connections/ExampleConnectionTypeTests.cs`. Construct the connection type with a fake model resolver: validation never uses it, so its members just throw. Implementing them explicitly avoids repeating the interface's generic constraint.

```csharp
using System.Net;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Example.Connections;
using Umbraco.Community.Automate.Example.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Example.Tests.Connections;

public class ExampleConnectionTypeTests
{
    [Fact]
    public void New_connections_start_with_the_configuration_reference()
        => Assert.Equal("$Umbraco:Automate:Secrets:Example:ApiKey", new ExampleConnectionSettings().ApiKey);

    [Fact]
    public async Task Accepted_key_connects()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, "{}"));

        var result = await CreateSut(handler).ValidateAsync(new ExampleConnectionSettings { ApiKey = "token" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Success, result.Status);
        Assert.Equal("Bearer token", handler.Requests.Single().Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task Missing_key_fails_without_calling_the_service()
    {
        var handler = new StubHttpMessageHandler();

        var result = await CreateSut(handler).ValidateAsync(new ExampleConnectionSettings { ApiKey = "" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Rejected_key_fails_with_the_status_code()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, "{}"));

        var result = await CreateSut(handler).ValidateAsync(new ExampleConnectionSettings { ApiKey = "token" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Contains("401", result.Message);
    }

    private static ExampleConnectionType CreateSut(HttpMessageHandler handler)
        => new(new ConnectionTypeInfrastructure(new UnusedModelResolver()), new StubHttpClientFactory(handler));

    private sealed class UnusedModelResolver : IEditableModelResolver
    {
        object IEditableModelResolver.ResolveModel(string alias, Type modelType, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
        TModel IEditableModelResolver.ResolveModel<TModel>(string alias, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
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

The same for every package; see [full.md](full.md#wiring-into-the-repo) for the solution, Demo site and root README snippets, and the [README skeleton](full.md#readme-skeleton). That includes adding the package's configuration to the Demo site: `Umbraco:Automate:Secrets:Example:ApiKey` with a fake placeholder (`"e2e-test"`) in `Umbraco.Community.Automate.Demo/appsettings.Development.json`, and with a `your-...` value in `appsettings.Local.example.json`, the template for real credentials.

## Adding a second action

Copy the action's three files under a new name, give it its own alias (`community.example.<name>`), add it to the composer, and test it the same way. When two or more actions repeat the same request and error-handling code, move that into an `Api/ExampleClient.cs` with a shared error mapping: that's the first step towards [full.md](full.md#api-client-and-models).
