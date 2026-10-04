# Umbraco.Community.Automate.GoogleSheets

A Google Sheets connection and actions for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate): add, find, update and delete rows, read and clear ranges, and create spreadsheets and tabs, all from an automation.

The connection signs in to a Google account with OAuth, and its tokens are stored and refreshed automatically. Actions take a spreadsheet's URL or ID, and column values can use `${ bindings }` from the trigger or earlier steps, picked with a column-list editor in the backoffice.

## Installation

```bash
dotnet add package Umbraco.Community.Automate.GoogleSheets
dotnet add package Umbraco.Automate.OpenIddict --version <your Umbraco Automate version>
```

The second line matters: the package works with Umbraco Automate 17 and 18, so on its own NuGet installs the oldest version of Automate's OAuth support it allows, `Umbraco.Automate.OpenIddict` 17.0.0. Installing the version that matches your site's `Umbraco.Automate` (for example 18.1.5 alongside Automate 18.5, the latest of each) keeps the two in step. If they're out of step, backoffice sign-in can fail with "Your session has timed out".

There's nothing to register in code: Umbraco loads the package on its own. Then follow **Setup** below.

## Setup

Google needs an OAuth client for your site before anyone can connect. This is done once per site, in the Google Cloud Console.

### 1. Create a Google Cloud OAuth client

1. In the [Google Cloud Console](https://console.cloud.google.com/), create a project (or pick an existing one).
2. Under **APIs & Services → Library**, enable the **Google Sheets API**.
3. Under **APIs & Services → OAuth consent screen**, set up the consent screen. While it's in **Testing** mode, only the Google accounts listed under **Test users** can sign in, so add every account that will connect.
4. Under **APIs & Services → Credentials**, choose **Create credentials → OAuth client ID**, with application type **Web application**.
5. Under **Authorized redirect URIs**, add your site's callback URL, exactly: `https://your-site/umbraco/automate/oauth/callback/googlesheets`. Add one for each environment (e.g. `https://localhost:44343/umbraco/automate/oauth/callback/googlesheets` for local development).
6. Copy the **Client ID** and **Client secret**.

### 2. Store the client in configuration

Add both under `Umbraco:Automate:Providers:GoogleSheets`, which is where Umbraco Automate's OAuth support reads them (locally, use [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)):

```json
{
  "Umbraco": {
    "Automate": {
      "Providers": {
        "GoogleSheets": {
          "ClientId": "your-client-id.apps.googleusercontent.com",
          "ClientSecret": "your-client-secret"
        }
      }
    }
  }
}
```

In production, use environment variables instead: `Umbraco__Automate__Providers__GoogleSheets__ClientId` and `Umbraco__Automate__Providers__GoogleSheets__ClientSecret`. The site reads them at startup, so restart it after changing them. If they're missing, the site logs a warning and the connection editor says the provider isn't configured instead of opening Google's sign-in.

### 3. Create the connection

1. Go to **Automation → Settings → Connections** and create a new **Google Sheets** connection.
2. Click **Authenticate** and sign in with the Google account the automations should act as. That account needs access to the spreadsheets you'll use.

The provider is registered as `GoogleSheets` rather than the generic `Google`, following OpenIddict's [multiple instances of the same provider](https://documentation.openiddict.com/integrations/web-providers#register-multiple-instances-of-the-same-provider) pattern, so a future Google Drive or Docs package can register its own client without colliding with this one.

## Actions

Every action takes a **Spreadsheet** (the URL from your browser's address bar, or just the ID) and, except Create Google Spreadsheet, a **Sheet / tab name**. Values support `${ binding }` expressions.

| Action | Other settings | Outcomes |
|---|---|---|
| **Append Row to Google Sheet** | **Column values**: one value per column, in order. | |
| **Append or Update Row in Google Sheet** | **Key column**, **Column values**, **First row is a header**. Updates the row whose key column matches; otherwise appends. | `updated`, `appended` |
| **Find Row in Google Sheet** | **Search column**, **Search value**, **Match mode** (Exact, Contains, StartsWith, EndsWith), **Case sensitive**, **First row is a header**. | `found`, `notFound` |
| **Update Row in Google Sheet** | **Lookup column**, **Lookup value**, **Column values**, **First row is a header**. | `updated`, `notFound` |
| **Delete Row from Google Sheet** | **Lookup column**, **Lookup value**, **First row is a header**. Later rows shift up. | `deleted`, `notFound` |
| **Get Rows from Google Sheet** | **Range** (optional, A1 notation; the whole tab if empty), **First row is a header**. | |
| **Get Cell Value from Google Sheet** | **Cell** (A1 notation, e.g. `B5`). | |
| **Clear Range in Google Sheet** | **Range** (optional; the whole tab if empty). Keeps formatting. | |
| **Create Google Spreadsheet** | **Title**, **Sheet tab names** (optional). | |
| **Create Sheet Tab in Google Spreadsheet** | **Sheet tab title**. | |

**First row is a header** is on by default, so a lookup value that matches a header label never finds or changes the header row. Turn it off for sheets without one.

## Outcomes and outputs

Outcomes let later steps branch, e.g. only send a welcome email when **Append or Update Row** gives `appended`. Outputs are available as `${ steps.<alias>.<field> }`:

| Action | Outputs |
|---|---|
| Append Row | `updatedRange`, `updatedRows`, `updatedCells` |
| Append or Update Row | `rowNumber`, `updatedRange`, `updatedRows`, `updatedCells` |
| Find Row | `found`, `rowNumber`, `values` |
| Update Row | `rowNumber`, `updatedRange`, `updatedRows`, `updatedCells` |
| Delete Row | `deletedRowNumber` |
| Get Rows | `rows`, `rowCount`, `headers` |
| Get Cell Value | `value`, `isEmpty` |
| Clear Range | `clearedRange` |
| Create Google Spreadsheet | `spreadsheetId`, `spreadsheetUrl` |
| Create Sheet Tab | `sheetId`, `sheetTitle` |

## Troubleshooting

**Signing in**

| Google shows | What to do |
|---|---|
| *Error 401: invalid_client* / "The OAuth client was not found" | The Client ID in configuration isn't a real Google OAuth client, or the site is still using a placeholder. Check the value and restart the site. |
| *Error 400: redirect_uri_mismatch* | Add the exact callback URL from [step 1](#1-create-a-google-cloud-oauth-client) to the client's **Authorized redirect URIs**. Google's error page shows the `redirect_uri` the site sent; it must match character for character. |
| *Access blocked: … has not completed the Google verification process* | The consent screen is in Testing mode: add the account under **Test users**. |

**Running actions**

| The step fails with | What to do |
|---|---|
| The connected account doesn't have access to the spreadsheet | Share the spreadsheet with that Google account, or authenticate the connection with an account that has access. |
| Google couldn't find a spreadsheet at that URL or ID | Check the link or ID for a typo. If it's right, check the sharing too: access problems can sometimes show as "not found". |
| The sheet/tab name doesn't match | Check the tab name matches exactly, including capitals. If it does, the account may not have access, which can surface this way for cross-domain access (a personal account and a Google Workspace spreadsheet). |
| Google is rate-limiting requests | The [Sheets API has a request quota](https://developers.google.com/workspace/sheets/api/limits). Automate retries the step after a wait. |

**Rows added twice.** Google Sheets has no way to recognise a repeated request, so if a step times out after Google has already written the row, Automate's retry appends it again. Where duplicates matter, use **Append or Update Row** with a key column: a retry then updates the same row.

## Compatibility

| Package version | Umbraco Automate | Umbraco CMS |
|---|---|---|
| 1.x | 17.x – 18.x | 17.4 – 18.x |

One build supports both Umbraco 17 and 18: it's compiled against 17, and every change is tested on both, including running the 17 build on 18. Umbraco 19 isn't supported until it has been tested.

## Links

- [Source code](https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Packages/GoogleSheets/Umbraco.Community.Automate.GoogleSheets)
- [Report an issue](https://github.com/umbraco-community/Umbraco.Community.Automate/issues)
- [Google Sheets API documentation](https://developers.google.com/workspace/sheets/api)
