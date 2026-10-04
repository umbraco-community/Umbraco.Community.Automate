# Umbraco.Community.Automate.Mastodon

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.Automate.Mastodon?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.Automate.Mastodon/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.Automate.Mastodon?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.Automate.Mastodon)
[![GitHub license](https://img.shields.io/github/license/umbraco-community/Umbraco.Community.Automate?color=8AB803)](https://github.com/umbraco-community/Umbraco.Community.Automate/blob/main/LICENSE)

A Mastodon connection type and action for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate).

Post statuses to any Mastodon instance as part of an automation workflow — for example, automatically tooting when a blog post is published.

## Installation

```bash
dotnet add package Umbraco.Community.Automate.Mastodon
```

There's nothing to register in code: Umbraco loads the package on its own. Then follow **Setup** below.

## Setup

### 1. Generate a Mastodon access token

In your Mastodon account go to **Preferences → Development → New application** and create an application with the `write:statuses` scope. Copy the access token.

### 2. Add your settings to configuration (recommended)

New connections are pre-filled with references to configuration, so if you store the instance URL and access token there, you don't need to type anything into the backoffice. Values go under a `Mastodon` key in Umbraco Automate's shared **Variables** (non-sensitive) and **Secrets** (sensitive) sections. Umbraco Automate resolves `$` references from its shared `Umbraco:Automate:Variables` and `Umbraco:Automate:Secrets` sections out of the box, so there's nothing to register. `Secrets` can only be referenced from sensitive fields such as **Access Token**.

Add the following to your `appsettings.json`:

```json
{
  "Umbraco": {
    "Automate": {
      "Variables": {
        "Mastodon": {
          "InstanceUrl": "https://umbracocommunity.social"
        }
      },
      "Secrets": {
        "Mastodon": {
          "AccessToken": "your-access-token-here"
        }
      }
    }
  }
}
```

For production, use environment variables instead of a config file:

```
Umbraco__Automate__Variables__Mastodon__InstanceUrl=https://umbracocommunity.social
Umbraco__Automate__Secrets__Mastodon__AccessToken=your-access-token-here
```

`InstanceUrl` and `AccessToken` are the names new connections reference by default. For several accounts, add more keys (e.g. `AccessTokenNews`) and point each connection's field at its own key.

### 3. Create the connection in the backoffice

1. Go to **Automation → Settings → Connections** and create a new **Mastodon** connection (in the **Social Networks** group).
2. **Instance URL** and **Access Token** are already filled in with `$Umbraco:Automate:Variables:Mastodon:InstanceUrl` and `$Umbraco:Automate:Secrets:Mastodon:AccessToken`, references to the values you stored in step 2. Leave them as they are, or replace either with the value itself (e.g. `https://mastodon.social`) to store it on the connection.
3. Click **Test connection** to verify. If a value is missing from configuration, Umbraco Automate reports which key, e.g. *Configuration key 'Umbraco:Automate:Secrets:Mastodon:AccessToken' not found*.

## Actions

Add the **Send Mastodon Post** action to any automation and select your Mastodon connection. Its settings:

| Setting | Description |
|---|---|
| Content | The post text. Supports `${ binding }` expressions. |
| Visibility | `public`, `unlisted`, `private`, or `direct`. Defaults to `public`. |
| Post URL | Optional URL appended to the post on a new line. |
| Sensitive | Marks the post as sensitive/NSFW. |
| Spoiler Text | Content warning shown before the post body. |

> **Note:** Most Mastodon instances limit posts to 500 characters (links count as 23 characters, and spoiler text counts toward the limit). Posts over the instance's limit are rejected by the Mastodon API and the action fails.

## Troubleshooting

**"Could not reach {InstanceUrl}"** — check the instance URL is absolute and has no trailing
slash: `https://mastodon.social`, not `mastodon.social` or `https://mastodon.social/`.

**"Authentication failed"** — the token is invalid, expired, or lacks the `write:statuses`
scope. Check the application still exists under **Preferences → Development** on your instance
and regenerate the token if needed.

**"Configuration key '...' not found"** — the field references a key that isn't in configuration.
Add it (see [step 2](#2-add-your-settings-to-configuration-recommended)), or enter the value directly in the field.

**The action fails but the connection tests fine** — most instances cap posts at 500 characters,
counting spoiler text and counting each link as 23. Posts over the cap are rejected by the API.

## Migrating from OC.Automate.Mastodon

This package replaces `OC.Automate.Mastodon` 1.x. The move to 2.x is a **breaking change**: you must update your package reference, configuration and connections:

### Package ID
This package was previously published as `OC.Automate.Mastodon` and now ships from the
[Umbraco.Community.Automate](https://github.com/umbraco-community/Umbraco.Community.Automate) monorepo:

- **Old**: `OC.Automate.Mastodon`
- **New**: `Umbraco.Community.Automate.Mastodon`

```bash
dotnet remove package OC.Automate.Mastodon
dotnet add package Umbraco.Community.Automate.Mastodon
```

### Configuration
- **Old (1.x)**: `OC:Automate:Mastodon:AccessTokens:connectionName` or `Umbraco:Automate:Providers:OCAutomateMastodon:AccessTokens:connectionName`
- **New (2.x)**: a `Mastodon` key under Umbraco Automate's shared `Variables` and `Secrets` sections:
  - `Umbraco:Automate:Variables:Mastodon:InstanceUrl` (instance URL)
  - `Umbraco:Automate:Secrets:Mastodon:AccessToken` (access token)

### Connections
- **Old (1.x)**: Connections required a "Connection Name" field matching an appsettings key
- **New (2.x)**: Connections have **Instance URL** and **Access Token** fields; each accepts either a literal value or a configuration reference (e.g. `$Umbraco:Automate:Secrets:Mastodon:AccessToken`)

### Steps to migrate
1. Move your access token to `Umbraco:Automate:Secrets:Mastodon:AccessToken` (and optionally the instance URL to `Umbraco:Automate:Variables:Mastodon:InstanceUrl`) in `appsettings.json` or environment variables
2. Recreate your Mastodon connections in the backoffice (select the **Mastodon** connection type in the **Social Networks** group); both fields come pre-filled with the references above

## Compatibility

| Package version | Umbraco Automate | Umbraco CMS |
|---|---|---|
| 2.x | 17.x – 18.x | 17.4 – 18.x |
| 1.x | 17.x – 18.x | 17.x – 18.x |

One build supports both Umbraco 17 and 18: it's compiled against 17, and every change is tested on both, including running the 17 build on 18. Umbraco 19 isn't supported until it has been tested.

## Links

- [Source code](https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Packages/Mastodon/Umbraco.Community.Automate.Mastodon)
- [Report an issue](https://github.com/umbraco-community/Umbraco.Community.Automate/issues)
