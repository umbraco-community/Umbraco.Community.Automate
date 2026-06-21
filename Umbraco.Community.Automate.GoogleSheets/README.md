# Umbraco Community Automate Google Sheets

Google Sheets connection and actions for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate).

## Overview

Umbraco.Community.Automate.GoogleSheets is a provider package that adds Google Sheets connectivity to Umbraco Automate. It contributes a Google Sheets connection type (authenticated via OAuth) and an Append Row action that can be used as a step in automations — for example, appending a row to a spreadsheet whenever a form is submitted or content is published.

## Key Features

- **Google Sheets connection type** — OAuth-based connection managed in the backoffice, powered by [Umbraco.Automate.OpenIddict](https://www.nuget.org/packages/Umbraco.Automate.OpenIddict)
- **Append Row action** — append a row of values to a sheet from an automation step, with bindings supported on the spreadsheet, sheet name, and every column value
- **Column list editor** — a repeatable column-value editor with an "Insert binding" picker per row, so values can reference earlier steps' outputs
- **Automatic token management** — OAuth credentials are stored and refreshed transparently
- **Setup status warning** — the connection editor warns and disables "Authenticate" if the provider's client ID/secret haven't been configured yet, instead of failing in the OAuth popup

## Installation

```bash
dotnet add package Umbraco.Community.Automate.GoogleSheets
```

## Configuration

Create an OAuth 2.0 Client ID in the [Google Cloud Console](https://console.cloud.google.com/apis/credentials) (enable the **Google Sheets API** for the project first), then configure its credentials via `appsettings.json`:

```json
{
  "Umbraco": {
    "Automate": {
      "Providers": {
        "Google": {
          "ClientId": "your-google-oauth-client-id",
          "ClientSecret": "your-google-oauth-client-secret",
          "Scopes": [ "https://www.googleapis.com/auth/spreadsheets" ]
        }
      }
    }
  }
}
```

Keep the client secret out of source control — use environment variables, user secrets, or a key vault to inject it at deployment time.

The OAuth callback URI follows the convention `{your-site}/umbraco/automate/oauth/callback/google` — add it to your OAuth client's **Authorized redirect URIs** in the Google Cloud Console.

Once configured, create a Google Sheets connection in a workspace from the backoffice and authorize it via the OAuth popup. The **Append Row to Google Sheet** action can then reference the connection — paste the sheet's URL or ID, the tab name, and the ordered column values to append.

## License

MIT — see [LICENSE](../LICENSE) for details.
