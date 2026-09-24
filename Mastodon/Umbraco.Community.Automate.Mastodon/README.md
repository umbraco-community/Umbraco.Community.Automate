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

No further setup required. The composer registers itself automatically via Umbraco's `IComposer` discovery.

## Setup

### 1. Generate a Mastodon access token

In your Mastodon account go to **Preferences → Development → New application** and create an application with the `write:statuses` scope. Copy the access token.

### 2. Add your settings to configuration (recommended)

The instance URL and access token are entered on the connection in the backoffice, but instead of typing the values directly you can store them in configuration and reference them. Values go in the package's `Umbraco:Community:Automate:Mastodon` section, split into **Variables** (non-sensitive values) and **Secrets** (sensitive values). The package registers both with Umbraco Automate's configuration allow-list, so no extra setup is needed; `Secrets` can only be referenced from sensitive fields such as **Access Token**.

Add the following to your `appsettings.json`:

```json
{
  "Umbraco": {
    "Community": {
      "Automate": {
        "Mastodon": {
          "Variables": {
            "InstanceUrl": "https://umbracocommunity.social"
          },
          "Secrets": {
            "AccessToken": "your-access-token-here"
          }
        }
      }
    }
  }
}
```

For production, use environment variables instead of a config file:

```
Umbraco__Community__Automate__Mastodon__Variables__InstanceUrl=https://umbracocommunity.social
Umbraco__Community__Automate__Mastodon__Secrets__AccessToken=your-access-token-here
```

The key names (`InstanceUrl`, `AccessToken`) are your choice — add more (e.g. one token per account) as needed — they just need to match the references you enter on the connection.

### 3. Create the connection in the backoffice

1. Go to **Automate → Connections** and create a new **Mastodon** connection (in the **Social Networks** group).
2. **Instance URL** — enter the URL directly (e.g. `https://mastodon.social`), or reference your configuration value: `$Umbraco:Community:Automate:Mastodon:Variables:InstanceUrl`
3. **Access Token** — enter the token directly, or (recommended) reference your configuration value: `$Umbraco:Community:Automate:Mastodon:Secrets:AccessToken`
4. Click **Test connection** to verify.

## Usage

Add the **Send Mastodon Post** action to any automation and select your Mastodon connection. Available fields:

| Field | Description |
|---|---|
| Content | The post text. Supports `${ binding }` expressions. |
| Visibility | `public`, `unlisted`, `private`, or `direct`. Defaults to `public`. |
| Post URL | Optional URL appended to the post on a new line. |
| Sensitive | Marks the post as sensitive/NSFW. |
| Spoiler Text | Content warning shown before the post body. |

> **Note:** Most Mastodon instances limit posts to 500 characters (links count as 23 characters, and spoiler text counts toward the limit). Posts over the instance's limit are rejected by the Mastodon API and the action fails.

## Migration from 1.x to 2.x

Version 2.x is a **breaking change**. If you're upgrading from 1.x, you must update your configuration:

### Package ID
This package was previously published as `OC.Automate.Mastodon` and now ships from the
[Umbraco.Community.Automate](https://github.com/umbraco-community/Umbraco.Community.Automate) monorepo:

- **Old**: `OC.Automate.Mastodon`
- **New**: `Umbraco.Community.Automate.Mastodon`

```bash
dotnet remove package OC.Automate.Mastodon
dotnet add package Umbraco.Community.Automate.Mastodon
```

The connection type alias (`mastodon`) and action alias (`mastodonSendPost`) are unchanged, so
existing automations keep working across the rename.

### Configuration Structure
- **Old (1.x)**: `OC:Automate:Mastodon:AccessTokens:connectionName` or `Umbraco:Automate:Providers:OCAutomateMastodon:AccessTokens:connectionName`
- **New (2.x)**: the package's `Umbraco:Community:Automate:Mastodon` section, split into `Variables` and `Secrets`:
  - `Umbraco:Community:Automate:Mastodon:Variables:InstanceUrl` (instance URL)
  - `Umbraco:Community:Automate:Mastodon:Secrets:AccessToken` (access token)

### Connection Setup
- **Old (1.x)**: Connections required a "Connection Name" field matching an appsettings key
- **New (2.x)**: Connections have **Instance URL** and **Access Token** fields; each accepts either a literal value or a configuration reference (e.g. `$Umbraco:Community:Automate:Mastodon:Secrets:AccessToken`)

### Steps to migrate:
1. Move your access token to `Umbraco:Community:Automate:Mastodon:Secrets:AccessToken` (and optionally the instance URL to `Umbraco:Community:Automate:Mastodon:Variables:InstanceUrl`) in `appsettings.json` or environment variables
2. Recreate your Mastodon connections in the backoffice (select the new **Mastodon** connection type in the **Social Networks** group)
3. Fill in both fields, using `$Umbraco:Community:Automate:Mastodon:...` references for any values stored in configuration

## Troubleshooting

**"Could not reach {InstanceUrl}"** — check the instance URL is absolute and has no trailing
slash: `https://mastodon.social`, not `mastodon.social` or `https://mastodon.social/`.

**"Authentication failed"** — the token is invalid, expired, or lacks the `write:statuses`
scope. Check the application still exists under **Preferences → Development** on your instance
and regenerate the token if needed.

**Either error when using a `$Umbraco:Community:Automate:Mastodon:...` reference** — the reference resolved to an
empty value, which usually means the key name doesn't match what's in configuration. Try the
literal value in the field to confirm the credentials themselves are good, then fix the key.

**The action fails but the connection tests fine** — most instances cap posts at 500 characters,
counting spoiler text and counting each link as 23. Posts over the cap are rejected by the API.

## Compatibility

| Package version | Umbraco Automate | Umbraco CMS |
|---|---|---|
| 2.x | 17.x – 18.x | 17.4 – 18.x |
| 1.x | 17.x – 18.x | 17.x – 18.x |

## Links

- [Source code](https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Mastodon/Umbraco.Community.Automate.Mastodon)
- [Report an issue](https://github.com/umbraco-community/Umbraco.Community.Automate/issues)
