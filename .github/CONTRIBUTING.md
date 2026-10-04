# Contributing

Thanks for your interest in contributing! This repo holds community-maintained packages for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate). Each one is a NuGet package that adds connection types, actions or triggers, usually for one external service.

You can contribute in several ways: report a bug, improve a README, add an action to an existing package, or build a whole new package. If you're planning something large, [open an issue](https://github.com/umbraco-community/Umbraco.Community.Automate/issues) first so we can agree on the approach before you write the code.

## Contents

- [Prerequisites](#prerequisites)
- [Getting the code](#getting-the-code)
- [Repo layout](#repo-layout)
- [Running the Demo site](#running-the-demo-site)
- [Building and testing](#building-and-testing)
- [Making a change](#making-a-change)
- [Adding a new package](#adding-a-new-package)
  - [Packages without a provider](#packages-without-a-provider)
  - [What a package's pull request may change](#what-a-packages-pull-request-may-change)
- [Preventing secret leaks](#preventing-secret-leaks)
- [Releasing a package](#releasing-a-package)

## Prerequisites

| Tool | Needed for |
|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Everything |
| [gitleaks](https://github.com/gitleaks/gitleaks#installing) | The secret scan that runs before each commit and push (`winget install gitleaks`, `scoop install gitleaks` or `brew install gitleaks`) |
| A trusted HTTPS dev certificate | The Demo site. Run `dotnet dev-certs https --trust` once per machine. |
| [Node.js 22.x](https://nodejs.org/) | Packages with a backoffice front end (a `Client/` folder), currently Google Sheets and the Kitchen Sink example. The Demo site needs both built. |

Any editor works. Visual Studio, Rider and VS Code (with C# Dev Kit) all open `Umbraco.Community.Automate.Demo.slnx` at the repo root. The Demo site is the only runnable project, so starting the solution (F5) runs it.

## Getting the code

Contributions come in through pull requests from a fork:

```bash
# 1. Fork umbraco-community/Umbraco.Community.Automate on GitHub, then clone your fork
git clone https://github.com/<you>/Umbraco.Community.Automate.git
cd Umbraco.Community.Automate

# 2. Add the original repo as "upstream" so you can pull in other people's changes
git remote add upstream https://github.com/umbraco-community/Umbraco.Community.Automate.git
git fetch upstream

# 3. Turn on the git hooks (once per clone; see "Preventing secret leaks")
./.githooks/setup.sh        # macOS/Linux
.\.githooks\setup.ps1       # Windows
```

Before starting new work, bring your `main` up to date:

```bash
git checkout main
git pull upstream main
git push origin main
```

## Repo layout

```
Packages/                                     one folder per package
  _Development/                               for building and testing packages (built and tested, never published)
    Simple/                                   reference example: an API key and one action
    KitchenSink/                              reference example: every convention, including a trigger and a front end
    TestConnection/                           a Test Connection action, used by the Demo site's test pages
  <Area>/                                     e.g. Mastodon
    Umbraco.Community.Automate.<Area>/        the package (what ships to NuGet)
    Umbraco.Community.Automate.<Area>.Tests/  xUnit tests for the package
Umbraco.Community.Automate.Demo/              the Demo site: a throwaway Umbraco site referencing every package
.github/                                      CI and release workflows, this guide, design notes
.githooks/                                    gitleaks pre-commit and pre-push hooks
Directory.Packages.props                      central NuGet versions for every project
Umbraco.Community.Automate.Demo.slnx          the solution: every package, _Development and the Demo site
```

Each NuGet package lives in its own folder under `Packages/`. Most packages add a connection with its actions and triggers, but a package that only adds a trigger or an action, with no connection, is just as welcome and uses the same layout. `Packages/_Development/` holds what helps people build and test packages, laid out the same way, one level deeper (the underscore keeps it at the top of the list): two reference examples, mainly for the skill to build from, **Simple** (an API key and one action) and **KitchenSink** (every folder below, including a trigger and a front end); and **TestConnection**, a helper action that runs a connection's **Test connection** check from an automation. It's there so you can check you've put the right credentials in and that automations using the connection will actually work; the Demo site's test pages use it. None of them is for use in a real site: none has a release tag prefix, and all are marked not packable, so they're never published.

### Inside a package

Every package uses the same folder names, so you always know where to look. The folders for the building blocks Umbraco Automate defines (actions, triggers, connections) and for composers are plural:

| Folder | What goes in it |
|---|---|
| `Actions/` | One class per action, plus its settings and output classes |
| `Triggers/` | One class per trigger, plus its settings and output classes. The Kitchen Sink example shows one. |
| `Connections/` | The connection type and its connection settings (and their validator) |
| `Composers/` | The `IComposer` that registers the package's services (HTTP and API clients), plus any `IUmbracoBuilder` extensions. Connection types, actions and triggers aren't registered here: Automate discovers them from their attributes. |
| `Api/` | The C# client for the external service: HTTP client, error mapping, exceptions |
| `Models/` | Request and response models for the external API, at the package root (not inside `Api/`) |
| `Configuration/` | `<Area>Configuration` with the configuration paths, default references and fixed service values such as the API base URL (one place to change them), plus any options classes bound from `appsettings.json` and their validators |
| `Client/` | The backoffice front end source (Vite + Lit, built by npm into `wwwroot/`). Only for connections that need custom UI. Its `public/` folder holds what would otherwise be in `wwwroot/` and is copied there unchanged. |
| `wwwroot/` | Static backoffice files served under `App_Plugins/`: `umbraco-package.json`, which Umbraco discovers by itself and which registers the icons and any other backoffice extensions, and `icons/` (`icons.js` plus one `<area>.icon.js` per icon). Every package with a custom icon uses this same pattern. |

Next to those folders sit `Directory.Build.props` (package metadata and the MinVer tag prefix), `README.md` (shipped inside the NuGet package) and usually a package icon. Folders specific to one connection, such as DevTo's `Articles/` and `Content/`, are fine as well. Namespaces follow the folders, and where a test project groups its tests into folders, it uses the same names.

CI finds packages from this layout. Any folder at `Packages/<Area>/<Project>/` (or `Packages/_Development/<Name>/<Project>/`) that contains a `Directory.Build.props` counts as a package. It must have a sibling `.Tests` project, and it gets front-end jobs if `Client/package.json` exists. You don't need to edit any workflow file to add a package.

## Running the Demo site

`Umbraco.Community.Automate.Demo/` references every package, so it's where you check a change in the real backoffice. On first run it installs itself into a local SQLite database without any prompts.

### First run

```bash
# Build the backoffice front ends. Their output (wwwroot/) is gitignored, so without
# this step the Google Sheets and Kitchen Sink icons and editors won't appear.
cd Packages/GoogleSheets/Umbraco.Community.Automate.GoogleSheets/Client
npm ci
npm run build
cd ../../../..
cd Packages/_Development/KitchenSink/Umbraco.Community.Automate.Examples.KitchenSink/Client
npm ci && npm run build
cd ../../../../..

dotnet run --project Umbraco.Community.Automate.Demo
```

Then open <https://localhost:44343/umbraco> and log in:

| | |
|---|---|
| Email | `admin@example.com` |
| Password | `password1234` |

The **Automation** section is in the top navigation. Connections are under **Settings → Connections** in that section's tree, and automations are under **Automations**.

The site sets itself up on startup, adding only what's missing (`Setup/DemoSetup.cs`):

- an **Automate API User**, for workspaces to run as (a workspace's **Service Account Key** only accepts API users);
- **one connection per package**, named after the package and pre-filled with its configuration references, so it uses the placeholders in `appsettings.Development.json` or your own values in `appsettings.Local.json`. A new package gets one automatically;
- a **Demo workspace**, and an **Automate tests** page in **Content** with **a page per connection**, imported with [uSync](https://jumoo.co.uk/uSync) from `Umbraco.Community.Automate.Demo/uSync/`. The import runs whenever `uSync/` has a *Test:* automation the database doesn't, so a package added later gets its page on the next start, and otherwise leaves the site alone. Each page has its own document type and a *Test: …* automation that starts when that page is published. It runs the connection's **Test connection** check (with the **Test Connection** action from `Packages/_Development/TestConnection/`), then an **If** step branches on its `connected` output to show the result on screen: a green message when it works, or a red one with the reason when it doesn't. To try a package, publish its page (each page says so, from the shared *Automate test instructions* composition); to try its actions, add them after the *Connection works* step.

To change the *Test:* automations, edit them in the backoffice: uSync writes Automate changes back to `uSync/` as you save, so commit those files. It doesn't write document types or pages on save, so testing never changes files; to export changes to those, set `uSync:Settings:ExportOnSave` to `All` in your `appsettings.Local.json` while you make them, and commit only the files you meant to change. uSync never handles connections. uSync.Automate has no Umbraco 18 release yet, so all of this is only imported when the site runs on 17.

In Visual Studio or Rider, set `Umbraco.Community.Automate.Demo` as the startup project and choose the `Umbraco.Web.UI` launch profile.

### Working on a front end

While you edit a connection's `Client/` code, run its build in watch mode next to the Demo site and refresh the browser after each change:

```bash
cd Packages/GoogleSheets/Umbraco.Community.Automate.GoogleSheets/Client
npm run watch
```

### Using real credentials

The Demo site's `appsettings.Development.json` contains placeholder credentials (`e2e-test`) so that everything boots. To test against the real services, override those values in one of two ways. **Never edit the tracked config files with real values.**

**A local settings file** (simplest): copy `Umbraco.Community.Automate.Demo/appsettings.Local.example.json` to `appsettings.Local.json` in the same folder and replace the values for the services you want to try. The file is git-ignored, so it can't be committed, and the Demo site loads it after everything else, so it overrides the placeholders. Restart the site after changing it: some packages, like Google Sheets, read their settings at startup.

**User secrets**, stored outside the repo altogether:

```bash
dotnet user-secrets set "Umbraco:Automate:Providers:GoogleSheets:ClientId" "<client-id>" --project Umbraco.Community.Automate.Demo
dotnet user-secrets set "Umbraco:Automate:Providers:GoogleSheets:ClientSecret" "<client-secret>" --project Umbraco.Community.Automate.Demo
dotnet user-secrets set "Umbraco:Automate:Secrets:Mastodon:AccessToken" "<token>" --project Umbraco.Community.Automate.Demo
dotnet user-secrets set "Umbraco:Automate:Secrets:DevTo:ApiKey" "<api-key>" --project Umbraco.Community.Automate.Demo
dotnet user-secrets set "Umbraco:Automate:Secrets:Pushover:ApiToken" "<app-token>" --project Umbraco.Community.Automate.Demo
dotnet user-secrets set "Umbraco:Automate:Secrets:Pushover:UserKey" "<user-key>" --project Umbraco.Community.Automate.Demo
dotnet user-secrets set "Umbraco:Automate:Secrets:Skoda:ApiKey" "<api-key>" --project Umbraco.Community.Automate.Demo
dotnet user-secrets set "Umbraco:Automate:Secrets:WeatherApi:ApiKey" "<api-key>" --project Umbraco.Community.Automate.Demo
```

New connections are pre-filled with references to exactly these keys, so once they're set you can create a connection without typing any credentials.

For Google Sheets, also add `https://localhost:44343/umbraco/automate/oauth/callback/googlesheets` as an authorised redirect URI on your OAuth client. Skoda's VIN is entered on the connection. Each package's README explains where to get its credentials.

### Resetting the site

Stop the site and delete `Umbraco.Community.Automate.Demo/umbraco/Data/`. On the next run it reinstalls from scratch and sets itself up again: the API user, a connection per package, and (on Umbraco 17) the workspace and test pages. Anything else you created is gone.

## Building and testing

### .NET

```bash
dotnet build                 # the whole solution
dotnet test                  # every test project

dotnet test Packages/GoogleSheets/Umbraco.Community.Automate.GoogleSheets.Tests   # just one connection
```

CI also runs `dotnet pack` on each package. That catches packaging mistakes that a build won't, such as a missing README or icon. If you touch a `.csproj` or `Directory.Build.props`, run it yourself:

```bash
dotnet pack Packages/GoogleSheets/Umbraco.Community.Automate.GoogleSheets -c Release -o ./pack-check
```

### Using a package before it's released

To try a package in your own site before it's on NuGet, reference it from a clone of this repo:

```bash
dotnet add <your-site>.csproj reference <clone>/Packages/Mastodon/Umbraco.Community.Automate.Mastodon/Umbraco.Community.Automate.Mastodon.csproj
```

Or pack it into a local feed:

```bash
dotnet pack Packages/Mastodon/Umbraco.Community.Automate.Mastodon -c Release -o ./local-feed
dotnet add <your-site>.csproj package Umbraco.Community.Automate.Mastodon --source ./local-feed --prerelease
```

If the package has a `Client/` folder (Google Sheets), build it first (`npm ci && npm run build` in that folder), or its backoffice editors won't load. Then follow the package's README to set it up.

### Front-end unit tests

From a connection's `Client/` folder:

```bash
npm ci
npm test            # web-test-runner; add :watch to re-run on change
```

### End-to-end tests

The Playwright suite lives with Google Sheets (`Packages/GoogleSheets/Umbraco.Community.Automate.GoogleSheets/Client/tests/e2e`). It drives the Demo site in E2E mode, which swaps Google's API for a stub so that no real calls are made.

```bash
cd Packages/GoogleSheets/Umbraco.Community.Automate.GoogleSheets/Client
cp .env.example .env               # defaults match the Demo site
npm ci && npm run build
npx playwright install chromium    # first time only
npm run test:e2e                   # or test:e2e:ui to watch it run
```

Playwright starts the Demo site itself with `AUTOMATE_E2E_MODE=1`. If you already have the site running normally, stop it first. Otherwise Playwright reuses that instance, which isn't in E2E mode, and the tests fail.

### Umbraco version compatibility

Every package ships one build for Umbraco 17 and 18. It's compiled against the lowest supported versions and published with ranges that stop short of 19; both sets of versions live in the root `Directory.Packages.props`, switched by `-p:UmbracoMajor=18`. The Demo site runs on 17, the main version people build for. To try it on 18, run `dotnet run --project Umbraco.Community.Automate.Demo -p:UmbracoMajor=18`; that runs the packages' 17 builds on 18, as an Umbraco 18 site would. Umbraco upgrades the database but can't downgrade it, so reset the site (see [Resetting the site](#resetting-the-site)) before going back to 17.

CI checks every package on both: it tests against 17 and 18, runs the 17 build on 18, and checks every Umbraco API the 17 build calls still exists in 18. To run the same check yourself (it needs bash, so on Windows use Git Bash or WSL):

```bash
./tools/test-umbraco-compat.sh Packages/Mastodon/Umbraco.Community.Automate.Mastodon
```

## Making a change

1. **Branch off an up-to-date `main`.** Name branches `<type>/<short-description>`, e.g. `feat/google-sheets-clear-range`, `fix/oauth-token-refresh`, `docs/readme-cleanup`.
2. **Make the change with tests.** New actions and bug fixes should come with unit tests in the package's `.Tests` project. Check UI changes in the Demo site.
3. **Update the docs.** If users would notice the change (a new action, setting, outcome or error message), update that package's `README.md`. It's what NuGet shows.
4. **Commit** using `type(scope): Description`, e.g. `add(action): AppendRowAction — append a row of values to a sheet`, `fix(action): require LookupValue in UpdateRowAction`, `test(action): FindRowActionTests — found/notFound outcomes`. Common types: `add`, `fix`, `refactor`, `test`, `docs`, `chore`.
5. **Push to your fork and open a PR against `umbraco-community/main`.** CI runs backend tests, front-end tests and the Playwright E2E suite, but only for the connections your change touches. Changes outside `Packages/<Area>/` (the Demo site, `Directory.Packages.props`, workflows) run everything. All checks must pass before the PR is merged.

Keep PRs focused. A new action normally lands in its own PR, with its own tests.

If `main` moves on while your PR is open, rebase onto it:

```bash
git fetch upstream
git rebase upstream/main
git push --force-with-lease
```

## Adding a new package

**We recommend building every new package, and migrating every existing one, with the `umbraco-automate` skill** for [Claude Code](https://claude.com/claude-code) ([`.claude/skills/umbraco-automate/`](../.claude/skills/umbraco-automate/SKILL.md)). It builds every package the same way, so the structure, naming, configuration and tests stay consistent across the repo, and reviews can focus on what the package does rather than where things go. It loads automatically when you work in this repo. Ask "Using the Umbraco Automate skill, create me a package for <service>" and it researches the service, then walks you through the choices (what the package does, how it connects, names, settings) before building it. It can also migrate a package from its own repository into this one, or bring an existing package up to the current conventions. A migrated package can adopt the community naming (recommended) or keep its own package ID, namespaces and aliases for compatibility with existing users; everything else follows the conventions either way. For anything else Automate-related ("Using the Umbraco Automate skill, add a trigger to Mastodon"), it follows the layout and conventions here. The checklist below is the same process by hand.

1. **Create the projects.** Add `Packages/<Area>/Umbraco.Community.Automate.<Area>/` and `Packages/<Area>/Umbraco.Community.Automate.<Area>.Tests/`. Start from the [Simple example](../Packages/_Development/Simple/Umbraco.Community.Automate.Examples.Simple/README.md) for an API key and an action, or the [Kitchen Sink](../Packages/_Development/KitchenSink/Umbraco.Community.Automate.Examples.KitchenSink/README.md) when you need a trigger, outcomes or a front end; delete what you don't need, and give your copy a `MinVerTagPrefix` and remove `IsPackable=false`. Among the real connections, WeatherApi is the simplest; Google Sheets shows OAuth.
2. **Use the standard folders** from [Inside a package](#inside-a-package): `Composers/`, plus `Actions/`, `Triggers/` and `Connections/` for whichever of those the package adds, and `Api/`, `Configuration/`, `Client/` and `wwwroot/` when you need them.
3. **Add a `Directory.Build.props`** modelled on an existing package's, with your own `MinVerTagPrefix` (e.g. `newconnection-v`). This file is also what makes CI pick up the package.
4. **Wire it in.** Add both projects to `Umbraco.Community.Automate.Demo.slnx`, and reference the package from `Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj`. Add the package to the list in `.github/ISSUE_TEMPLATE/bug-report.yml`, and a line for it with its maintainers in `.github/CODEOWNERS`.
5. **Add package versions to `Directory.Packages.props`.** Projects don't specify versions themselves.
6. **Write the package `README.md`**: installation, setup, every setting, outcomes and outputs, troubleshooting and compatibility. Add a row for it to the table in the root `README.md`.
7. **Add the package's configuration to the Demo site.** Every key the package reads goes in two files, under the same paths it uses: obviously fake placeholders (`"e2e-test"`) in `Umbraco.Community.Automate.Demo/appsettings.Development.json`, so the site boots and connections resolve, and `your-...` values in `appsettings.Local.example.json`, the template for trying it with real credentials. Never put real values in either file.
8. **Add a test page for the package to the Demo site:** copy the Pushover files in `Umbraco.Community.Automate.Demo/uSync/v17/` and change the name, alias, icon and every `Key` and step `Id` (a new GUID each): `ContentTypes/automatetestpushover.config` (the page type), `Content/pushover.config` (the page; its `ContentType` is the new type's alias), and `Automate-Automations/testpushover.config` (its trigger's `contentTypes` is the new type's key, its Test Connection step's `connectionAlias` is the package's connection alias, e.g. `community.bluesky` becomes `bluesky`, and its two Notify Editor steps, after the If step, name the package). Then add the new type to the `<Structure>` list in `ContentTypes/automatetests.config`, so it's allowed under *Automate tests*, and the connection alias to `<AllowedConnections>` in `Automate-Workspaces/demo.config` (with a new GUID as its `Key`). Check it by restarting the Demo site, which imports the new page, then publishing it. Reviewers test the package this way, so it's needed before a pull request. If a package is later removed or renamed, remove or rename its test page files, these two lines and its configuration keys too, so the Demo never refers to a package that doesn't exist.

Before the first release, follow [Adding a new package to this scheme](#adding-a-new-package-to-this-scheme) below.

### Packages without a provider

Not every package connects to a service. Triggers and actions that work on Umbraco itself, or general-purpose steps that need no connection, are just as welcome. They go in `Packages/` like any other package, with the same layout and no `Connections/` folder. There are no separate top-level folders for triggers, actions or connections, because the thing people install, version and release is a package, and most packages contain more than one kind.

- **Name it after what it's for**, e.g. `Packages/ContentTools/` for extra content triggers and actions, or `Packages/Text/` for steps that format text and dates. Keep each package to one purpose.
- **Don't make a catch-all package** (`Common`, `Utilities`). Everyone would install every step to get one, and any change would release them all. If a step fits an existing general package, add it there; if not, start a new focused one.
- **Don't duplicate what Umbraco Automate already does.** Umbraco Automate 17 already includes these, so check the list before building something general-purpose:

| | Built in |
|---|---|
| Content | Triggers: Content Published, Saved and Unpublished (and their batch versions). Actions: Find Content, Get Content, Get Content Property, Update Content Property, Publish Content, Unpublish Content, Notify Editor |
| Media | Triggers: Media Saved, Deleted and Trashed (and batch versions). Action: Update Media Property |
| Members and users | Triggers: Member Saved and Deleted; User Saved, Deleted, Locked, Login Success, Login Failed and Password Changed |
| General | Triggers: Manual, Scheduled, Webhook. Actions: HTTP Request, Send Email, Log Message, Delay, Request Approval, Set Variable, plus the If, Switch, While and For Each steps |

- **Don't share code between packages yet.** A shared library would be a dependency every package has to keep in step with. A few small duplicated classes are cheaper; we'll revisit it if the same substantial code turns up in several packages.

### What a package's pull request may change

A pull request that adds or changes a package should only change that package. Packages are independent: a change for one must never alter how another builds, behaves or is released. A package's pull request may touch:

- everything under its own `Packages/<Area>/` folder;
- its two lines in `Umbraco.Community.Automate.Demo.slnx`, and its `ProjectReference` in `Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj`;
- its configuration keys, under its own name, in `Umbraco.Community.Automate.Demo/appsettings.Development.json` (placeholders) and `appsettings.Local.example.json` (the template for real values);
- its test page's three files in `Umbraco.Community.Automate.Demo/uSync/v17/`, its line in `ContentTypes/automatetests.config` and its connection's line in `Automate-Workspaces/demo.config`;
- **new** entries in `Directory.Packages.props` for libraries no package uses yet;
- its row in the root `README.md`, its line in `.github/CODEOWNERS`, its entry in `.github/ISSUE_TEMPLATE/bug-report.yml`, and its `wwwroot/` line in `.gitignore` if it has a `Client/`.

It shouldn't change other packages, anything in `Packages/_Development/`, the CI and release workflows (`.github/workflows/`), `.githooks/`, the root `.editorconfig` or `.gitattributes`, the skill in `.claude/`, or the version of a library other packages already use. If one of those really is necessary (a shared library needs an upgrade, CI can't build your package), open an issue or a separate pull request for it and explain why, so it can be reviewed on its own. `CODEOWNERS` asks the owners of anything outside your package to review changes to it. CI also adds a warning to a pull request that changes more than one package or anything in `.github/workflows/`; it doesn't fail the build, but reviewers will ask why.

## Preventing secret leaks

Never put real credentials (OAuth Client IDs/Secrets, API keys, etc.) into a git-tracked file like `appsettings.Development.json` — even locally, even temporarily. Put them in the git-ignored `appsettings.Local.json` or in [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets), which are stored outside the repo entirely (see [Using real credentials](#using-real-credentials)). Both override the tracked placeholder values when you run the Demo site, and neither can be committed.

### Git hooks

The setup script from [Getting the code](#getting-the-code) needs running once per clone:

```bash
./.githooks/setup.sh        # macOS/Linux
```

```powershell
.\.githooks\setup.ps1       # Windows
```

It points git at the hooks in `.githooks/`, which run [gitleaks](https://github.com/gitleaks/gitleaks) automatically: `pre-commit` scans what you've staged, and `pre-push` scans every commit you're about to push. They're a second, local layer of defence in case a real secret ever ends up staged despite the above. If you set this repo up with the old Lefthook-based script, run the setup script again.

### If a commit or push is blocked

If gitleaks finds something that looks like a secret, your commit or push will fail with output identifying the file and line. To resolve it:

1. Unstage or remove the offending change (`git restore --staged <file>` or edit the file to remove the real value).
2. If it's a real credential you need locally, put it in `appsettings.Local.json` or set it with `dotnet user-secrets set` instead (see above).
3. Re-commit/re-push.

## Releasing a package

Releases are cut by maintainers with push access to this repo. Contributors don't need to do anything here: once your PR is merged, it ships in that package's next release.

This repo uses [MinVer](https://github.com/adamralph/minver) to derive each package's version from git tags — there is no hand-maintained `<Version>` anywhere. Each package has its own tag prefix (e.g. `googlesheets-v`) configured via `MinVerTagPrefix` in that package's own `Directory.Build.props`, so packages in this monorepo version independently: a `googlesheets-v1.0.0` tag only affects the Google Sheets package, even if other packages have had commits in between.

`MinVerIgnoreHeight` is also set to `true` for the same reason: MinVer normally auto-generates a pre-release version between tags based on how many commits have happened since the last tag (its "height"). In a monorepo, that height would react to *other* packages' commits too, producing a misleading version. Since a release here only ever happens by deliberately pushing a tag — never automatically between tags — that auto-increment behaviour isn't needed, so it's turned off.

### How a release happens

1. **Push a tag** matching `<package-slug>-v<semver>` to this repo, e.g.:
   ```bash
   git tag googlesheets-v1.0.0
   git push origin googlesheets-v1.0.0
   ```
   This is the *only* thing that starts a release — merging to `main` never does.
2. `.github/workflows/release.yml` resolves which package the tag belongs to by finding the `Directory.Build.props` whose `MinVerTagPrefix` matches, then runs that package's full CI suite (the same checks as a normal PR, via a call to `ci.yml`) against the tagged commit.
3. If CI passes, it packs the `.nupkg` and `.snupkg` and **pauses for manual approval** — the publish job targets the `nuget-publish` GitHub Environment, which requires a reviewer to approve the run in the GitHub UI before anything is published.
4. Once approved, it publishes to nuget.org using [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) — GitHub's OIDC token is exchanged for a short-lived NuGet API key, so no long-lived API key is ever stored as a repository secret.
5. It creates a GitHub Release for the tag, titled from the package's `<Title>` (e.g. "Umbraco Community Automate Google Sheets v1.0.0"), with GitHub's auto-generated release notes and the `.nupkg`/`.snupkg` attached as downloadable assets.

### Why a fork can't publish a release

Trusted Publishing is bound to this specific repository and workflow — a fork's copy of `release.yml` would request an OIDC token identifying it as `<forker>/Umbraco.Community.Automate`, which nuget.org's trust policy for this package rejects outright, regardless of what the workflow file says. Combined with the fact that a tag pushed to a fork never triggers a workflow run in the upstream repo at all, the actual access boundary for cutting a release is simply **push access to this repository** — the same permission that already lets someone merge to `main`. The `nuget-publish` environment's required-reviewer approval adds a second, deliberate confirmation on top of that.

### Adding a new package to this scheme

1. Add `MinVerTagPrefix` (e.g. `newpackage-v`) and `MinVerIgnoreHeight = true` to that package's own `Directory.Build.props`, following the Google Sheets package as a template.
2. Register a Trusted Publishing policy for the new package on nuget.org, scoped to this repo, the `release.yml` workflow, and the `nuget-publish` environment.
3. No changes to `release.yml` itself are needed — it resolves the package from the tag automatically.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](../LICENSE).
