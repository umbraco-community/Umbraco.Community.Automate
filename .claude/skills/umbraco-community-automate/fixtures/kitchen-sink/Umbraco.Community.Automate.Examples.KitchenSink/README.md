# Kitchen Sink example

> **For reference only.** This example shows the skill, and anyone building by hand, two conventions none of the repo's real packages use yet. It isn't meant to be installed in a real site, and it's never published.

A reference connection for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate) that shows a trigger and outcomes: the two parts of the framework this repo's other packages don't exercise. For the common case of an API key and one action, look at [Pushover](../../../Pushover/Umbraco.Community.Automate.Pushover/README.md) or [Skoda](../../../Skoda/Umbraco.Community.Automate.Skoda/README.md) instead — they're real, maintained packages and the better "what does idiomatic look like today" reference.

It talks to [httpbin.org](https://httpbin.org), a free service that echoes requests back and accepts any API token, so its tests need no credentials. It isn't published to NuGet, and isn't referenced by the Demo site: CI builds and tests it on every pull request so it can't go stale, but it has no release tag prefix and isn't packable.

## What it adds

| | Name | Shows |
|---|---|---|
| Connection | **Kitchen Sink Example (httpbin.org)** | An API key pre-filled with a configuration reference; **Test connection** making one cheap authenticated call |
| Action | **Send Example Message** | A typed output for later steps, an idempotency key, and error categories |
| Action | **Check Example Status** | Outcomes (`available`, `notFound`) that later steps branch on, instead of failing |
| Trigger | **Dictionary Item Saved (Kitchen Sink Example)** | A notification trigger with a per-automation filter |

## Folder by folder

| Folder | What's in it |
|---|---|
| `Actions/` | Each action with its settings and output class. [`SendMessageAction`](Actions/SendMessageAction.cs) is the pattern for "create something" actions; [`CheckStatusAction`](Actions/CheckStatusAction.cs) for "look something up". |
| `Triggers/` | [`DictionaryItemSavedTrigger`](Triggers/DictionaryItemSavedTrigger.cs): `MapEvent` turns an Umbraco notification into trigger events, `CanHandle` filters them per automation. Automate subscribes to the notification for you. |
| `Connections/` | The connection type, its settings (the API key defaults to `$Umbraco:Automate:Secrets:KitchenSink:ApiKey`), and a validator shared with the actions. |
| `Composers/` | One composer registering the API client. Automate discovers the connection type, actions and trigger from their attributes, so they aren't registered. |
| `Api/` | [`KitchenSinkClient`](Api/KitchenSinkClient.cs), the only class that talks HTTP: it sends requests and maps every failure to a `StepRunErrorCategory`, which decides whether Automate retries. |
| `Models/` | Request and response models for the service. |
| `Configuration/` | `KitchenSinkConfiguration`: the one place the configuration path, default reference and the service's base URL are defined. |
| `wwwroot/` | The icon: `umbraco-package.json` and `icons/`, the same hand-written files every package in this repo uses. |

The tests in `Umbraco.Community.Automate.Examples.KitchenSink.Tests/` use the same folders, xUnit only, with hand-written HTTP fakes and Umbraco's `ActionTestHarness`.

## Run the tests

This fixture isn't referenced by the Demo site (it's a reference for the skill, not a package to
browse in the backoffice), so there's no Demo setup to try it in — build and run its own tests instead:

```bash
dotnet test .claude/skills/umbraco-community-automate/fixtures/kitchen-sink/Umbraco.Community.Automate.Examples.KitchenSink.Tests
```
