# Simple connection: an API key and an action

Every file for the most common package: one connection with an API key, and one action that
calls the service. This is generated from **WeatherApi** (`Packages/WeatherApi/`) — a real,
shipped package, and the simplest one here — showing only its connection and its first action,
**Get Current Weather**; its README names a second, **Get Today's Weather**, which is identical
in shape (see [Adding a second action](#adding-a-second-action)).

Replace `WeatherApi` / `weatherApi` (as shown) with your own area name, and adjust to your
service. Because this is real code rather than a stripped-down template, it also validates one
thing a minimal example wouldn't: a culture argument, with its own error handling. Copy the
*structure* (one settings class, one output class, one action class, a connection, a
configuration class, a composer), not that specific validation — write whatever your own service
actually needs instead. If the real code in `Packages/WeatherApi/` disagrees with this file, the
real code wins, since this file is generated from it.

Outgrow it when the package needs a trigger, outcomes, several actions sharing request and error
handling, or a backoffice editor: then follow [full.md](full.md), which builds on the same files.

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
Packages/<Area>/
  Umbraco.Community.Automate.<Area>/
    Actions/
      GetCurrentWeatherAction.cs
      GetCurrentWeatherOutput.cs
      GetCurrentWeatherSettings.cs
    Composers/
      WeatherApiComposer.cs
    Configuration/
      WeatherApiConfiguration.cs
    Connections/
      WeatherApiConnectionSettings.cs, WeatherApiConnectionSettingsValidator.cs, WeatherApiConnectionType.cs
    Directory.Build.props
    README.md
    community-automate-128.png          copy from another package
    Umbraco.Community.Automate.<Area>.csproj
  Umbraco.Community.Automate.<Area>.Tests/
    Actions/GetCurrentWeatherActionTests.cs
    Connections/WeatherApiConnectionTypeTests.cs, WeatherApiConnectionSettingsValidatorTests.cs
    Fakes/StubHttp.cs
    Umbraco.Community.Automate.<Area>.Tests.csproj
```

No `Client/` or `wwwroot/`: the icon is a built-in Umbraco one (`icon-partly-cloudy`), because
WeatherAPI.com isn't the kind of commercial brand this repo puts a logo on. A package for a
vendor whose logo you want to show adds that instead (see Icons in SKILL.md). WeatherApi has
grown an `Api/` and `Models/` folder for its shared request logic — see [Adding a second
action](#adding-a-second-action) — which a package with genuinely one action and nothing to
share yet can skip, calling the connection's `HttpClient` directly instead.

## Package project

`Packages/<Area>/Umbraco.Community.Automate.<Area>/Umbraco.Community.Automate.<Area>.csproj`. Plain `Microsoft.NET.Sdk`: there are no static files to serve.

`Umbraco.Community.Automate.WeatherApi/Umbraco.Community.Automate.WeatherApi.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Title>Umbraco Community Automate WeatherAPI</Title>
    <Description>WeatherAPI.com connection type and actions for Umbraco Automate. Get the current weather or today's forecast for a location as part of an automation workflow.</Description>
    <PackageTags>umbraco automate automation weatherapi weather umbraco-marketplace</PackageTags>
    <PackageIcon>community-automate-128.png</PackageIcon>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Umbraco.Automate.Core" />
    <!-- Versions the package from its weatherapi-v* tag (see Directory.Build.props). -->
    <PackageReference Include="MinVer" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Umbraco.Community.Automate.WeatherApi.Tests" />
  </ItemGroup>

  <ItemGroup>
    <None Include="README.md" Pack="true" PackagePath="" />
    <None Include="community-automate-128.png" Pack="true" PackagePath="" />
  </ItemGroup>

</Project>
```

No `Version` attributes: versions live in the root `Directory.Packages.props`.

## Directory.Build.props

`Packages/<Area>/Umbraco.Community.Automate.<Area>/Directory.Build.props`. Besides the package metadata, this file is what makes CI find the package.

`Umbraco.Community.Automate.WeatherApi/Directory.Build.props`
```xml
<Project>
  <PropertyGroup>
    <Authors>Paul Wright, Umbraco Community</Authors>
    <PackageProjectUrl>https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Packages/WeatherApi/Umbraco.Community.Automate.WeatherApi</PackageProjectUrl>
    <RepositoryUrl>https://github.com/umbraco-community/Umbraco.Community.Automate</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <PropertyGroup>
    <!-- Releases come from weatherapi-v* tags only; height-based pre-release
         versions would react to other packages' commits in this monorepo. -->
    <MinVerTagPrefix>weatherapi-v</MinVerTagPrefix>
    <MinVerIgnoreHeight>true</MinVerIgnoreHeight>
  </PropertyGroup>

  <PropertyGroup>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>
</Project>
```

## Configuration

`Configuration/WeatherApiConfiguration.cs`. The one place the configuration path, the default reference and the service's fixed values are defined.

`Umbraco.Community.Automate.WeatherApi/Configuration/WeatherApiConfiguration.cs`
```csharp
namespace Umbraco.Community.Automate.WeatherApi.Configuration;

/// <summary>
/// Where this package's values live in configuration. They sit under Umbraco Automate's shared
/// <c>Umbraco:Automate:Secrets</c> and <c>Umbraco:Automate:Variables</c> sections, which Automate
/// resolves <c>$</c> references from by default, so nothing needs registering. Nesting under
/// <c>WeatherApi</c> keeps this package's keys together and avoids clashes with other packages.
/// </summary>
public static class WeatherApiConfiguration
{
    public const string VariablesPath = "Umbraco:Automate:Variables:WeatherApi";
    public const string SecretsPath = "Umbraco:Automate:Secrets:WeatherApi";

    /// <summary>The reference new connections start with, so a key in configuration is used without any typing.</summary>
    public const string ApiKeyReference = "$" + SecretsPath + ":ApiKey";

    // Fixed value: it's the same for every site, so it's a constant here, not configuration.
    /// <summary>The WeatherAPI.com REST API.</summary>
    public const string BaseUrl = "https://api.weatherapi.com/v1";
}
```

## Connection settings and type

`Connections/WeatherApiConnectionSettings.cs`. The credential defaults to its configuration reference, so a new connection opens pre-filled.

`Umbraco.Community.Automate.WeatherApi/Connections/WeatherApiConnectionSettings.cs`
```csharp
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.WeatherApi.Configuration;

namespace Umbraco.Community.Automate.WeatherApi.Connections;

public sealed class WeatherApiConnectionSettings
{
    [Field(
        Label = "API Key",
        Description = "The WeatherAPI.com API key.",
        IsSensitive = true,
        SortOrder = 1)]
    public string ApiKey { get; set; } = WeatherApiConfiguration.ApiKeyReference;
}
```

`Connections/WeatherApiConnectionSettingsValidator.cs`. Worth having as soon as more than one
place checks the connection: here, the connection type and the action already share it.

`Umbraco.Community.Automate.WeatherApi/Connections/WeatherApiConnectionSettingsValidator.cs`
```csharp
using Umbraco.Community.Automate.WeatherApi.Configuration;

namespace Umbraco.Community.Automate.WeatherApi.Connections;

/// <summary>
/// Shared validation for <see cref="WeatherApiConnectionSettings"/>, used by the connection
/// type (Test connection) and both actions, so they report the same message.
/// </summary>
public static class WeatherApiConnectionSettingsValidator
{
    /// <summary>Returns the first problem found, or <c>null</c> when the settings are valid.</summary>
    public static string? Validate(WeatherApiConnectionSettings? settings)
    {
        if (string.IsNullOrWhiteSpace(settings?.ApiKey))
            return "A WeatherAPI.com API key is required.";

        // Automate resolves $-references before settings reach this code, and reports a missing
        // key itself; this is a safety net for a reference that arrives unresolved anyway.
        if (settings.ApiKey.TrimStart().StartsWith('$'))
            return $"The API key reference '{settings.ApiKey}' could not be resolved. Add the key to configuration at {WeatherApiConfiguration.SecretsPath}:ApiKey, or enter the key itself on the connection.";

        return null;
    }
}
```

`Connections/WeatherApiConnectionType.cs`. `ValidateAsync` runs when the user clicks **Test
connection**: check the fields, then make one cheap authenticated call — here, through the
shared `Api/WeatherApiRequestHelper.cs` (see [Adding a second action](#adding-a-second-action));
with one action and no shared helper yet, call the service directly instead. The alias is stored
in saved connections and automations, so it can never change once released.

`Umbraco.Community.Automate.WeatherApi/Connections/WeatherApiConnectionType.cs`
```csharp
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.WeatherApi.Api;

namespace Umbraco.Community.Automate.WeatherApi.Connections;

[ConnectionType("community.weatherApi", "WeatherAPI.com",
    Description = "Connect to WeatherAPI.com using an API key",
    Group = "Weather",
    Icon = "icon-partly-cloudy")]
public sealed class WeatherApiConnectionType(ConnectionTypeInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
    : ConnectionTypeBase<WeatherApiConnectionSettings>(infrastructure)
{
    public override async Task<ConnectionValidationResult> ValidateAsync(
        object? settings,
        CancellationToken cancellationToken)
    {
        var typed = settings as WeatherApiConnectionSettings;

        if (WeatherApiConnectionSettingsValidator.Validate(typed) is { } error)
            return ConnectionValidationResult.Failure(error);

        using var client = httpClientFactory.CreateClient();

        return await WeatherApiRequestHelper.ValidateApiKeyAsync(client, typed!.ApiKey, cancellationToken);
    }
}
```

For a utility with no vendor, pick the icon from Umbraco's built-in set (`icon-paper-plane`, `icon-message`, `icon-chat`, `icon-partly-cloudy`...). For a commercial service, use the vendor's logo, found on the web (see Icons in SKILL.md), with the files in [full.md](full.md#icons-and-umbraco-packagejson). Either way, use the same icon on the action.

## Action, settings and output

`Actions/GetCurrentWeatherSettings.cs`. The fields shown in the automation editor.
`SupportsBindings` lets a value come from the trigger or an earlier step. See
[fields.md](fields.md) for choosing the editor for each setting.

`Umbraco.Community.Automate.WeatherApi/Actions/GetCurrentWeatherSettings.cs`
```csharp
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.WeatherApi.Actions;

public class GetCurrentWeatherSettings
{
    [Field(
        Label = "Location",
        Description = "The location to get current weather for. Accepts a city name, US zip, UK postcode, IP address, or \"lat,lon\". Supports bindings.",
        SupportsBindings = true,
        SortOrder = 1)]
    public string Location { get; set; } = string.Empty;

    [Field(
        Label = "Culture",
        Description = "Optional. The culture to localize the weather condition text into, e.g. \"en-GB\", \"fr-FR\", \"es-ES\". Falls back to English if not set. Supports bindings.",
        SupportsBindings = true,
        SortOrder = 2)]
    public string? Culture { get; set; }
}
```

`Actions/GetCurrentWeatherOutput.cs`. What later steps can use, in camelCase: `${
steps.<alias>.temperatureC }`. Drop it (and use `ActionBase<TSettings>`) if the action returns
nothing useful.

`Umbraco.Community.Automate.WeatherApi/Actions/GetCurrentWeatherOutput.cs`
```csharp
namespace Umbraco.Community.Automate.WeatherApi.Actions;

public sealed class GetCurrentWeatherOutput
{
    public string? LocationName { get; init; }
    public string? Region { get; init; }
    public string? Country { get; init; }
    public string? LocalTime { get; init; }
    public double TemperatureC { get; init; }
    public double TemperatureF { get; init; }
    public string? Condition { get; init; }
    public string? ConditionIconUrl { get; init; }
    public int ConditionCode { get; init; }
    public int Humidity { get; init; }
    public int Cloud { get; init; }
    public double WindKph { get; init; }
    public double WindMph { get; init; }
    public string? WindDirection { get; init; }
    public string? LastUpdated { get; init; }
    public bool WillItRain { get; init; }
    public int ChanceOfRain { get; init; }
    public bool WillItSnow { get; init; }
    public int ChanceOfSnow { get; init; }
    public double Uv { get; init; }

    /// <summary>The full, unprocessed JSON response returned by WeatherAPI.com.</summary>
    public string? RawResponse { get; init; }
}
```

`Actions/GetCurrentWeatherAction.cs`. Check the inputs, call the service, and report the result.
The failure category decides whether Automate retries the step: rate limits and outages are
worth retrying, a rejected key isn't. The culture handling is specific to this action — a package
with one simple input and output doesn't need anything like it.

`Umbraco.Community.Automate.WeatherApi/Actions/GetCurrentWeatherAction.cs`
```csharp
using System.Globalization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.WeatherApi.Api;
using Umbraco.Community.Automate.WeatherApi.Connections;

namespace Umbraco.Community.Automate.WeatherApi.Actions;

[Action("community.weatherApi.getCurrentWeather", "Get Current Weather",
    Description = "Gets the current weather conditions for a location",
    Group = "Weather",
    Icon = "icon-partly-cloudy",
    ConnectionTypeAlias = "community.weatherApi")]
public class GetCurrentWeatherAction(ActionInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
    : ActionBase<GetCurrentWeatherSettings, GetCurrentWeatherOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<GetCurrentWeatherSettings>();

        if (string.IsNullOrWhiteSpace(settings.Location))
        {
            return ActionResult.Failed(
                new ArgumentException("Location is required."),
                StepRunErrorCategory.Validation);
        }

        var connectionSettings = context.Connection?.GetSettings<WeatherApiConnectionSettings>();

        if (WeatherApiConnectionSettingsValidator.Validate(connectionSettings) is { } connectionError)
        {
            return ActionResult.Failed(
                new InvalidOperationException(connectionError),
                StepRunErrorCategory.ConfigurationError);
        }

        string? languageCode = null;
        if (!string.IsNullOrWhiteSpace(settings.Culture))
        {
            try
            {
                languageCode = WeatherApiRequestHelper.ResolveLanguageCode(CultureInfo.GetCultureInfo(settings.Culture));
            }
            catch (CultureNotFoundException ex)
            {
                return ActionResult.Failed(
                    new ArgumentException($"\"{settings.Culture}\" is not a recognized culture.", ex),
                    StepRunErrorCategory.Validation);
            }
        }

        try
        {
            var client = httpClientFactory.CreateClient();

            var result = await WeatherApiRequestHelper.GetCurrentWeatherAsync(
                client, connectionSettings!.ApiKey, settings.Location, languageCode, cancellationToken);

            if (!result.IsSuccess)
            {
                return ActionResult.Failed(
                    new InvalidOperationException(result.ErrorMessage ?? $"WeatherAPI.com returned status {result.StatusCode}"),
                    WeatherApiRequestHelper.Classify(result.StatusCode));
            }

            var location = result.Response?.Location;
            var current = result.Response?.Current;
            var condition = current?.Condition;
            var iconUrl = condition?.Icon;
            if (!string.IsNullOrWhiteSpace(iconUrl) && iconUrl.StartsWith("//"))
                iconUrl = $"https:{iconUrl}";

            return Success(new GetCurrentWeatherOutput
            {
                LocationName = location?.Name,
                Region = location?.Region,
                Country = location?.Country,
                LocalTime = location?.Localtime,
                TemperatureC = current?.TempC ?? 0,
                TemperatureF = current?.TempF ?? 0,
                Condition = condition?.Text,
                ConditionIconUrl = iconUrl,
                ConditionCode = condition?.Code ?? 0,
                Humidity = current?.Humidity ?? 0,
                Cloud = current?.Cloud ?? 0,
                WindKph = current?.WindKph ?? 0,
                WindMph = current?.WindMph ?? 0,
                WindDirection = current?.WindDir,
                LastUpdated = current?.LastUpdated,
                WillItRain = current?.WillItRain == 1,
                ChanceOfRain = current?.ChanceOfRain ?? 0,
                WillItSnow = current?.WillItSnow == 1,
                ChanceOfSnow = current?.ChanceOfSnow ?? 0,
                Uv = current?.Uv ?? 0,
                RawResponse = result.RawBody,
            });
        }
        catch (HttpRequestException ex)
        {
            return ActionResult.Failed(
                new InvalidOperationException($"Could not reach WeatherAPI.com: {ex.Message}", ex),
                StepRunErrorCategory.ServiceUnavailable);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return ActionResult.Failed(
                new InvalidOperationException("The request to WeatherAPI.com timed out.", ex),
                StepRunErrorCategory.Timeout);
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }
}
```

## Composer

`Composers/WeatherApiComposer.cs`. Registers the package's services. Umbraco finds composers on
its own, and Automate finds the connection type and actions from their attributes, so a site only
installs the package.

`Umbraco.Community.Automate.WeatherApi/Composers/WeatherApiComposer.cs`
```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.Automate.WeatherApi.Composers;

public class WeatherApiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient();
    }
}
```

## Test project

`Packages/<Area>/Umbraco.Community.Automate.<Area>.Tests/Umbraco.Community.Automate.<Area>.Tests.csproj`.

`Umbraco.Community.Automate.WeatherApi.Tests/Umbraco.Community.Automate.WeatherApi.Tests.csproj`
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
    <ProjectReference Include="..\Umbraco.Community.Automate.WeatherApi\Umbraco.Community.Automate.WeatherApi.csproj" />
  </ItemGroup>

</Project>
```

## Tests

xUnit only, with a hand-written HTTP stub instead of a mocking library. Never call the real service.

`Fakes/StubHttp.cs`. Returns canned responses in order and records each request.

`Umbraco.Community.Automate.WeatherApi.Tests/Fakes/StubHttp.cs`
```csharp
using System.Net;
using System.Text;

namespace Umbraco.Community.Automate.WeatherApi.Tests.Fakes;

/// <summary>Returns canned responses in order and records every request sent.</summary>
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

public sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
```

`Connections/WeatherApiConnectionSettingsValidatorTests.cs`.

`Umbraco.Community.Automate.WeatherApi.Tests/Connections/WeatherApiConnectionSettingsValidatorTests.cs`
```csharp
using Umbraco.Community.Automate.WeatherApi.Connections;
using Xunit;

namespace Umbraco.Community.Automate.WeatherApi.Tests.Connections;

public class WeatherApiConnectionSettingsValidatorTests
{
    [Fact]
    public void New_connections_default_to_the_configuration_reference()
        => Assert.Equal("$Umbraco:Automate:Secrets:WeatherApi:ApiKey", new WeatherApiConnectionSettings().ApiKey);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_api_key_fails(string apiKey)
        => Assert.Equal("A WeatherAPI.com API key is required.",
            WeatherApiConnectionSettingsValidator.Validate(new WeatherApiConnectionSettings { ApiKey = apiKey }));

    [Fact]
    public void Missing_settings_fail()
        => Assert.Equal("A WeatherAPI.com API key is required.", WeatherApiConnectionSettingsValidator.Validate(null));

    [Fact]
    public void Unresolved_default_reference_explains_where_to_put_the_key()
    {
        // A reference arriving unresolved (Automate normally resolves it, or reports the missing key, first).
        var error = WeatherApiConnectionSettingsValidator.Validate(new WeatherApiConnectionSettings());

        Assert.NotNull(error);
        Assert.Contains("could not be resolved", error);
        Assert.Contains("Umbraco:Automate:Secrets:WeatherApi:ApiKey", error);
    }

    [Fact]
    public void Real_key_passes()
        => Assert.Null(WeatherApiConnectionSettingsValidator.Validate(new WeatherApiConnectionSettings { ApiKey = "abc123" }));
}
```

`Connections/WeatherApiConnectionTypeTests.cs`. Construct the connection type with a fake model
resolver: validation never uses it, so its members just throw. Implementing them explicitly
avoids repeating the interface's generic constraint. `WeatherApiResponses` (in the same `Fakes/`
folder) holds the trimmed, canned API response bodies these tests and the action tests share.

`Umbraco.Community.Automate.WeatherApi.Tests/Connections/WeatherApiConnectionTypeTests.cs`
```csharp
using System.Net;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.WeatherApi.Connections;
using Umbraco.Community.Automate.WeatherApi.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.WeatherApi.Tests.Connections;

public class WeatherApiConnectionTypeTests
{
    [Fact]
    public async Task Unresolved_reference_fails_without_calling_the_api()
    {
        var handler = new StubHttpMessageHandler();

        var result = await CreateSut(handler).ValidateAsync(new WeatherApiConnectionSettings(), CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Valid_key_succeeds()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, WeatherApiResponses.Current));

        var result = await CreateSut(handler).ValidateAsync(new WeatherApiConnectionSettings { ApiKey = "abc123" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Success, result.Status);
        Assert.Contains("key=abc123", handler.Requests.Single().RequestUri!.Query);
    }

    [Fact]
    public async Task Rejected_key_reports_the_api_message()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, WeatherApiResponses.InvalidKey));

        var result = await CreateSut(handler).ValidateAsync(new WeatherApiConnectionSettings { ApiKey = "wrong" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Equal("API key is invalid.", result.Message);
    }

    private static WeatherApiConnectionType CreateSut(HttpMessageHandler handler)
        => new(new ConnectionTypeInfrastructure(new UnusedModelResolver()), new StubHttpClientFactory(handler));

    /// <summary>ValidateAsync never resolves models; this only satisfies the constructor.</summary>
    private sealed class UnusedModelResolver : IEditableModelResolver
    {
        object IEditableModelResolver.ResolveModel(string alias, Type modelType, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
        TModel IEditableModelResolver.ResolveModel<TModel>(string alias, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
    }
}
```

`Actions/GetCurrentWeatherActionTests.cs`. `ActionTestHarness` runs the action end to end; give
it every service its constructor asks for.

`Umbraco.Community.Automate.WeatherApi.Tests/Actions/GetCurrentWeatherActionTests.cs`
```csharp
using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.WeatherApi.Actions;
using Umbraco.Community.Automate.WeatherApi.Connections;
using Umbraco.Community.Automate.WeatherApi.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.WeatherApi.Tests.Actions;

public class GetCurrentWeatherActionTests
{
    [Fact]
    public async Task Returns_the_current_weather()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, WeatherApiResponses.Current));

        var result = await Run(handler, new GetCurrentWeatherSettings { Location = "London", Culture = "fr-FR" });

        Assert.Equal(ActionResultStatus.Success, result.Status);
        var output = Assert.IsType<GetCurrentWeatherOutput>(result.OutputData);
        Assert.Equal("London", output.LocationName);
        Assert.Equal(14.0, output.TemperatureC);
        Assert.Equal("Partly cloudy", output.Condition);
        // WeatherAPI.com returns protocol-relative icon URLs; the action makes them absolute.
        Assert.Equal("https://cdn.weatherapi.com/weather/64x64/day/116.png", output.ConditionIconUrl);

        var query = handler.Requests.Single().RequestUri!.Query;
        Assert.Contains("q=London", query);
        Assert.Contains("lang=fr", query);
    }

    [Fact]
    public async Task Missing_location_is_a_validation_error()
    {
        var result = await Run(new StubHttpMessageHandler(), new GetCurrentWeatherSettings { Location = "" });

        Assert.Equal(ActionResultStatus.Failed, result.Status);
        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
    }

    [Fact]
    public async Task Malformed_culture_is_a_validation_error()
    {
        // .NET accepts any well-formed name (e.g. "xx-YY") as a culture, so only malformed
        // names are rejected; a well-formed unknown one is passed through to WeatherAPI.com.
        var result = await Run(new StubHttpMessageHandler(), new GetCurrentWeatherSettings { Location = "London", Culture = "!!" });

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
    }

    [Fact]
    public async Task Unresolved_api_key_reference_is_a_configuration_error()
    {
        var handler = new StubHttpMessageHandler();

        var result = await Run(handler, new GetCurrentWeatherSettings { Location = "London" }, new WeatherApiConnectionSettings());

        Assert.Equal(StepRunErrorCategory.ConfigurationError, result.ErrorCategory);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Invalid_key_is_an_authentication_error()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, WeatherApiResponses.InvalidKey));

        var result = await Run(handler, new GetCurrentWeatherSettings { Location = "London" });

        Assert.Equal(StepRunErrorCategory.Authentication, result.ErrorCategory);
        Assert.Equal("API key is invalid.", result.Exception?.Message);
    }

    [Fact]
    public async Task Unknown_location_is_a_validation_error()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.BadRequest, WeatherApiResponses.NoLocation));

        var result = await Run(handler, new GetCurrentWeatherSettings { Location = "Nowhereville" });

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
    }

    private static Task<ActionResult> Run(
        StubHttpMessageHandler handler,
        GetCurrentWeatherSettings settings,
        WeatherApiConnectionSettings? connection = null)
        => ActionTestHarness.For<GetCurrentWeatherAction>()
            .WithService<IHttpClientFactory>(new StubHttpClientFactory(handler))
            .WithSettings(settings)
            .WithConnection("community.weatherApi", connection ?? new WeatherApiConnectionSettings { ApiKey = "abc123" })
            .ExecuteAsync();
}
```

`<Area>FieldTests.cs`. Every package has this test: a setting without a `Label` or `Description` doesn't fail anywhere, it just shows a raw `#uaFields_...` localization key in the backoffice.

`Umbraco.Community.Automate.WeatherApi.Tests/WeatherApiFieldTests.cs`
```csharp
using System.Reflection;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.WeatherApi.Connections;
using Xunit;

namespace Umbraco.Community.Automate.WeatherApi.Tests;

/// <summary>
/// A setting without a Label or Description shows a raw localization key in the backoffice
/// (e.g. #uaFields_...Description) instead of failing anywhere, so this test catches it.
/// </summary>
public class WeatherApiFieldTests
{
    [Fact]
    public void Every_setting_has_a_label_and_description()
    {
        var fields = typeof(WeatherApiConnectionType).Assembly.GetTypes()
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

The same for every package; see [full.md](full.md#wiring-into-the-repo) for the solution, Demo
site and root README snippets, and the [README skeleton](full.md#readme-skeleton). That includes
adding the package's configuration to the Demo site: `Umbraco:Automate:Secrets:<Area>:ApiKey`
with a fake placeholder (`"e2e-test"`) in `Umbraco.Community.Automate.Demo/appsettings.Development.json`,
and with a `your-...` value in `appsettings.Local.example.json`, the template for real credentials.

## Adding a second action

WeatherApi's own README names its second action, **Get Today's Weather** (`Actions/GetTodaysWeatherAction.cs`):
copy the first action's three files under a new name, give it its own alias
(`community.<area>.<name>`), add tests, and document it in the README. Once two actions call the
service the same way, as WeatherApi's do, move the shared request-building, JSON parsing and
error-classification logic into an `Api/<Area>RequestHelper.cs` (or `<Area>Client.cs` — see
[full.md](full.md#api-client-and-models)) used by both actions and the connection type's
`ValidateAsync`, with response shapes in `Models/`. Read `Packages/WeatherApi/Umbraco.Community.Automate.WeatherApi/Api/WeatherApiRequestHelper.cs`
directly for a real, maintained example of exactly that — it's not duplicated here to avoid a
second place for it to drift from.
