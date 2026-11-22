# Umbraco.Community.Automate.Pushover

A [Pushover](https://pushover.net/) connection and action for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate): send push notifications to phones and desktops as part of an automation.

Pushover is a simple notification service that delivers real-time alerts to iOS, Android and desktop devices. Use this package for instant operational alerts, for example:

- **New orders**: play a "kerching" sound on your phone when an Umbraco Commerce order is placed.
- **Moderation**: a distinct alert when content is submitted for approval.
- **Team notifications**: route different events to different users or groups, with different sounds and priorities.

## Installation

```bash
dotnet add package Umbraco.Community.Automate.Pushover
```

There's nothing to register in code: Umbraco loads the package on its own. Then follow **Setup** below.

## Setup

### 1. Get an API token and a user or group key

1. In your Pushover account, go to **Your Applications → Create an Application/API Token**, and copy the **API token**.
2. Your **user key** is shown on your Pushover dashboard. To notify a group instead, create a delivery group and use its **group key**.

### 2. Store them in configuration

Add both under `Umbraco:Automate:Secrets:Pushover`, in Umbraco Automate's shared Secrets section (locally, use [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)):

```json
{
  "Umbraco": {
    "Automate": {
      "Secrets": {
        "Pushover": {
          "ApiToken": "your-api-token",
          "UserKey": "your-user-or-group-key"
        }
      }
    }
  }
}
```

In production, use environment variables instead: `Umbraco__Automate__Secrets__Pushover__ApiToken` and `Umbraco__Automate__Secrets__Pushover__UserKey`. Umbraco Automate resolves references from this shared section out of the box, so there's nothing to register.

### 3. Create the connection

1. Go to **Automation → Settings → Connections** and create a new **Pushover** connection.
2. **API token** and **User or group key** are already filled in with `$Umbraco:Automate:Secrets:Pushover:ApiToken` and `$Umbraco:Automate:Secrets:Pushover:UserKey`, references to the values you stored in step 2. Leave them, or replace them with the values themselves to store them on the connection.
3. Under **Advanced**, **Retry** and **Expire** apply only to **Max** priority notifications, which Pushover repeats every *Retry* seconds (at least 30) until someone acknowledges them or *Expire* seconds (at most 10,800, 3 hours) have passed. They default to 60 and 1,800.
4. Click **Test connection**. Pushover checks the token and key without sending anything, and lists the user's devices.

Create more connections, with different tokens or keys, to send to different Pushover applications, users or groups.

## Actions

### Send Pushover Notification

| Setting | Description |
|---|---|
| **Title** | Optional. Shown above the message. Supports `${ binding }` expressions. |
| **Message** | Required. The notification's text. Supports `${ binding }` expressions. |
| **Sound** | The sound it plays, chosen from [Pushover's sounds](https://pushover.net/api#sounds). Defaults to `pushover`. |
| **Custom sound** | Optional. The name of a sound uploaded to your Pushover account, used instead of **Sound**. Supports `${ binding }` expressions. |
| **URL** | Optional. A link people can open from the notification: a web address, or `mailto:`, `tel:` and other URI schemes. Supports `${ binding }` expressions. |
| **URL title** | Optional. The link's text; without it, the URL itself is shown. Supports `${ binding }` expressions. |
| **Priority** | `Min`, `Low`, `Default`, `High` or `Max`. **Max** repeats the notification until it's acknowledged, using the connection's **Retry** and **Expire**. Defaults to `Default`. |

## Outputs

**Send Pushover Notification** makes these available to later steps as `${ steps.<alias>.<output> }`:

| Output | Description |
|---|---|
| `status` | `1` when Pushover accepted the notification |
| `request` | Pushover's ID for the request, useful when contacting its support |

## Troubleshooting

| Message | What to do |
|---|---|
| *Configuration key '...' not found* | The token or key isn't in configuration under the path shown. Add it (see [Setup](#2-store-them-in-configuration)), or enter the value on the connection. |
| *Pushover rejected the request: application token is invalid* | Check the API token: it's the application's token, not your user key. |
| *Pushover rejected the request: ...* naming the user or key | Check the user or group key on your Pushover dashboard. |
| *A message is required.* | The **Message** setting is empty, or a binding in it resolved to nothing. |
| A rate-limit or *Could not reach Pushover* failure | Temporary: Automate retries the step. Pushover limits how many messages an application can send each month. |

## Migrating from SA.Automate.Pushover

This package was previously published as `SA.Automate.Pushover` and now ships from the [Umbraco.Community.Automate](https://github.com/umbraco-community/Umbraco.Community.Automate) repo:

```bash
dotnet remove package SA.Automate.Pushover
dotnet add package Umbraco.Community.Automate.Pushover
```

Then:

- **Move your configuration.** The token and key now live under `Umbraco:Automate:Secrets:Pushover` as `ApiToken` and `UserKey` (the old package read `Umbraco:Automate:Secrets:PushoverApiToken` and `PushoverUserKey`). Only new connections are pre-filled with the new references; a connection you recreate picks them up.
- **Recreate connections and steps.** The connection type is now `community.pushover` and the action `community.pushover.sendNotification` (previously `pushover` and `pushover.SendNotification`), so automations built with the old package need their Pushover connection and step added again. The settings and outputs are the same, so bindings such as `${ steps.<alias>.request }` keep working.

## Compatibility

| Package version | Umbraco Automate | Umbraco CMS |
|---|---|---|
| 1.x | 17.x – 18.x | 17.4 – 18.x |

One build supports both Umbraco 17 and 18: it's compiled against 17, and every change is tested on both, including running the 17 build on 18. Umbraco 19 isn't supported until it has been tested.

## Links

- [Source code](https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Packages/Pushover/Umbraco.Community.Automate.Pushover)
- [Report an issue](https://github.com/umbraco-community/Umbraco.Community.Automate/issues)
- [Pushover API documentation](https://pushover.net/api)
