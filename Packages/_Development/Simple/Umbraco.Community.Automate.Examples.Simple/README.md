# Simple example

> **For reference only.** This example shows the skill, and anyone building by hand, what a finished package looks like. It isn't meant to be installed in a real site, and it's never published.

The smallest useful connection for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate): an API key and one action. Most connections start like this, so it's the reference the skill builds new packages from.

It talks to [httpbin.org](https://httpbin.org), a free service that echoes requests back and accepts any API token, so it works in the Demo site without signing up for anything. It isn't published to NuGet: CI builds and tests it on every pull request so it can't go stale, but it has no release tag prefix and isn't packable.

When you need more (a trigger, outcomes, a shared API client, custom icons or a backoffice editor), see the [Kitchen Sink example](../../KitchenSink/Umbraco.Community.Automate.Examples.KitchenSink/README.md).

## What it adds

| | Name | Shows |
|---|---|---|
| Connection | **Simple Example (httpbin.org)** | An API key pre-filled with a configuration reference; **Test connection** making one cheap authenticated call |
| Action | **Send Message (Simple Example)** | A setting that accepts `${ bindings }`, a typed output (the message as the service received it, the URL it went to, and when it arrived, which httpbin.org echoes back), and error categories that tell Automate whether to retry |

It uses Umbraco's built-in `icon-paper-plane`, so there are no front-end files.

## The files

| File | What it does |
|---|---|
| [`Configuration/SimpleConfiguration.cs`](Configuration/SimpleConfiguration.cs) | Where the API key lives in configuration, and the service's base URL. |
| [`Connections/SimpleConnectionSettings.cs`](Connections/SimpleConnectionSettings.cs) | The connection's one field, the API key, which defaults to `$Umbraco:Automate:Secrets:Simple:ApiKey`. |
| [`Connections/SimpleConnectionType.cs`](Connections/SimpleConnectionType.cs) | The connection type. `ValidateAsync` runs when the user clicks **Test connection**. |
| [`Actions/SendMessageAction.cs`](Actions/SendMessageAction.cs) | The action: checks its inputs, calls the service, and returns an output or a categorised failure. |
| [`Actions/SendMessageSettings.cs`](Actions/SendMessageSettings.cs), [`SendMessageOutput.cs`](Actions/SendMessageOutput.cs) | The action's fields, and what it gives later steps. |
| [`Composers/SimpleComposer.cs`](Composers/SimpleComposer.cs) | Registers the connection type and action. |

The tests in `Umbraco.Community.Automate.Examples.Simple.Tests/` cover the action and the connection with a hand-written HTTP stub and Umbraco's `ActionTestHarness`.

## Try it in the Demo site

The Demo site references this package and already has a placeholder key at `Umbraco:Automate:Secrets:Simple:ApiKey`.

```bash
dotnet run --project Umbraco.Community.Automate.Demo
```

1. In **Automation → Settings → Connections**, create a **Simple Example (httpbin.org)** connection. The API key is already filled in; click **Test connection**.
2. Create an automation with any trigger (for example **Content Published**) and a **Send Message (Simple Example)** step, with a message such as `Published ${ trigger.contentName }`.
3. Publish some content and check **Automation → Runs**: the step's output shows your message as httpbin.org received it, the URL it was posted to, and when it arrived. A later step can use them as `${ steps.<alias>.message }`, `${ steps.<alias>.url }` and `${ steps.<alias>.receivedAt }`.

## Run the tests

```bash
dotnet test Packages/_Development/Simple/Umbraco.Community.Automate.Examples.Simple.Tests
```
