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
        "GoogleSheets": {
          "ClientId": "your-google-oauth-client-id",
          "ClientSecret": "your-google-oauth-client-secret"
        }
      }
    }
  }
}
```

Keep the client secret out of source control — use environment variables, user secrets, or a key vault to inject it at deployment time.

The OAuth callback URI follows the convention `{your-site}/umbraco/automate/oauth/callback/googlesheets` — add it to your OAuth client's **Authorized redirect URIs** in the Google Cloud Console.

The provider is registered as `GoogleSheets` rather than the generic `Google`, so other Google-flavored Automate packages (Drive, Gmail, etc.) can register their own OpenIddict client without colliding with this one — OpenIddict rejects two registrations that share the same provider name without a distinct identifier.

Once configured, create a Google Sheets connection in a workspace from the backoffice and authorize it via the OAuth popup. The **Append Row to Google Sheet** action can then reference the connection — paste the sheet's URL or ID, the tab name, and the ordered column values to append.

## Troubleshooting

If a run fails saying the connected account doesn't have access to the spreadsheet, share the spreadsheet with that specific Google account, or authorize the connection using an account that already has access to it.

If a run fails with a message that Google "couldn't find a spreadsheet" at the given URL/ID, double-check the link or ID is correct — that's usually a typo or a stale link. If it looks right, also check the sharing settings: access problems can occasionally surface as this same "not found" error rather than the access-denied one above.

If a run fails saying Google is rate-limiting requests, this isn't an error with your setup — the [Sheets API has a fixed request quota](https://developers.google.com/workspace/sheets/api/limits), and the run will need to be retried after a short wait.

## License

MIT — see [LICENSE](../LICENSE) for details.
