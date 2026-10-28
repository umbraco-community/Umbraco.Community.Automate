# Test Connection

> **For development only.** The Demo site uses this to test connections. It isn't meant to be installed in a real site, and it's never published.

One action, **Test Connection** (`community.testConnection`), that runs a connection's own **Test connection** check from an automation, the same check as the button under **Automation → Settings → Connections**. It's a helper: use it to check you've put the right credentials in, and that automations using the connection will actually work, before you build them.

| Setting | What it is |
|---|---|
| **Connection** | The alias of the connection to test, e.g. `pushover`. |

| Output | What it is |
|---|---|
| `connected` | `true` when the check passed (a warning still counts), `false` when it failed or there's no connection with that alias. |
| `message` | What the check reported, e.g. "Connected to httpbin.org." or why it failed. |

The step always succeeds, because a failed check is the answer you asked for. Branch on it with an **If** step: `${ steps.testConnection.connected }` equals `true`.

## In the Demo site

Each package gets a *Test: …* automation on a manual trigger, so you can check it from the Automate workspace with the **Run now** entity action — no content to publish. Run it, then open the automation's run history to see whether it connected.

## Run the tests

```bash
dotnet test Packages/_Development/TestConnection/Umbraco.Community.Automate.TestConnection.Tests
```
