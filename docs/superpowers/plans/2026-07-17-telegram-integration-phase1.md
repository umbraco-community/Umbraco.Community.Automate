# Telegram Integration — Phase 1 (Send Action + Notification Channel) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a new `Umbraco.Community.Automate.Telegram` package that adds a Telegram connection type, a "Send Telegram Message" action, and a Telegram failure-notification channel to Umbraco.Automate — mirroring the existing GoogleSheets community package's structure and conventions.

**Architecture:** A plain (non-OAuth) `ConnectionTypeBase<TelegramConnectionSettings>` stores a bot token + default chat ID. A shared static `TelegramApiClient` helper (same pattern as GoogleSheets' `GoogleSheetsAuth` static class) wraps Telegram's Bot API `sendMessage`/`getMe` endpoints via the app's shared `"UmbracoAutomate"` named `HttpClient`, with a built-in single retry on HTTP 429. `SendMessageAction` (a workflow step) and `TelegramNotificationChannel` (an automatic failure-notification channel, configured independently per the existing Email/Webhook channel pattern) both call this shared client. A `TelegramMarkdownEscaper` static helper escapes MarkdownV2 reserved characters for text the package itself constructs (the notification channel's automation name/error text) — the send action's user-authored message is sent as raw MarkdownV2 without auto-escaping, matching how other free-text fields in this codebase aren't silently mutated.

**Tech Stack:** .NET 10 (net10.0), C# 13, `Umbraco.Automate.Core` (attribute-driven action/connection/notification-channel discovery), `System.Text.Json` (with `JsonNamingPolicy.SnakeCaseLower` for Telegram's snake_case wire format), xUnit + Moq (`Moq.Protected` for `HttpMessageHandler` stubbing) + Shouldly for tests, `Umbraco.Automate.Testing`'s `ActionTestHarness<TAction>` for action tests.

## Global Constraints

- Target framework: `net10.0`, `ImplicitUsings` enabled, `Nullable` enabled — matches every other project in this repo.
- Package versions are centrally managed (`Directory.Packages.props` at repo root, `ManagePackageVersionsCentrally=true`) — `PackageReference` entries in this package's `.csproj` must NOT include a `Version=` attribute. `Umbraco.Automate.Core`, `Umbraco.Automate.Testing`, `MinVer`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Moq`, `Shouldly` are already present in `Directory.Packages.props` — no new entries needed.
- Package name: `Umbraco.Community.Automate.Telegram`. Folder: `Telegram/` at repo root, containing `Umbraco.Community.Automate.Telegram/` (main project) and `Umbraco.Community.Automate.Telegram.Tests/` (test project), exactly mirroring `GoogleSheets/`.
- `MinVerTagPrefix` for this package is `telegram-v` (own `Directory.Build.props`, independent versioning from other packages in this monorepo).
- Namespaces: `Umbraco.Community.Automate.Telegram.Connection` (singular, matching GoogleSheets), `.Actions`, `.Notifications`, `.Client`.
- Telegram Bot API base URL: `https://api.telegram.org/bot{token}/{method}` — the token goes in the URL path, not an `Authorization` header.
- `StepRunErrorCategory` mapping for `SendMessageAction` failures: missing/empty text or missing chat ID → `Validation`; missing bot token on the connection → `ConfigurationError`; Telegram HTTP 401/403 → `Authentication`; Telegram HTTP 429 after one retry → `RateLimiting`; any other non-2xx → `InvalidResponse`.
- Notification channels in this codebase are **self-contained** — they do not reference a `Connection`. `TelegramNotificationChannelSettings` carries its own `BotToken`/`ChatId` fields directly, duplicating what a `TelegramConnectionType` connection stores, exactly like `WebhookNotificationChannelSettings.Secret`/`EmailNotificationChannelSettings.Recipients` are self-contained today. Do not attempt to wire the notification channel to a `Connection` — `NotificationChannelAttribute` has no `ConnectionTypeAlias` property, so the framework doesn't support it.
- This phase does **not** include the Phase 2 Telegram trigger (inbound bot commands) — that is deliberately out of scope for this plan and will get its own follow-up plan once this phase lands, per `docs/superpowers/specs/2026-07-16-telegram-integration-design.md`.

---

### Task 1: Package scaffolding + shared Telegram API client

**Files:**
- Create: `Telegram/Umbraco.Community.Automate.Telegram/Umbraco.Community.Automate.Telegram.csproj`
- Create: `Telegram/Umbraco.Community.Automate.Telegram/Directory.Build.props`
- Create: `Telegram/Umbraco.Community.Automate.Telegram.Tests/Umbraco.Community.Automate.Telegram.Tests.csproj`
- Create: `Telegram/Umbraco.Community.Automate.Telegram/Client/TelegramMarkdownEscaper.cs`
- Create: `Telegram/Umbraco.Community.Automate.Telegram/Client/TelegramApiResult.cs`
- Create: `Telegram/Umbraco.Community.Automate.Telegram/Client/TelegramApiClient.cs`
- Test: `Telegram/Umbraco.Community.Automate.Telegram.Tests/Client/TelegramMarkdownEscaperTests.cs`
- Test: `Telegram/Umbraco.Community.Automate.Telegram.Tests/Client/TelegramApiClientTests.cs`

**Interfaces:**
- Produces: `TelegramMarkdownEscaper.Escape(string text) : string`; `TelegramApiResult` record with `Success : bool`, `StatusCode : HttpStatusCode?`, `ErrorDescription : string?`, `RetryAfterSeconds : int?`, `MessageId : int?`; `TelegramApiClient.SendMessageAsync(IHttpClientFactory, string botToken, string chatId, string text, CancellationToken) : Task<TelegramApiResult>`; `TelegramApiClient.GetMeAsync(IHttpClientFactory, string botToken, CancellationToken) : Task<TelegramApiResult>`. Test helpers `TelegramApiClientTests.CreateHttpClientFactory(HttpMessageHandler) : IHttpClientFactory` and `TelegramApiClientTests.StubHandler(HttpStatusCode, string json, Action<HttpRequestMessage>? capture = null) : HttpMessageHandler` are `internal static` and reused by Tasks 2–4's tests (same test assembly).
- Consumes: nothing from other tasks (this is the foundation).

- [ ] **Step 1: Scaffold the two projects and verify they build**

Create `Telegram/Umbraco.Community.Automate.Telegram/Umbraco.Community.Automate.Telegram.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <Title>Umbraco Community Automate Telegram</Title>
        <Description>Telegram connection, actions, and notification channel for Umbraco Automate</Description>
        <PackageTags>umbraco automate automation telegram umbraco-marketplace</PackageTags>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Umbraco.Automate.Core" />
        <PackageReference Include="MinVer" PrivateAssets="All" />
    </ItemGroup>

    <ItemGroup>
        <None Include="README.md" Pack="true" PackagePath="" />
    </ItemGroup>

</Project>
```

Create `Telegram/Umbraco.Community.Automate.Telegram/Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Authors>Warren Buckley &amp; Umbraco Community</Authors>
    <PackageProjectUrl>https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Telegram/Umbraco.Community.Automate.Telegram</PackageProjectUrl>
    <RepositoryUrl>https://github.com/umbraco-community/Umbraco.Community.Automate</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <PropertyGroup>
    <!-- Tags this repo uses for Telegram releases, e.g. telegram-v1.0.0. IgnoreHeight keeps
         this package's version independent of unrelated commits elsewhere in the monorepo. -->
    <MinVerTagPrefix>telegram-v</MinVerTagPrefix>
    <MinVerIgnoreHeight>true</MinVerIgnoreHeight>
  </PropertyGroup>

  <PropertyGroup>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>
</Project>
```

Create a placeholder `Telegram/Umbraco.Community.Automate.Telegram/README.md` (Task 5 replaces this with the full README — a `None Include="README.md"` pack item requires the file to exist for the project to build):

```markdown
# Umbraco Community Automate Telegram

Telegram connection, actions, and notification channel for Umbraco Automate. (Full README written in Task 5.)
```

Create `Telegram/Umbraco.Community.Automate.Telegram.Tests/Umbraco.Community.Automate.Telegram.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <IsPackable>false</IsPackable>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="xunit" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="Moq" />
        <PackageReference Include="Shouldly" />
    </ItemGroup>
    <ItemGroup>
        <PackageReference Include="Umbraco.Automate.Testing" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\Umbraco.Community.Automate.Telegram\Umbraco.Community.Automate.Telegram.csproj" />
    </ItemGroup>
</Project>
```

Run: `dotnet build Telegram/Umbraco.Community.Automate.Telegram/Umbraco.Community.Automate.Telegram.csproj`
Expected: builds successfully (empty class library, no source files yet).

- [ ] **Step 2: Write the failing test for `TelegramMarkdownEscaper`**

Create `Telegram/Umbraco.Community.Automate.Telegram.Tests/Client/TelegramMarkdownEscaperTests.cs`:

```csharp
using Shouldly;
using Umbraco.Community.Automate.Telegram.Client;
using Xunit;

namespace Umbraco.Community.Automate.Telegram.Tests.Client;

public class TelegramMarkdownEscaperTests
{
    [Theory]
    [InlineData("hello world", "hello world")]
    [InlineData("Deploy v1.2.3 failed!", "Deploy v1\\.2\\.3 failed\\!")]
    [InlineData("100% done (ok)", "100% done \\(ok\\)")]
    [InlineData("a_b*c[d]e", "a\\_b\\*c\\[d\\]e")]
    [InlineData(@"back\slash", @"back\\slash")]
    public void Escape_escapes_all_markdownv2_reserved_characters(string input, string expected)
    {
        TelegramMarkdownEscaper.Escape(input).ShouldBe(expected);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests --filter TelegramMarkdownEscaperTests`
Expected: FAIL to compile — `TelegramMarkdownEscaper` does not exist yet.

- [ ] **Step 4: Implement `TelegramMarkdownEscaper`**

Create `Telegram/Umbraco.Community.Automate.Telegram/Client/TelegramMarkdownEscaper.cs`:

```csharp
using System.Text;

namespace Umbraco.Community.Automate.Telegram.Client;

/// <summary>
/// Escapes Telegram MarkdownV2 reserved characters in text this package constructs
/// (e.g. automation names, error messages interpolated into notification text).
/// </summary>
public static class TelegramMarkdownEscaper
{
    private const string ReservedCharacters = "_*[]()~`>#+-=|{}.!\\";

    public static string Escape(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (ReservedCharacters.Contains(c))
                builder.Append('\\');
            builder.Append(c);
        }
        return builder.ToString();
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests --filter TelegramMarkdownEscaperTests`
Expected: PASS (5 tests).

- [ ] **Step 6: Write the failing tests for `TelegramApiClient`**

Create `Telegram/Umbraco.Community.Automate.Telegram.Tests/Client/TelegramApiClientTests.cs`:

```csharp
using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using Shouldly;
using Umbraco.Community.Automate.Telegram.Client;
using Xunit;

namespace Umbraco.Community.Automate.Telegram.Tests.Client;

public class TelegramApiClientTests
{
    private const string TestBotToken = "123456:TEST-TOKEN";

    [Fact]
    public async Task SendMessageAsync_returns_success_with_message_id_on_ok_response()
    {
        var handler = StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":42}}""");

        var result = await TelegramApiClient.SendMessageAsync(
            CreateHttpClientFactory(handler), TestBotToken, "12345", "hello", CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.MessageId.ShouldBe(42);
    }

    [Fact]
    public async Task SendMessageAsync_posts_to_the_expected_telegram_url()
    {
        HttpRequestMessage? captured = null;
        var handler = StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""", req => captured = req);

        await TelegramApiClient.SendMessageAsync(
            CreateHttpClientFactory(handler), TestBotToken, "12345", "hello", CancellationToken.None);

        captured!.RequestUri!.ToString().ShouldBe($"https://api.telegram.org/bot{TestBotToken}/sendMessage");
        captured.Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    public async Task SendMessageAsync_returns_authentication_failure_on_401()
    {
        var handler = StubHandler(HttpStatusCode.Unauthorized, """{"ok":false,"description":"Unauthorized"}""");

        var result = await TelegramApiClient.SendMessageAsync(
            CreateHttpClientFactory(handler), "bad-token", "12345", "hello", CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        result.ErrorDescription.ShouldBe("Unauthorized");
    }

    [Fact]
    public async Task SendMessageAsync_retries_once_after_429_then_succeeds()
    {
        var callCount = 0;
        var handler = StubHandlerSequence(_ =>
        {
            callCount++;
            return callCount == 1
                ? (HttpStatusCode.TooManyRequests, """{"ok":false,"description":"Too Many Requests: retry after 0","parameters":{"retry_after":0}}""")
                : (HttpStatusCode.OK, """{"ok":true,"result":{"message_id":7}}""");
        });

        var result = await TelegramApiClient.SendMessageAsync(
            CreateHttpClientFactory(handler), TestBotToken, "12345", "hello", CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.MessageId.ShouldBe(7);
        callCount.ShouldBe(2);
    }

    [Fact]
    public async Task SendMessageAsync_fails_rate_limited_when_still_429_after_retry()
    {
        var handler = StubHandler(HttpStatusCode.TooManyRequests,
            """{"ok":false,"description":"Too Many Requests: retry after 0","parameters":{"retry_after":0}}""");

        var result = await TelegramApiClient.SendMessageAsync(
            CreateHttpClientFactory(handler), TestBotToken, "12345", "hello", CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GetMeAsync_returns_success_when_token_is_valid()
    {
        var handler = StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"id":1,"is_bot":true}}""");

        var result = await TelegramApiClient.GetMeAsync(CreateHttpClientFactory(handler), TestBotToken, CancellationToken.None);

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task GetMeAsync_returns_failure_when_token_is_invalid()
    {
        var handler = StubHandler(HttpStatusCode.Unauthorized, """{"ok":false,"description":"Unauthorized"}""");

        var result = await TelegramApiClient.GetMeAsync(CreateHttpClientFactory(handler), "bad-token", CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.ErrorDescription.ShouldBe("Unauthorized");
    }

    internal static IHttpClientFactory CreateHttpClientFactory(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("UmbracoAutomate")).Returns(client);
        return factory.Object;
    }

    internal static HttpMessageHandler StubHandler(HttpStatusCode code, string json, Action<HttpRequestMessage>? capture = null)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capture?.Invoke(req);
                return Task.FromResult(new HttpResponseMessage(code)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                });
            });
        return mock.Object;
    }

    internal static HttpMessageHandler StubHandlerSequence(Func<HttpRequestMessage, (HttpStatusCode Code, string Json)> responder)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                var (code, json) = responder(req);
                return Task.FromResult(new HttpResponseMessage(code)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                });
            });
        return mock.Object;
    }
}
```

- [ ] **Step 7: Run the tests to verify they fail**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests --filter TelegramApiClientTests`
Expected: FAIL to compile — `TelegramApiClient` and `TelegramApiResult` do not exist yet.

- [ ] **Step 8: Implement `TelegramApiResult` and `TelegramApiClient`**

Create `Telegram/Umbraco.Community.Automate.Telegram/Client/TelegramApiResult.cs`:

```csharp
using System.Net;

namespace Umbraco.Community.Automate.Telegram.Client;

/// <summary>
/// The outcome of a Telegram Bot API call.
/// </summary>
public sealed record TelegramApiResult
{
    public required bool Success { get; init; }
    public HttpStatusCode? StatusCode { get; init; }
    public string? ErrorDescription { get; init; }
    public int? RetryAfterSeconds { get; init; }
    public int? MessageId { get; init; }

    public static TelegramApiResult Succeeded(int? messageId = null) =>
        new() { Success = true, MessageId = messageId };

    public static TelegramApiResult Failed(HttpStatusCode statusCode, string? errorDescription, int? retryAfterSeconds = null) =>
        new() { Success = false, StatusCode = statusCode, ErrorDescription = errorDescription, RetryAfterSeconds = retryAfterSeconds };
}
```

Create `Telegram/Umbraco.Community.Automate.Telegram/Client/TelegramApiClient.cs`:

```csharp
using System.Net;
using System.Text;
using System.Text.Json;

namespace Umbraco.Community.Automate.Telegram.Client;

/// <summary>
/// Thin wrapper around the Telegram Bot API's sendMessage and getMe endpoints. Shared by
/// SendMessageAction, TelegramNotificationChannel, and TelegramConnectionType's connectivity
/// check, since they all authenticate and call Telegram the same way. Retries once on HTTP 429
/// (rate limited) using Telegram's own retry_after value before returning a failure.
/// </summary>
public static class TelegramApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static async Task<TelegramApiResult> SendMessageAsync(
        IHttpClientFactory httpClientFactory, string botToken, string chatId, string text, CancellationToken cancellationToken)
    {
        var firstAttempt = await SendMessageOnceAsync(httpClientFactory, botToken, chatId, text, cancellationToken);

        if (firstAttempt.StatusCode != HttpStatusCode.TooManyRequests)
            return firstAttempt;

        var delaySeconds = firstAttempt.RetryAfterSeconds ?? 1;
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

        return await SendMessageOnceAsync(httpClientFactory, botToken, chatId, text, cancellationToken);
    }

    public static async Task<TelegramApiResult> GetMeAsync(
        IHttpClientFactory httpClientFactory, string botToken, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("UmbracoAutomate");
        var url = $"https://api.telegram.org/bot{botToken}/getMe";

        using var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
            return TelegramApiResult.Succeeded();

        var errorBody = JsonSerializer.Deserialize<TelegramResponse<object>>(body, JsonOptions);
        return TelegramApiResult.Failed(response.StatusCode, errorBody?.Description);
    }

    private static async Task<TelegramApiResult> SendMessageOnceAsync(
        IHttpClientFactory httpClientFactory, string botToken, string chatId, string text, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("UmbracoAutomate");
        var url = $"https://api.telegram.org/bot{botToken}/sendMessage";

        var payload = JsonSerializer.Serialize(new
        {
            chat_id = chatId,
            text,
            parse_mode = "MarkdownV2",
        });

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var parsed = JsonSerializer.Deserialize<TelegramResponse<TelegramMessageResult>>(body, JsonOptions);
            return TelegramApiResult.Succeeded(parsed?.Result?.MessageId);
        }

        var errorBody = JsonSerializer.Deserialize<TelegramResponse<object>>(body, JsonOptions);
        return TelegramApiResult.Failed(response.StatusCode, errorBody?.Description, errorBody?.Parameters?.RetryAfter);
    }

    private sealed class TelegramResponse<T>
    {
        public bool Ok { get; set; }
        public T? Result { get; set; }
        public string? Description { get; set; }
        public TelegramResponseParameters? Parameters { get; set; }
    }

    private sealed class TelegramResponseParameters
    {
        public int? RetryAfter { get; set; }
    }

    private sealed class TelegramMessageResult
    {
        public int MessageId { get; set; }
    }
}
```

- [ ] **Step 9: Run the tests to verify they pass**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests`
Expected: PASS (12 tests total — 5 escaper + 7 API client).

- [ ] **Step 10: Commit**

```bash
git add Telegram/
git commit -m "$(cat <<'EOF'
add: scaffold Telegram package with shared API client and Markdown escaper

Foundation for the connection type, send action, and notification
channel added in later commits — one place for the sendMessage/getMe
HTTP calls and the single-retry-on-429 rate-limit handling they share.
EOF
)"
```

---

### Task 2: `TelegramConnectionType` + `TelegramConnectionSettings`

**Files:**
- Create: `Telegram/Umbraco.Community.Automate.Telegram/Connection/TelegramConnectionSettings.cs`
- Create: `Telegram/Umbraco.Community.Automate.Telegram/Connection/TelegramConnectionType.cs`
- Test: `Telegram/Umbraco.Community.Automate.Telegram.Tests/Connection/TelegramConnectionTypeTests.cs`

**Interfaces:**
- Consumes: `TelegramApiClient.GetMeAsync` and the test helpers `TelegramApiClientTests.CreateHttpClientFactory`/`StubHandler` from Task 1.
- Produces: `TelegramConnectionSettings { string? BotToken; string? ChatId; }` and `TelegramConnectionType : ConnectionTypeBase<TelegramConnectionSettings>` with alias `"telegram"` — consumed by `SendMessageAction` (Task 3) via `context.Connection.GetSettings<TelegramConnectionSettings>()` and by `[Action(..., ConnectionTypeAlias = "telegram")]`.

- [ ] **Step 1: Write the failing tests for `TelegramConnectionType`**

Create `Telegram/Umbraco.Community.Automate.Telegram.Tests/Connection/TelegramConnectionTypeTests.cs`:

```csharp
using System.Net;
using Moq;
using Shouldly;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Telegram.Connection;
using Umbraco.Community.Automate.Telegram.Tests.Client;
using Xunit;

namespace Umbraco.Community.Automate.Telegram.Tests.Connection;

public class TelegramConnectionTypeTests
{
    [Fact]
    public async Task ValidateAsync_returns_success_when_bot_token_is_valid()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"id":1,"is_bot":true}}""");
        var connectionType = new TelegramConnectionType(
            new ConnectionTypeInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler));

        var result = await connectionType.ValidateAsync(
            new TelegramConnectionSettings { BotToken = "123456:TEST-TOKEN", ChatId = "123" },
            CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Success);
    }

    [Fact]
    public async Task ValidateAsync_returns_failure_when_bot_token_is_rejected()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.Unauthorized, """{"ok":false,"description":"Unauthorized"}""");
        var connectionType = new TelegramConnectionType(
            new ConnectionTypeInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler));

        var result = await connectionType.ValidateAsync(
            new TelegramConnectionSettings { BotToken = "bad-token", ChatId = "123" },
            CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Failure);
        result.Message.ShouldBe("Unauthorized");
    }

    [Fact]
    public async Task ValidateAsync_returns_failure_when_bot_token_is_missing()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, "{}");
        var connectionType = new TelegramConnectionType(
            new ConnectionTypeInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler));

        var result = await connectionType.ValidateAsync(
            new TelegramConnectionSettings { BotToken = null, ChatId = "123" },
            CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Failure);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests --filter TelegramConnectionTypeTests`
Expected: FAIL to compile — `TelegramConnectionType` and `TelegramConnectionSettings` do not exist yet.

- [ ] **Step 3: Implement `TelegramConnectionSettings` and `TelegramConnectionType`**

Create `Telegram/Umbraco.Community.Automate.Telegram/Connection/TelegramConnectionSettings.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Telegram.Connection;

/// <summary>
/// Settings for the Telegram connection type.
/// </summary>
public sealed class TelegramConnectionSettings
{
    [Field(Label = "Bot Token", Description = "The token issued by @BotFather for your bot.", IsSensitive = true)]
    [Required]
    public string? BotToken { get; set; }

    [Field(Label = "Chat ID",
        Description = "The default chat, group, or channel ID this bot sends messages to.",
        SortOrder = 1)]
    [Required]
    public string? ChatId { get; set; }
}
```

Create `Telegram/Umbraco.Community.Automate.Telegram/Connection/TelegramConnectionType.cs`:

```csharp
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Telegram.Client;

namespace Umbraco.Community.Automate.Telegram.Connection;

/// <summary>
/// Connection type for a Telegram bot, authenticated with a static bot token (no OAuth).
/// </summary>
[ConnectionType("telegram", "Telegram",
    Group = "Messaging",
    Icon = "icon-telegram",
    Description = "Connect a Telegram bot to send messages from your workflows.")]
public sealed class TelegramConnectionType : ConnectionTypeBase<TelegramConnectionSettings>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TelegramConnectionType(ConnectionTypeInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override async Task<ConnectionValidationResult> ValidateAsync(object? settings, CancellationToken cancellationToken)
    {
        if (settings is not TelegramConnectionSettings typed || string.IsNullOrWhiteSpace(typed.BotToken))
            return ConnectionValidationResult.Failure("A bot token is required.");

        var result = await TelegramApiClient.GetMeAsync(_httpClientFactory, typed.BotToken, cancellationToken);

        return result.Success
            ? ConnectionValidationResult.Success("Bot token verified.")
            : ConnectionValidationResult.Failure(result.ErrorDescription ?? "Telegram rejected the bot token.");
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests --filter TelegramConnectionTypeTests`
Expected: PASS (3 tests).

- [ ] **Step 5: Run the full test suite to check for regressions**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests`
Expected: PASS (15 tests total).

- [ ] **Step 6: Commit**

```bash
git add Telegram/
git commit -m "$(cat <<'EOF'
add: Telegram connection type with bot-token verification

Plain bot-token connection (no OAuth) — ValidateAsync calls Telegram's
getMe endpoint so the backoffice connection editor can confirm the
token works before it's used by an action.
EOF
)"
```

---

### Task 3: `SendMessageAction` + `SendMessageSettings` + `SendMessageOutput`

**Files:**
- Create: `Telegram/Umbraco.Community.Automate.Telegram/Actions/SendMessageSettings.cs`
- Create: `Telegram/Umbraco.Community.Automate.Telegram/Actions/SendMessageOutput.cs`
- Create: `Telegram/Umbraco.Community.Automate.Telegram/Actions/SendMessageAction.cs`
- Test: `Telegram/Umbraco.Community.Automate.Telegram.Tests/Actions/SendMessageActionTests.cs`

**Interfaces:**
- Consumes: `TelegramApiClient.SendMessageAsync` (Task 1), `TelegramConnectionSettings` (Task 2) via `context.Connection.GetSettings<TelegramConnectionSettings>()`, test helpers `TelegramApiClientTests.CreateHttpClientFactory`/`StubHandler` (Task 1).
- Produces: `SendMessageOutput { int MessageId; DateTimeOffset SentAt; }`, action alias `"telegram.sendMessage"` — no other task consumes this directly, it's a leaf workflow step.

- [ ] **Step 1: Write the failing tests for `SendMessageAction`**

Create `Telegram/Umbraco.Community.Automate.Telegram.Tests/Actions/SendMessageActionTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using Shouldly;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Telegram.Actions;
using Umbraco.Community.Automate.Telegram.Connection;
using Umbraco.Community.Automate.Telegram.Tests.Client;
using Xunit;

namespace Umbraco.Community.Automate.Telegram.Tests.Actions;

public class SendMessageActionTests
{
    private const string TestBotToken = "123456:TEST-TOKEN";

    [Fact]
    public async Task ExecuteAsync_sends_message_and_returns_message_id_on_success()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":99}}""");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (SendMessageOutput)result.OutputData!;
        output.MessageId.ShouldBe(99);
    }

    [Fact]
    public async Task ExecuteAsync_uses_connection_chat_id_when_no_override_is_set()
    {
        HttpRequestMessage? captured = null;
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""", req => captured = req);

        await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        var requestBody = await captured!.Content!.ReadAsStringAsync();
        JsonDocument.Parse(requestBody).RootElement.GetProperty("chat_id").GetString().ShouldBe("999");
    }

    [Fact]
    public async Task ExecuteAsync_uses_per_action_chat_id_override_when_set()
    {
        HttpRequestMessage? captured = null;
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""", req => captured = req);

        await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello", ChatId = "override-chat" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        var requestBody = await captured!.Content!.ReadAsStringAsync();
        JsonDocument.Parse(requestBody).RootElement.GetProperty("chat_id").GetString().ShouldBe("override-chat");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_text_is_empty()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, "{}");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_configuration_error_when_bot_token_missing()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, "{}");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = null, ChatId = "999" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.ConfigurationError);
    }

    [Fact]
    public async Task ExecuteAsync_fails_configuration_error_when_no_chat_id_available()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, "{}");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = null })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.ConfigurationError);
    }

    [Fact]
    public async Task ExecuteAsync_fails_authentication_on_401()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.Unauthorized, """{"ok":false,"description":"Unauthorized"}""");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Authentication);
    }

    [Fact]
    public async Task ExecuteAsync_fails_rate_limiting_when_still_429_after_retry()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.TooManyRequests,
            """{"ok":false,"description":"Too Many Requests","parameters":{"retry_after":0}}""");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.RateLimiting);
    }

    [Fact]
    public async Task ExecuteAsync_fails_invalid_response_on_other_errors()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.BadRequest,
            """{"ok":false,"description":"Bad Request: chat not found"}""");

        var result = await ActionTestHarness.For<SendMessageAction>()
            .WithService(TelegramApiClientTests.CreateHttpClientFactory(handler))
            .WithSettings(new SendMessageSettings { Text = "Hello" })
            .WithConnection("telegram", new TelegramConnectionSettings { BotToken = TestBotToken, ChatId = "999" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
        result.Exception!.Message.ShouldContain("chat not found");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests --filter SendMessageActionTests`
Expected: FAIL to compile — `SendMessageAction`, `SendMessageSettings`, `SendMessageOutput` do not exist yet.

- [ ] **Step 3: Implement `SendMessageSettings`, `SendMessageOutput`, `SendMessageAction`**

Create `Telegram/Umbraco.Community.Automate.Telegram/Actions/SendMessageSettings.cs`:

```csharp
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Telegram.Actions;

/// <summary>
/// Settings for the <see cref="SendMessageAction"/>.
/// </summary>
public sealed class SendMessageSettings
{
    [Field(Label = "Message",
        Description = "The message to send, formatted as Telegram MarkdownV2. Reserved characters " +
            "(_ * [ ] ( ) ~ ` > # + - = | { } . ! \\) must be escaped with a backslash, including in bound values.",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SupportsBindings = true)]
    public string Text { get; set; } = string.Empty;

    [Field(Label = "Chat ID override",
        Description = "Optional. Overrides the connection's default chat ID for this step.",
        SortOrder = 1,
        SupportsBindings = true)]
    public string? ChatId { get; set; }
}
```

Create `Telegram/Umbraco.Community.Automate.Telegram/Actions/SendMessageOutput.cs`:

```csharp
namespace Umbraco.Community.Automate.Telegram.Actions;

/// <summary>
/// Output produced by the <see cref="SendMessageAction"/>.
/// </summary>
public sealed class SendMessageOutput
{
    public int MessageId { get; init; }
    public DateTimeOffset SentAt { get; init; }
}
```

Create `Telegram/Umbraco.Community.Automate.Telegram/Actions/SendMessageAction.cs`:

```csharp
using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Telegram.Client;
using Umbraco.Community.Automate.Telegram.Connection;

namespace Umbraco.Community.Automate.Telegram.Actions;

/// <summary>
/// Sends a text message to a Telegram chat via the connected bot.
/// </summary>
[Action("telegram.sendMessage", "Send Telegram Message",
    Description = "Sends a text message to a Telegram chat via a bot.",
    Group = "Messaging",
    Icon = "icon-telegram",
    ConnectionTypeAlias = "telegram")]
public sealed class SendMessageAction : ActionBase<SendMessageSettings, SendMessageOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SendMessageAction(ActionInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<SendMessageSettings>();

        if (string.IsNullOrWhiteSpace(settings.Text))
            return ActionResult.Failed(new ArgumentException("Message text is required."), StepRunErrorCategory.Validation);

        var connectionSettings = context.Connection?.GetSettings<TelegramConnectionSettings>();
        if (string.IsNullOrWhiteSpace(connectionSettings?.BotToken))
            return ActionResult.Failed(
                new InvalidOperationException("Telegram bot token is not configured on the connection."),
                StepRunErrorCategory.ConfigurationError);

        var chatId = string.IsNullOrWhiteSpace(settings.ChatId) ? connectionSettings.ChatId : settings.ChatId;
        if (string.IsNullOrWhiteSpace(chatId))
            return ActionResult.Failed(
                new InvalidOperationException("No chat ID configured on the connection or this step."),
                StepRunErrorCategory.ConfigurationError);

        var result = await TelegramApiClient.SendMessageAsync(_httpClientFactory, connectionSettings.BotToken, chatId, settings.Text, cancellationToken);

        if (result.Success)
            return Success(new SendMessageOutput { MessageId = result.MessageId ?? 0, SentAt = DateTimeOffset.UtcNow });

        return result.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                ActionResult.Failed(new InvalidOperationException(result.ErrorDescription ?? "Telegram rejected the bot token."), StepRunErrorCategory.Authentication),
            HttpStatusCode.TooManyRequests =>
                ActionResult.Failed(new InvalidOperationException(result.ErrorDescription ?? "Telegram rate-limited this bot."), StepRunErrorCategory.RateLimiting),
            _ =>
                ActionResult.Failed(new InvalidOperationException(result.ErrorDescription ?? $"Telegram API returned {(int?)result.StatusCode}."), StepRunErrorCategory.InvalidResponse),
        };
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests --filter SendMessageActionTests`
Expected: PASS (9 tests).

- [ ] **Step 5: Run the full test suite to check for regressions**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests`
Expected: PASS (24 tests total).

- [ ] **Step 6: Commit**

```bash
git add Telegram/
git commit -m "$(cat <<'EOF'
add: Send Telegram Message action

Workflow step that posts a MarkdownV2 message to the connection's
default chat, with an optional per-step chat ID override and
StepRunErrorCategory mapping for auth/rate-limit/other API failures.
EOF
)"
```

---

### Task 4: `TelegramNotificationChannel` + `TelegramNotificationChannelSettings`

**Files:**
- Create: `Telegram/Umbraco.Community.Automate.Telegram/Notifications/TelegramNotificationChannelSettings.cs`
- Create: `Telegram/Umbraco.Community.Automate.Telegram/Notifications/TelegramNotificationChannel.cs`
- Test: `Telegram/Umbraco.Community.Automate.Telegram.Tests/Notifications/TelegramNotificationChannelTests.cs`

**Interfaces:**
- Consumes: `TelegramApiClient.SendMessageAsync` and `TelegramMarkdownEscaper.Escape` (Task 1), test helpers `TelegramApiClientTests.CreateHttpClientFactory`/`StubHandler` (Task 1). Does **not** consume `TelegramConnectionSettings` (Task 2) — notification channels are self-contained per the Global Constraints note.
- Produces: notification channel alias `"telegram"`, registered automatically alongside the built-in Email/Webhook channels — no other task consumes this directly.

- [ ] **Step 1: Write the failing tests for `TelegramNotificationChannel`**

Create `Telegram/Umbraco.Community.Automate.Telegram.Tests/Notifications/TelegramNotificationChannelTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.Automate.Core.Notifications.Channels;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Telegram.Notifications;
using Umbraco.Community.Automate.Telegram.Tests.Client;
using Xunit;

namespace Umbraco.Community.Automate.Telegram.Tests.Notifications;

public class TelegramNotificationChannelTests
{
    private const string TestBotToken = "123456:TEST-TOKEN";

    [Fact]
    public async Task NotifyAsync_sends_escaped_subject_and_body_as_markdown()
    {
        HttpRequestMessage? captured = null;
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""", req => captured = req);

        var channel = new TelegramNotificationChannel(
            new NotificationChannelInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler),
            NullLogger<TelegramNotificationChannel>.Instance);

        await channel.NotifyAsync(
            new NotificationMessage { Subject = "Run failed!", TextBody = "Automation MyFlow (v3) failed." },
            new TelegramNotificationChannelSettings { BotToken = TestBotToken, ChatId = "999" },
            CancellationToken.None);

        var requestBody = await captured!.Content!.ReadAsStringAsync();
        var sentText = JsonDocument.Parse(requestBody).RootElement.GetProperty("text").GetString();

        sentText.ShouldBe("*Run failed\\!*\nAutomation MyFlow \\(v3\\) failed\\.");
    }

    [Fact]
    public async Task NotifyAsync_sends_subject_only_when_body_is_empty()
    {
        HttpRequestMessage? captured = null;
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""", req => captured = req);

        var channel = new TelegramNotificationChannel(
            new NotificationChannelInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler),
            NullLogger<TelegramNotificationChannel>.Instance);

        await channel.NotifyAsync(
            new NotificationMessage { Subject = "Run failed" },
            new TelegramNotificationChannelSettings { BotToken = TestBotToken, ChatId = "999" },
            CancellationToken.None);

        var requestBody = await captured!.Content!.ReadAsStringAsync();
        JsonDocument.Parse(requestBody).RootElement.GetProperty("text").GetString().ShouldBe("*Run failed*");
    }

    [Fact]
    public async Task NotifyAsync_does_not_throw_when_telegram_api_call_fails()
    {
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.BadRequest, """{"ok":false,"description":"Bad Request"}""");

        var channel = new TelegramNotificationChannel(
            new NotificationChannelInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler),
            NullLogger<TelegramNotificationChannel>.Instance);

        await Should.NotThrowAsync(() => channel.NotifyAsync(
            new NotificationMessage { Subject = "Run failed" },
            new TelegramNotificationChannelSettings { BotToken = TestBotToken, ChatId = "999" },
            CancellationToken.None));
    }

    [Theory]
    [InlineData(null, "999")]
    [InlineData(TestBotToken, null)]
    public async Task NotifyAsync_skips_sending_when_bot_token_or_chat_id_is_missing(string? botToken, string? chatId)
    {
        HttpRequestMessage? captured = null;
        var handler = TelegramApiClientTests.StubHandler(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""", req => captured = req);

        var channel = new TelegramNotificationChannel(
            new NotificationChannelInfrastructure(Mock.Of<IEditableModelResolver>()),
            TelegramApiClientTests.CreateHttpClientFactory(handler),
            NullLogger<TelegramNotificationChannel>.Instance);

        await channel.NotifyAsync(
            new NotificationMessage { Subject = "Run failed" },
            new TelegramNotificationChannelSettings { BotToken = botToken, ChatId = chatId },
            CancellationToken.None);

        captured.ShouldBeNull();
    }
}
```

Note: `TestBotToken` is declared `private const string` at the top of this class, so it's already a compile-time constant and usable directly in `[InlineData(TestBotToken, null)]`.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests --filter TelegramNotificationChannelTests`
Expected: FAIL to compile — `TelegramNotificationChannel` and `TelegramNotificationChannelSettings` do not exist yet.

- [ ] **Step 3: Implement `TelegramNotificationChannelSettings` and `TelegramNotificationChannel`**

Create `Telegram/Umbraco.Community.Automate.Telegram/Notifications/TelegramNotificationChannelSettings.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Telegram.Notifications;

/// <summary>
/// Settings for the Telegram notification channel. Self-contained (not linked to a Connection),
/// matching the existing Email/Webhook notification channel pattern.
/// </summary>
public sealed class TelegramNotificationChannelSettings
{
    [Field(Label = "Bot Token", Description = "The token issued by @BotFather for your bot.", IsSensitive = true)]
    [Required]
    public string? BotToken { get; set; }

    [Field(Label = "Chat ID", Description = "The chat, group, or channel ID to notify.", SortOrder = 1)]
    [Required]
    public string? ChatId { get; set; }
}
```

Create `Telegram/Umbraco.Community.Automate.Telegram/Notifications/TelegramNotificationChannel.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Notifications.Channels;
using Umbraco.Community.Automate.Telegram.Client;

namespace Umbraco.Community.Automate.Telegram.Notifications;

/// <summary>
/// Notification channel that sends a Telegram message when a run matches the automation's
/// configured notification conditions (e.g. Failed).
/// </summary>
[NotificationChannel("telegram", "Telegram",
    Description = "Sends a Telegram message when a run matches the configured notification conditions.",
    Icon = "icon-telegram")]
public sealed class TelegramNotificationChannel : NotificationChannelBase<TelegramNotificationChannelSettings>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramNotificationChannel> _logger;

    public TelegramNotificationChannel(
        NotificationChannelInfrastructure infrastructure,
        IHttpClientFactory httpClientFactory,
        ILogger<TelegramNotificationChannel> logger)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task NotifyAsync(
        NotificationMessage message, TelegramNotificationChannelSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.BotToken) || string.IsNullOrWhiteSpace(settings.ChatId))
        {
            _logger.LogWarning("Telegram notification channel is missing a bot token or chat ID, skipping");
            return;
        }

        var text = BuildMessageText(message);
        var result = await TelegramApiClient.SendMessageAsync(_httpClientFactory, settings.BotToken, settings.ChatId, text, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning("Telegram notification failed: {Description}", result.ErrorDescription);
        }
    }

    private static string BuildMessageText(NotificationMessage message)
    {
        var subject = TelegramMarkdownEscaper.Escape(message.Subject ?? "Automation notification");
        var body = string.IsNullOrWhiteSpace(message.TextBody) ? null : TelegramMarkdownEscaper.Escape(message.TextBody);
        return body is null ? $"*{subject}*" : $"*{subject}*\n{body}";
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests --filter TelegramNotificationChannelTests`
Expected: PASS (5 tests).

- [ ] **Step 5: Run the full test suite to check for regressions**

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests`
Expected: PASS (29 tests total).

- [ ] **Step 6: Commit**

```bash
git add Telegram/
git commit -m "$(cat <<'EOF'
add: Telegram failure-notification channel

Self-contained channel (own bot token + chat ID, no Connection link)
alongside the existing Email and Webhook channels, so an automation
can notify a Telegram chat automatically based on its NotifyOn flags.
EOF
)"
```

---

### Task 5: README, solution/Demo wiring, and final verification

**Files:**
- Modify: `Telegram/Umbraco.Community.Automate.Telegram/README.md` (replace Task 1's placeholder)
- Modify: `Umbraco.Community.Automate.slnx`
- Modify: `Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj`

**Interfaces:**
- Consumes: nothing new — this task wires up and documents what Tasks 1–4 produced.
- Produces: nothing consumed by other tasks — this is the final task in this plan.

- [ ] **Step 1: Write the full README**

Replace the contents of `Telegram/Umbraco.Community.Automate.Telegram/README.md`:

```markdown
# Umbraco Community Automate Telegram

Telegram connection, actions, and notification channel for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate).

## Overview

Umbraco.Community.Automate.Telegram is a provider package that adds Telegram messaging to Umbraco Automate. It contributes a Telegram connection type (authenticated with a bot token), a **Send Telegram Message** action, and a Telegram notification channel — so an automation can post to a Telegram chat as a workflow step, or automatically when a run fails.

## Key Features

- **Telegram connection type** — stores a bot token (from [@BotFather](https://t.me/BotFather)) and a default chat ID, verified via Telegram's `getMe` API
- **Send Telegram Message action** — sends a MarkdownV2-formatted message to the connection's default chat, with an optional per-step chat ID override
- **Telegram notification channel** — notifies a Telegram chat automatically based on an automation's run-status settings (Failed, Completed, etc.), alongside the built-in Email and Webhook channels
- **Rate-limit aware** — retries once after Telegram's `retry_after` delay on HTTP 429 before failing

## Installation

```bash
dotnet add package Umbraco.Community.Automate.Telegram
```

## Configuration

1. Create a bot with [@BotFather](https://t.me/BotFather) on Telegram and copy the bot token it gives you.
2. Add the bot to the chat, group, or channel you want it to post to, and find that chat's ID (e.g. via `https://api.telegram.org/bot<token>/getUpdates` after sending the bot a message, or a chat-ID lookup bot).
3. Create a Telegram connection in a workspace from the backoffice, paste in the bot token and chat ID, and verify it — this powers the **Send Telegram Message** action.
4. To get failure notifications, open an automation's notification settings and enable the **Telegram** channel, entering the same bot token and chat ID directly — notification channels configure their own credentials, independent of any connection.

## Message formatting

Messages are sent using Telegram's MarkdownV2 parse mode. The **Send Telegram Message** action sends your text as-is — if your message includes literal `_ * [ ] ( ) ~ \` > # + - = | { } . !` characters (including in bound values from earlier steps), escape them with a backslash or Telegram will reject the message. The notification channel escapes the automation name and error details automatically since it builds the message for you.

## Troubleshooting

If a run fails with an authentication error, the bot token is invalid or was revoked — generate a new one with @BotFather and update the connection.

If a run fails saying Telegram couldn't find the chat, double-check the chat ID and make sure the bot has been added to that chat (or has an open conversation with it, for a private chat).

If a run fails with a rate-limiting error, Telegram is throttling this bot — the action already retries once automatically; if it still fails, wait before retrying the run.

## License

MIT — see [LICENSE](../../LICENSE) for details.
```

- [ ] **Step 2: Add the new projects to the solution**

Edit `Umbraco.Community.Automate.slnx`:

```xml
<Solution>
  <Folder Name="/GoogleSheets/">
    <Project Path="GoogleSheets/Umbraco.Community.Automate.GoogleSheets.Tests/Umbraco.Community.Automate.GoogleSheets.Tests.csproj" />
    <Project Path="GoogleSheets/Umbraco.Community.Automate.GoogleSheets/Umbraco.Community.Automate.GoogleSheets.csproj" />
  </Folder>
  <Folder Name="/Telegram/">
    <Project Path="Telegram/Umbraco.Community.Automate.Telegram.Tests/Umbraco.Community.Automate.Telegram.Tests.csproj" />
    <Project Path="Telegram/Umbraco.Community.Automate.Telegram/Umbraco.Community.Automate.Telegram.csproj" />
  </Folder>
  <Project Path="Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj" />
</Solution>
```

- [ ] **Step 3: Reference the new package from the Demo site**

Edit `Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj`, adding a second `ProjectReference` next to the existing GoogleSheets one:

```xml
  <ItemGroup>
    <ProjectReference Include="..\GoogleSheets\Umbraco.Community.Automate.GoogleSheets\Umbraco.Community.Automate.GoogleSheets.csproj" />
    <ProjectReference Include="..\Telegram\Umbraco.Community.Automate.Telegram\Umbraco.Community.Automate.Telegram.csproj" />
  </ItemGroup>
```

- [ ] **Step 4: Build the full solution and run the full test suite**

Run: `dotnet build Umbraco.Community.Automate.slnx`
Expected: builds successfully, including the Demo site with the new Telegram project reference.

Run: `dotnet test Telegram/Umbraco.Community.Automate.Telegram.Tests`
Expected: PASS (29 tests).

- [ ] **Step 5: Manually verify in the Demo site**

Run: `dotnet run --project Umbraco.Community.Automate.Demo`

In the backoffice:
1. Create a Telegram connection with a real bot token + chat ID (from a test bot created via @BotFather), and confirm it validates successfully.
2. Build a test automation with a **Send Telegram Message** step using that connection, run it, and confirm the message arrives in the target chat with MarkdownV2 formatting (e.g. `*bold*`) rendered correctly.
3. Add the Telegram notification channel to that automation's notification settings (own bot token + chat ID), configure it to notify on `Failed`, force a run failure (e.g. temporarily break a step's settings), and confirm the Telegram notification arrives.

Stop the Demo site once verified (`Ctrl+C`).

- [ ] **Step 6: Commit**

```bash
git add Telegram/ Umbraco.Community.Automate.slnx Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj
git commit -m "$(cat <<'EOF'
docs: add Telegram package README and wire it into the solution/Demo

Completes Phase 1 (connection, send action, notification channel) —
the Telegram package now builds as part of the full solution and is
referenced by the Demo site for manual end-to-end verification.
EOF
)"
```

---

## Self-Review Notes

- **Spec coverage:** all three Phase 1 deliverables from `docs/superpowers/specs/2026-07-16-telegram-integration-design.md` are covered — connection type (Task 2), send action (Task 3), notification channel (Task 4). The Phase 2 trigger is explicitly out of scope per the Global Constraints and gets its own future plan.
- **Architecture correction from the spec:** the spec assumed the notification channel would "resolve the automation's TelegramConnectionType the same way SendMessageAction does." Reading the actual `NotificationChannelAttribute`/`INotificationChannel` source (Task 1 research) showed notification channels have no `ConnectionTypeAlias` concept at all — they're self-contained, exactly like the existing Email/Webhook channels. `TelegramNotificationChannelSettings` therefore carries its own `BotToken`/`ChatId` fields rather than referencing a `Connection`. This is called out explicitly in Global Constraints and Task 4 so it isn't missed during implementation.
- **Type consistency:** `TelegramApiResult`, `TelegramApiClient`, `TelegramMarkdownEscaper` (Task 1) are used with identical signatures in Tasks 2–4. `TelegramConnectionSettings` (Task 2) and `SendMessageSettings`/`SendMessageOutput` (Task 3) match between their definitions and every test's usage. `TelegramApiClientTests.CreateHttpClientFactory`/`StubHandler` (Task 1, `internal static`) are reused verbatim by Tasks 2–4's test files rather than being redefined.
- **No placeholders:** every step has complete, real code — no TODOs or "add appropriate error handling" left for the implementer.
