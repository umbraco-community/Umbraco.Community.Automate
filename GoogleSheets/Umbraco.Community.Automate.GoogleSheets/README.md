# Umbraco Community Automate Google Sheets

Google Sheets connection and actions for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate).

## Overview

Umbraco.Community.Automate.GoogleSheets is a provider package that adds Google Sheets connectivity to Umbraco Automate. It contributes a Google Sheets connection type (authenticated via OAuth) and ten actions covering the spreadsheet lifecycle — creating spreadsheets and sheet tabs, appending/finding/updating/deleting/upserting rows, and reading or clearing ranges — so an automation can manage spreadsheet data end-to-end without leaving the workflow builder.

## Requirements

Supports `Umbraco.Automate` 17.x (declared range: `[17.0.0-beta.1, 18.0.0)`). See [CONTRIBUTING.md](../../CONTRIBUTING.md#supporting-multiple-umbraco-versions) for how this range is maintained and when it changes.

## Key Features

- **Google Sheets connection type** — OAuth-based connection managed in the backoffice, powered by [Umbraco.Automate.OpenIddict](https://www.nuget.org/packages/Umbraco.Automate.OpenIddict)
- **Row actions** — locate and mutate individual rows by column value:
  - **Append Row** — append a row of values to a sheet
  - **Find Row** — search a column for a matching value; `found`/`notFound` outcomes, with configurable match mode (Exact/Contains/StartsWith/EndsWith) and case sensitivity
  - **Update Row** — find a row and overwrite its column values; `updated`/`notFound` outcomes
  - **Delete Row** — find a row and delete it, shifting subsequent rows up; `deleted`/`notFound` outcomes
  - **Append or Update Row** — upsert: updates the row if a key column value already exists, otherwise appends a new row; `updated`/`appended` outcomes

  Find Row, Update Row, Delete Row, and Append or Update Row all skip the header row by default (a "First row is a header" toggle, on by default) — a lookup value that happens to equal a header label won't match or mutate it. Turn the toggle off for headerless sheets.
- **Range actions** — read or clear a sheet without a row lookup:
  - **Get Rows** — read every row from a tab or a specific A1 range, optionally separating the header row from the data rows
  - **Get Cell Value** — read a single cell by A1 notation (e.g. `A1`, `B5`)
  - **Clear Range** — clear values from a tab or a specific A1 range, preserving formatting
- **Structural actions** — manage spreadsheets and tabs themselves:
  - **Create Google Spreadsheet** — create a new spreadsheet, optionally with named sheet tabs
  - **Create Sheet Tab** — add a new tab to an existing spreadsheet
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

The provider is registered as `GoogleSheets` rather than the generic `Google`, following the OpenIddict [multiple-instances-of-the-same-provider](https://documentation.openiddict.com/integrations/web-providers#register-multiple-instances-of-the-same-provider) pattern. This means a future Google Drive or Google Docs package can register its own OpenIddict client (with its own unique provider name and redirect URI) without colliding with this one.

Once configured, create a Google Sheets connection in a workspace from the backoffice and authorize it via the OAuth popup. Any of this package's actions can then reference that connection — paste the sheet's URL or ID (and, where relevant, the tab name) into the action's settings, along with whatever the action needs: column values to write, a column/value to search or match on, an A1 range, or a new spreadsheet/tab title.

## Troubleshooting

If a run fails saying the connected account doesn't have access to the spreadsheet, share the spreadsheet with that specific Google account, or authorize the connection using an account that already has access to it.

If a run fails with a message that Google "couldn't find a spreadsheet" at the given URL/ID, double-check the link or ID is correct — that's usually a typo or a stale link. If it looks right, also check the sharing settings: access problems can occasionally surface as this same "not found" error rather than the access-denied one above.

If a run fails saying the sheet/tab name doesn't match, first verify the tab name in your spreadsheet matches exactly — including capitalisation. If the tab name is right, the connected account may not have permission to access the spreadsheet; this error can surface for cross-domain access (e.g. a personal Google account trying to append to a Google Workspace spreadsheet it hasn't been granted access to).

If a run fails saying Google is rate-limiting requests, this isn't an error with your setup — the [Sheets API has a fixed request quota](https://developers.google.com/workspace/sheets/api/limits), and the run will need to be retried after a short wait.

## License

MIT — see [LICENSE](../../LICENSE) for details.
