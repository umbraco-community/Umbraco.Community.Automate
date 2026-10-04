# Kitchen Sink example

> **For reference only.** This example shows the skill, and anyone building by hand, what a finished package looks like. It isn't meant to be installed in a real site, and it's never published.

A reference connection for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate) that shows every convention in this repo in one small, working package: a trigger, outcomes, an API client and a backoffice front end. The skill takes the parts a new package needs from here. For the common case of an API key and one action, start from the [Simple example](../../Simple/Umbraco.Community.Automate.Examples.Simple/README.md) instead.

It talks to [httpbin.org](https://httpbin.org), a free service that echoes requests back and accepts any API token, so everything works in the Demo site without signing up for anything. It isn't published to NuGet: CI builds and tests it on every pull request so it can't go stale, but it has no release tag prefix and isn't packable.

## What it adds

| | Name | Shows |
|---|---|---|
| Connection | **Kitchen Sink Example (httpbin.org)** | An API key pre-filled with a configuration reference; **Test connection** making one cheap authenticated call |
| Action | **Send Example Message** | A typed output for later steps, an idempotency key, error categories, and a custom field editor |
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
| `Client/` | The backoffice front end (Vite + Lit): a message editor with a character count, with a unit test. `public/` holds the hand-written `umbraco-package.json` and `icons/`, the same files every package uses for its icons. `npm run build` writes all of it to `wwwroot/`. |
| `wwwroot/` | Build output from `Client/` (not committed), served at `/App_Plugins/UmbracoCommunityAutomateExamplesKitchenSink/`. |

The tests in `Umbraco.Community.Automate.Examples.KitchenSink.Tests/` use the same folders, xUnit only, with hand-written HTTP fakes and Umbraco's `ActionTestHarness`.

## Try it in the Demo site

The Demo site references this package and already has a placeholder key at `Umbraco:Automate:Secrets:KitchenSink:ApiKey`.

```bash
cd Packages/_Development/KitchenSink/Umbraco.Community.Automate.Examples.KitchenSink/Client
npm ci && npm run build
cd ../../../../..
dotnet run --project Umbraco.Community.Automate.Demo
```

1. In **Automation → Settings → Connections**, create a **Kitchen Sink Example (httpbin.org)** connection. The API key is already filled in; click **Test connection**.
2. Create an automation with the **Dictionary Item Saved (Kitchen Sink Example)** trigger (optionally set **Key starts with**) and a **Send Example Message** step, with a message such as `Saved ${ trigger.key }`.
3. Publish it, save a dictionary item under **Translation**, and check **Automation → Runs**.

## Run the tests

```bash
dotnet test Packages/_Development/KitchenSink/Umbraco.Community.Automate.Examples.KitchenSink.Tests

cd Packages/_Development/KitchenSink/Umbraco.Community.Automate.Examples.KitchenSink/Client
npm ci && npm test
```
