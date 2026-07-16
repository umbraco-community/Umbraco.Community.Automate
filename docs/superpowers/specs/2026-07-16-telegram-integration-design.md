# Telegram Messenger Integration

## Context

Umbraco.Automate currently has no Telegram support. Telegram should serve three roles, mirroring patterns already proven by the Slack and GoogleSheets community packages and the built-in Email/Webhook notification channels:

1. An **action** a workflow can use to send a Telegram message as a step.
2. A **failure-notification channel** (like the existing Email/Webhook channels) so a workflow can notify a Telegram chat automatically when a run fails/completes/etc.
3. A **trigger** that starts a workflow when a Telegram bot command is received.

This is designed as one cohesive spec/package but built in two phases:

- **Phase 1** delivers the send action + notification channel (both outbound, lower risk, immediately useful for "notify me on failure").
- **Phase 2** adds the inbound command trigger.

All three share one connection (bot token + chat ID), so designing them together avoids rework later.

The package follows the existing `Umbraco.Community.Automate.GoogleSheets` package structure exactly (this repo, not the core Umbraco.Automate monorepo), since Telegram is a community-maintained provider, not an official first-party one.

## Architecture recap (from codebase research)

- Everything (actions, triggers, connections, notification channels) is attribute-driven and auto-discovered by Umbraco's type-finder — no manual DI registration needed. See `Configuration/UmbracoBuilderExtensions.Collections.cs` in `Umbraco.Automate.Core`.
- `IAction : IStepType` (`Actions/IAction.cs`, `ActionBase<TSettings,TOutput>` in `Actions/ActionBase.cs`), marker `[Action(...)]`.
- `ITrigger : IStepType` (`Triggers/ITrigger.cs`), with `WebhookTriggerBase<TSettings,TOutput> : TriggerBase<...>, IWebhookTrigger` (`Triggers/WebhookTriggerBase.cs`) giving each automation a unique inbound webhook URL — this is what the Phase 2 trigger builds on.
- `INotificationChannel` channels are **not** workflow actions — they're configured per-automation (`Automation.NotificationSettings.Channels`, `NotifyOn` flags) and dispatched automatically by `Notifications/Channels/ChannelNotifier.cs` when a run finishes (`Runs/RunCompletedNotificationDispatcher.cs`). `EmailNotificationChannel.cs` and `WebhookNotificationChannel.cs` (`Notifications/Channels/BuiltIn/`) are the templates to follow.
- Connections use plain `ConnectionTypeBase<TSettings>` (`Connections/ConnectionTypeBase.cs`) unless OAuth is needed (Slack/GoogleSheets use the separate `Umbraco.Automate.OpenIddict` package for that) — Telegram does not need OAuth, it authenticates with a static bot token, so it uses the plain base with an `[Field(IsSensitive = true)]` token field, following the exact pattern of `WebhookNotificationChannelSettings.Secret`.
- All outbound HTTP goes through the shared named `HttpClient("UmbracoAutomate")` (`IHttpClientFactory.CreateClient("UmbracoAutomate")`), which gets SSRF protection for free via `Security/SsrfProtectionHandler.cs`. Every existing action/channel uses this client rather than creating its own — Telegram does the same.
- `StepRunErrorCategory` (`Umbraco.Automate.Core/Actions/ActionResult.cs`) already has a `RateLimiting` value to use for Telegram's 429 responses.
- Inbound webhook authenticity is handled via the existing `IWebhookAuthenticator` extensibility point (`PlainSecretWebhookAuthenticator` etc.) — Telegram's `setWebhook` supports a secret token header (`X-Telegram-Bot-Api-Secret-Token`) that maps onto this directly.

## Decisions made during brainstorming

| Question | Decision |
|---|---|
| Scope | Action + trigger + notification channel, all three, one spec, phased build |
| Repo | Community repo (`Umbraco.Community.Automate.Telegram`), not core |
| Chat targeting | Chat ID stored per connection (one bot = one default destination) |
| Trigger scope (Phase 2) | Bot commands only (e.g. `/deploy`), not arbitrary messages |
| Update delivery (Phase 2) | Webhook push via Telegram's `setWebhook`, reusing `IWebhookTrigger` infra |
| Message format | MarkdownV2 |
| Attachment scope | Text only (`sendMessage`) — no photos/documents in v1 |
| Rate limiting | Simple local retry: one retry after `retry_after` seconds, then fail with `StepRunErrorCategory.RateLimiting` |

## Package layout

New top-level folder in this repo (`Umbraco.Community.Automate`), mirroring `GoogleSheets/`:

```
Telegram/
├── Umbraco.Community.Automate.Telegram/
│   ├── Connection/
│   │   ├── TelegramConnectionType.cs        [ConnectionType("telegram", "Telegram", ...)]
│   │   └── TelegramConnectionSettings.cs    BotToken ([Field(IsSensitive=true)]), ChatId
│   ├── Actions/
│   │   ├── SendMessageAction.cs             [Action("telegram.sendMessage", ..., ConnectionTypeAlias="telegram")]
│   │   ├── SendMessageSettings.cs           Text ([Field(SupportsBindings=true)]), optional ChatId override
│   │   └── SendMessageOutput.cs             MessageId, SentAt
│   ├── Notifications/
│   │   ├── TelegramNotificationChannel.cs   [NotificationChannel("telegram", "Telegram", ...)]
│   │   └── TelegramNotificationChannelSettings.cs
│   ├── Triggers/                             (Phase 2)
│   │   ├── TelegramCommandTrigger.cs         [Trigger("telegram.command", ...)] : WebhookTriggerBase<...>
│   │   ├── TelegramCommandTriggerSettings.cs Command (e.g. "/deploy")
│   │   └── TelegramUpdateOutput.cs           Command, Args, ChatId, FromUsername
│   ├── Client/
│   │   ├── TelegramApiClient.cs             thin wrapper: SendMessageAsync, GetMeAsync, SetWebhookAsync
│   │   └── TelegramMarkdownEscaper.cs       shared MarkdownV2 escaping, used by action + notification channel
│   ├── appsettings-schema.Telegram.json
│   ├── Directory.Build.props                MinVerTagPrefix="telegram-v"
│   ├── README.md
│   └── Umbraco.Community.Automate.Telegram.csproj
└── Umbraco.Community.Automate.Telegram.Tests/
    ├── Actions/SendMessageActionTests.cs
    ├── Notifications/TelegramNotificationChannelTests.cs
    └── Triggers/TelegramCommandTriggerTests.cs   (Phase 2)
```

Reference the `dotnet new umbraco-automate-actions` template (`/Users/warren/Code/Umbraco.Automate/templates/umbraco-automate-actions/`) to scaffold the initial project/test project shape if it saves time, then adapt to match GoogleSheets' actual structure (Connection/, Notifications/, Client/ folders it doesn't generate).

## Component details

### `TelegramConnectionType`

Plain `ConnectionTypeBase<TelegramConnectionSettings>`. Settings:

- `BotToken` — `[Field(IsSensitive = true)]`, the token issued by @BotFather.
- `ChatId` — the single default destination chat/group/channel (one bot connection = one destination).

`ValidateAsync` calls Telegram's `getMe` endpoint to confirm the token is valid and reachable.

### Phase 1a: `SendMessageAction`

`ActionBase<SendMessageSettings, SendMessageOutput>`, alias `telegram.sendMessage`, `ConnectionTypeAlias = "telegram"`.

- `Text` field: `SupportsBindings = true`, sent as MarkdownV2 (escaped via `TelegramMarkdownEscaper`).
- Optional per-action `ChatId` override of the connection's default.
- Uses `TelegramApiClient.SendMessageAsync`, which resolves the `"UmbracoAutomate"` named `HttpClient`.
- On HTTP 429, retries once after the API's `retry_after` seconds, then fails with `ActionResult.Failed(ex, StepRunErrorCategory.RateLimiting)` if still limited.
- Other Telegram API errors map to `StepRunErrorCategory.Validation` (bad settings, e.g. malformed chat ID) or `InvalidResponse` (unexpected API error — chat not found, bot blocked/kicked, bad MarkdownV2 escaping).

### Phase 1b: `TelegramNotificationChannel`

`NotificationChannelBase<TelegramNotificationChannelSettings>`, alias `telegram`. Plugs directly into the existing `ChannelNotifier`/`NotifyOn` pipeline — no new dispatch mechanism needed.

- Resolves the automation's `TelegramConnectionType` (bot token + chat ID) the same way `SendMessageAction` does.
- Message body: run status, automation name, failure reason/link — formatted as MarkdownV2, reusing `TelegramMarkdownEscaper`.
- Gets the same try/catch isolation `ChannelNotifier` already provides every channel (a Telegram outage won't block Email from firing).

### Phase 2: `TelegramCommandTrigger`

`WebhookTriggerBase<TelegramCommandTriggerSettings, TelegramUpdateOutput>` — reuses the existing per-automation unique webhook URL infrastructure as-is.

- Setup requires calling Telegram's `setWebhook` with the automation's webhook URL and a secret token, validated inbound via the existing `PlainSecretWebhookAuthenticator` (`X-Telegram-Bot-Api-Secret-Token` header).
- `CanHandle(output, settings)` parses the incoming update's message text and matches against the configured `Command` (e.g. `/deploy`) — only bot commands fire the trigger, not arbitrary messages.
- Output exposes `Command`, `Args` (rest of the message text), `ChatId`, `FromUsername` for downstream steps.

## Error handling summary

| Scenario | `StepRunErrorCategory` |
|---|---|
| Missing/invalid settings (bad chat ID format, empty text) | `Validation` |
| Invalid or revoked bot token | `Authentication` |
| HTTP 429 after one retry | `RateLimiting` |
| Chat not found, bot blocked/kicked, malformed MarkdownV2 | `InvalidResponse` |
| Telegram API unreachable/5xx | `ServiceUnavailable` |

## Testing

Mirror GoogleSheets exactly: xUnit + Moq (`Moq.Protected` stubbing `HttpMessageHandler` for Telegram API responses) + Shouldly, using `Umbraco.Automate.Testing` builders (`AutomationBuilder`, `ConnectionBuilder`, `StepConfigurationBuilder`, `AutomationRunBuilder`). One test class per action/channel/trigger, named `<Verb>_<expected behavior>`. Manually exercised end-to-end in `Umbraco.Community.Automate.Demo` alongside the other packages.

## Verification

- `dotnet build` and `dotnet test` on the new `Umbraco.Community.Automate.Telegram(.Tests)` projects.
- Manually verify in the Demo site: configure a real Telegram bot token + chat ID, run `SendMessageAction` in a test workflow, confirm the message arrives in the target chat with MarkdownV2 formatting rendered correctly.
- Manually trigger a failed automation run and confirm the `TelegramNotificationChannel` fires per its `NotifyOn` config.
- (Phase 2) Register a webhook against a test bot, send it a `/command` message from Telegram, confirm the automation run starts with the correct `Command`/`Args`/`ChatId` output.

## Out of scope (v1)

- Photo/document sending (`sendPhoto`/`sendDocument`).
- Multiple destination chats per connection (per-action override exists, but no chat-list/broadcast feature).
- Polling-based update delivery (`getUpdates`) — webhook push only.
- Shared cross-package retry/backoff helper — Telegram's retry logic is self-contained in this package for now.
