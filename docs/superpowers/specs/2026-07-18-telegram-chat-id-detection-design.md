# Telegram Chat ID Detection

## Context

Phase 1 of the Telegram integration (connection, send action, notification channel — see `docs/superpowers/specs/2026-07-16-telegram-integration-design.md`) shipped with a plain "Chat ID" text field the user fills in manually. Finding that ID today means messaging the bot, then hand-crafting a `https://api.telegram.org/bot<token>/getUpdates` URL and reading raw JSON — confirmed during manual testing of Phase 1 to be a genuinely rough, non-obvious step.

Investigating how Zapier and n8n handle this showed they don't solve it any more cleverly than we could — Telegram's Bot API has no "list my chats" endpoint, so everyone relies on the same underlying mechanism (a message must land in the bot's `getUpdates` first). Their actual advantage is UX: they surface that lookup *inside* the product (a "test"-style step showing the incoming JSON) instead of sending the user to a raw browser URL. This spec designs the equivalent for Umbraco.Automate: a "Detect chat ID" affordance on the Chat ID field that calls `getUpdates` on the user's behalf and lets them pick from a list.

This is a separate, self-contained deliverable from Phase 1 (already shipped) and the future Phase 2 trigger (inbound bot commands) — it touches new subsystems (a package-owned backend API endpoint, a package-owned backoffice frontend project) that neither of those does, so it gets its own spec → plan → implementation cycle.

## Decisions made during brainstorming

| Question | Decision |
|---|---|
| Save-first vs stateless | **Stateless** — the new endpoint accepts the bot token directly from the live, unsaved form value. One flow: paste token → Detect → pick chat → save once. Does not reuse the existing by-id `Test connection` flow (which only works after save, since sensitive fields are masked on refetch). |
| UI shape | A field-level `propertyEditorUi` (input + "Detect" button), mirroring the existing `ua-webhook-secret-field` shape — not a whole custom editor, not a workspace-level action button. |
| Cross-field value access | Confirmed feasible with no core changes: the settings form already uses Umbraco's standard `<umb-property-dataset>`/`UMB_PROPERTY_DATASET_CONTEXT` mechanism. A custom Chat ID editor can consume that context and call `propertyValueByAlias('botToken')` to reactively read the sibling Bot Token field's live value. |
| Multiple chats detected | Dedupe by `chat.id` (in case more than one person has messaged the bot), show the most recent name/username/type for each, let the user pick. |
| Empty / conflict states | Distinct friendly messages: no messages yet ("send your bot a message first"), and a 409 conflict once Phase 2 exists (webhook active on this bot — detection needs `getUpdates`, which Telegram blocks while a webhook is registered). |

## Architecture

### Backend

- **`TelegramApiClient.GetUpdatesAsync(IHttpClientFactory, string botToken, CancellationToken)`** — new method alongside the existing `SendMessageAsync`/`GetMeAsync` (`Telegram/Umbraco.Community.Automate.Telegram/Client/TelegramApiClient.cs`). Calls `https://api.telegram.org/bot{token}/getUpdates`, deserializes Telegram's `Update[]` shape (already have the `JsonNamingPolicy.SnakeCaseLower` options in place), and returns a de-duplicated list of detected chats (`ChatId`, `Type`, `DisplayName`) — most recent update per chat wins on dedupe.
- **New controller**, own route/Swagger group (Telegram only depends on `Umbraco.Automate.Core`, so it cannot reuse `Umbraco.Automate.Web`'s `UmbracoAutomateManagementControllerBase`/`AutomateAuthorizationPolicies` — those live in a project this package doesn't reference). `[Authorize]` against a standard Umbraco backoffice authorization policy (not Automate-specific), requiring a logged-in backoffice user. `POST` endpoint, request body `{ "botToken": "..." }`, response is either a list of detected chats or a distinguishable error: `NoMessagesYet` (empty `getUpdates` result) vs `WebhookConflict` (Telegram's 409 when a webhook is already registered for this bot) vs a generic failure.
- The bot token in the request body is transient — read from the live form, sent once, used to call Telegram, never persisted by this endpoint (the connection itself is what persists it, on save, through the existing sensitive-field encryption path).

### Frontend

New `propertyEditorUi`, e.g. `UmbracoCommunityAutomateTelegram.PropertyEditorUi.ChatIdField`:

- Input + "Detect" button, same layout shape as `ua-webhook-secret-field` (`Umbraco.Automate/.../Client/src/core/components/webhook-secret-field/webhook-secret-field.element.ts`).
- On connect, consumes `UMB_PROPERTY_DATASET_CONTEXT` and calls `propertyValueByAlias('botToken')` to reactively track the sibling Bot Token field's live value (exact alias to be confirmed against the actual `EditableModelSchema` key casing during implementation — expected to be camelCase `botToken`, matching the `TelegramConnectionSettings.BotToken` property).
- Clicking Detect: disables itself if the bot token field is empty (nothing to detect against), otherwise POSTs the current token to the new endpoint and renders one of:
  - A small picker (list) of detected chats (name/username, type, id) — selecting one sets `this.value` and dispatches `UmbChangeEvent()`.
  - An empty-state message: "No messages found yet — send your bot a message on Telegram, then try again."
  - A conflict-state message (once Phase 2 exists): "Can't detect right now — this bot has an active automation trigger. Enter the ID manually or use `@channelusername` for public channels."
  - A generic error message for anything else (bad token, network failure).
- Referenced via `[Field(EditorUiAlias = "UmbracoCommunityAutomateTelegram.PropertyEditorUi.ChatIdField")]` from `TelegramConnectionSettings.ChatId`. `SendMessageSettings.ChatId` (per-step override) and `TelegramNotificationChannelSettings.ChatId` are separate design calls — they don't have a sibling Bot Token field to read from in the same shape (the notification channel does have its own `BotToken` field, so it could reuse the same editor; the action's override field has no bot token nearby at all, so it stays a plain text field). This spec's scope is the **connection's** Chat ID field only; extending the same editor to the notification channel's Chat ID field is a natural but separate follow-up once this lands.

### New `Client/` project

A new Vite/TypeScript project under `Telegram/Umbraco.Community.Automate.Telegram/Client/`, structured like GoogleSheets' existing `Client/` folder:

```
Client/
├── src/
│   ├── manifests.ts                    (aggregates chat-id-field manifests)
│   └── chat-id-field/
│       ├── manifests.ts                (propertyEditorUi registration)
│       └── chat-id-field.element.ts
├── public/umbraco-package.json         (bundle registration)
├── package.json, vite.config.ts, web-test-runner.config.mjs
└── (Playwright e2e, matching GoogleSheets' Client/tests structure)
```

`.csproj` changes mirror GoogleSheets exactly: `Sdk="Microsoft.NET.Sdk.Razor"`, `AddRazorSupportForMvc`, `StaticWebAssetBasePath`, `<Content Remove="Client\**" />` + `<None Include="Client\public\umbraco-package.json" Pack="false" />`.

## Testing

- **Backend:** `TelegramApiClient.GetUpdatesAsync` tests following the existing `StubHandler`/`Moq.Protected` pattern (`Telegram/Umbraco.Community.Automate.Telegram.Tests/Client/`). The new controller is tested by instantiating it directly and calling its action method (no existing precedent in this codebase for a community package spinning up a full `WebApplicationFactory`/`TestServer`, so direct unit tests match how actions/channels are already tested here).
- **Frontend:** component tests via `web-test-runner`, matching GoogleSheets' `column-list` test setup — cover the picker rendering, empty state, and conflict state.
- **Manual:** exercise in `Umbraco.Community.Automate.Demo` with a real bot — paste a fresh token with no prior messages (confirm empty state), send the bot a message and detect again (confirm picker), select a chat (confirm it fills the field).

## Out of scope

- Extending the same Chat ID editor to `TelegramNotificationChannelSettings`/`SendMessageSettings` — natural follow-up, not required for this spec.
- Any change to the existing by-id `Test connection` button/flow.
- Handling the Phase 2 webhook-conflict case beyond a friendly error message — actually resolving the conflict (e.g. offering to temporarily unregister the webhook) is out of scope.
