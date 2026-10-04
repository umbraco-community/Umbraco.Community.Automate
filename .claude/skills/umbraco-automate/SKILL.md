---
name: umbraco-automate
description: The house rules for building anything for Umbraco Automate in the Umbraco.Community.Automate repo: connections, actions, triggers, outcomes and outputs, API clients, configuration and secrets ($-references in Umbraco:Automate:Secrets/Variables), backoffice front ends (Client/, Vite + Lit), tests (xUnit only), the Demo site, CI and releases. Covers the Packages/<Area>/ layout, standard folder names (Actions, Triggers, Connections, Composers, Api, Models, Configuration, Client), community.<area> aliases and project wiring, with Packages/_Development/Simple and Packages/_Development/KitchenSink as the references to copy. Use it whenever someone wants to create or change a package, connection, action or trigger (e.g. "build me an Automate package for Facebook", "add a Slack connection", "add a trigger to Mastodon"), migrate or import an existing Automate package into the repo or bring one up to the current conventions, write tests or a front end for one, wire something into the Demo site (its configuration and uSync test page), prepare a pull request for a package, remove or rename a package, or asks where something goes or how something is done in this repo, even if they don't mention Automate or the skill.
---

# Building for Umbraco Automate

This repo is a monorepo of community packages for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate). Each connection is one NuGet package that adds a **connection type** (credentials for an external service), **actions** and **triggers** that automations can use. This skill covers anything built here: new connections, new actions or triggers on existing ones, front ends, configuration, tests and the Demo site.

Every connection follows the same shape on purpose: contributors and reviewers should be able to open any package and know where everything is, and CI discovers packages purely from the folder layout. Follow this skill so a new connection looks like the existing ones. When in doubt, copy one of the two reference connections in `Packages/_Development/` (reference only, never for a real site: both talk to httpbin.org, are built and tested by CI, and are never published): **`Packages/_Development/Simple/`** is an API key and one action, the shape most connections need; **`Packages/_Development/KitchenSink/`** uses every folder here, including a trigger, outcomes, an API client, a custom field editor in `Client/`, and xUnit-only tests. Among the real connections, **WeatherApi** is the simplest; **Google Sheets** shows OAuth; **DevTo** shows content conversion in depth.

## Start here

Work out which kind of request this is, because each has its own process:

- **A new package** ("build me a package for Facebook", "I want a Slack integration"): don't start coding. Follow [references/new-package.md](references/new-package.md): research the service, then take the user through multiple-choice decisions (what it does, how it connects, shape, names, settings) and build only after they confirm a summary.
- **Migrating an existing package into this repo**, or **bringing a package here up to the current conventions**: follow [references/migrating.md](references/migrating.md), which reads the source, asks about anything that affects existing users (aliases, configuration, package ID), then reshapes it.
- **A change to an existing package** (a new action, a fix, a new setting): no interview needed. Follow "Adding to an existing connection" below and the conventions.
- **Finishing a package for review**: setting it up in the Demo site, or opening a pull request for it. Follow [Demo setup](#demo-setup), including its pull request check.
- **Removing or renaming a package**: tidy up after it as in [Keep the Demo in step with the packages](#keep-the-demo-in-step-with-the-packages).
- **A question** about where something goes or how something is done: answer it from this skill and the repo.

When you do need the user to choose, ask multiple-choice questions with a recommended option first (the `AskUserQuestion` tool when available), rather than open questions or silent assumptions.

## Choose a shape first

Most community connections are simple: an API key and an action or two. Build that unless the package needs more, and grow it later; never add folders "just in case".

| | **Simple** (most packages) | **Full** (only what's needed from it) |
|---|---|---|
| Use when | an API key (or token) and actions that each make one call | a trigger, outcomes, several actions sharing request and error handling, or a custom field editor |
| Folders | `Actions/`, `Composers/`, `Configuration/`, `Connections/` | any of the standard folders below, including `Api/`, `Models/`, `Triggers/`, `Client/`, `wwwroot/` |
| Icon | either: the vendor's logo for a commercial service, otherwise a built-in Umbraco icon (see [Icons](#icons)) | the same |
| Reference code | `Packages/_Development/Simple/` | `Packages/_Development/KitchenSink/` |
| Every file, ready to copy | [references/simple.md](references/simple.md) | [references/full.md](references/full.md) |

Read the matching reference before writing code, and [references/fields.md](references/fields.md) for every settings class. Both are complete (project files, every class, tests and wiring) and are checked by generating a package from them and running its tests. The guided process in [references/new-package.md](references/new-package.md) settles the shape with the user: recommend Simple unless their answers need something from the Full column. When a Simple package later needs one Full feature, add just that section from `full.md`: the files are written to fit together.

## Layout

```
Packages/<Area>/                                  <Area> = PascalCase name, usually the service, e.g. Bluesky
  Umbraco.Community.Automate.<Area>/              the package
    Actions/        one class per action + its settings and output classes
    Triggers/       one class per trigger + its settings and output classes (when needed)
    Connections/    the connection type + its connection settings (+ validator)
    Composers/      IComposer, IUmbracoBuilder extensions
    Api/            C# client for the external service: client, error mapping, exceptions
    Models/         request and response models for the external API
    Configuration/  <Area>Configuration (config paths, default references, fixed service values), plus options classes if genuinely needed
    Client/         backoffice front end (Vite + Lit, npm). Only if custom UI is needed
    wwwroot/        static backoffice files served under App_Plugins/: umbraco-package.json, icons/
    Directory.Build.props
    README.md
    community-automate-128.png
    Umbraco.Community.Automate.<Area>.csproj
  Umbraco.Community.Automate.<Area>.Tests/        xUnit tests, same folder names where grouped
```

Why these names:
- **Plural** for Automate's building blocks (`Actions`, `Triggers`, `Connections`) and for `Composers`, matching each other and Umbraco's own convention for composers.
- **`Connections/`, not `Connection/` or `ConnectionType/`**: a singular `Connection` namespace shadows Automate's `Connection` type inside the package's own namespace tree and breaks code that uses it.
- **`Client/` is only ever the npm front end.** CI gives a package front-end jobs when `Client/package.json` exists, so a C# API client must go in `Api/`, never `Client/`.
- Namespaces follow folders: `Umbraco.Community.Automate.<Area>.Actions`, `.Connections`, and so on.
- Folders unique to one connection (e.g. DevTo's `Articles/`, `Content/`) are fine alongside the standard ones.

Don't create empty folders: add `Triggers/`, `Api/`, `Models/`, `Configuration/`, `Client/` or `wwwroot/` only when there's something to put in them.

A package doesn't have to be a connection. One that only adds a trigger or an action (for example an Umbraco-side trigger, or an action that needs no external service) is just as welcome: it uses the same layout with only the folders it needs, say `Triggers/` and `Composers/`, and no `Connections/`. See **Packages without a provider** below.

Keep the structure flat: these packages are small, so standard folders sit directly in the package root (models in a root `Models/`, not `Api/Models/`), which makes everything easy to find.

## Creating a new connection

Work through these in order and tick them off. For a package with no connection, skip the connection steps (3, 4 and the connection validation) and the `ConnectionTypeAlias` on actions; everything else applies. Before starting, settle the service, its actions, authentication, shape and names with the user through the guided process in [references/new-package.md](references/new-package.md).

1. **Projects.** Create the package and `.Tests` projects under `Packages/<Area>/` from [simple.md](references/simple.md) or [full.md](references/full.md). If the package has a `wwwroot/` (custom icons), use `Microsoft.NET.Sdk.Razor` with `StaticWebAssetBasePath` `App_Plugins/UmbracoCommunityAutomate<Area>` so it's served as static web assets; otherwise plain `Microsoft.NET.Sdk` is enough (see WeatherApi).
2. **`Directory.Build.props`** in the package folder, with `MinVerTagPrefix` `<area-lowercase>-v` and `MinVerIgnoreHeight` true. This file is also the marker that makes CI find the package; without it the package is silently skipped.
3. **Configuration class** `Configuration/<Area>Configuration.cs` holding the config paths, each credential's default reference and the service's fixed values such as its base URL (see Configuration and secrets).
4. **Connection type and settings** in `Connections/`, with credential fields defaulting to those references.
5. **Actions** in `Actions/`, each with its settings (and output, if it returns data). Full: an `Api/` client shared by the actions, outcomes, and **triggers** in `Triggers/`.
6. **Composer** in `Composers/`: register the package's **services** only (HTTP clients, an API client). Don't register the connection type, actions or triggers: Automate discovers them from their `[ConnectionType]`, `[Action]` and `[Trigger]` attributes. Icons and configuration references need no registration either.
7. **Icons**: the vendor's logo for a commercial service, found on the web, and a built-in Umbraco icon (e.g. `icon-link`, no files needed) for everything else; see [Icons](#icons). A logo is registered with `wwwroot/umbraco-package.json`, which lists `icons/icons.js`, which lists `icons/<area>.icon.js` (in `Client/public/` instead if the package has a `Client/`). No C# is involved; add the icon test from [full.md](references/full.md#tests).
8. **Package versions** go in the root `Directory.Packages.props` only; `.csproj` files never specify versions. Every package supports Umbraco 17 and 18 from one build: Umbraco and Automate packages are listed there twice, as ranges for each major (`-p:UmbracoMajor=18` switches), so a new one goes in both groups.
9. **Wire it in**: add both projects to `Umbraco.Community.Automate.Demo.slnx` (in a `/Packages/<Area>/` solution folder) and a `ProjectReference` from `Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj`. Add the package to the list in `.github/ISSUE_TEMPLATE/bug-report.yml`, and a line for it with its maintainers in `.github/CODEOWNERS`.
10. **Demo setup**: follow [Demo setup](#demo-setup) below. Add the configuration placeholders as soon as the package reads configuration, since the Demo needs them to resolve the connection's references. The test page can wait until the package works, often in a later prompt, but it must be done before the pull request.
11. **Tests** for every action, outcome and trigger and the connection validation (see Testing). Full with a `Client/`: front-end tests too, and build it (`npm ci && npm run build`) before running the Demo site or packing.
12. **README.md** in the package (see README), and a row in the root `README.md` connections table.
13. **Verify**: `dotnet build`, `dotnet test`, `dotnet pack Packages/<Area>/Umbraco.Community.Automate.<Area> -c Release -o ./pack-check`, `tools/test-umbraco-compat.sh Packages/<Area>/Umbraco.Community.Automate.<Area>` (bash; checks Umbraco 17 and 18), then run the Demo site: check the connection type appears under **Automation → Settings → Connections → Create** with its icon and pre-filled references, and that **Test connection** resolves them (put placeholder values in `Umbraco.Community.Automate.Demo/appsettings.Development.json` or user secrets).

You don't need to edit any CI workflow: `ci.yml` discovers packages from the layout, and `release.yml` finds a package from its tag prefix.

When you finish the first build and the Demo setup isn't complete yet, say so at the end of your reply, listing what's left (configuration placeholders, test page), so the user knows it's still to do before a pull request.

## Demo setup

Every package gets a working setup in the Demo site, so a reviewer or tester can run the site, publish one page and see whether the package connects, without reading its code. It's the last part of building a package, often done in a follow-up prompt once the package works, and it's required before a pull request.

**1. Configuration.** Add **every** configuration key the package reads (each credential reference, any variables, OAuth provider settings), under the same paths as `<Area>Configuration`, to:
- `Umbraco.Community.Automate.Demo/appsettings.Development.json`: obviously fake placeholders (`"e2e-test"`), so the site boots, the Demo's connection resolves its references, and CI's end-to-end runs work without real credentials.
- `Umbraco.Community.Automate.Demo/appsettings.Local.example.json`: the same keys with `your-...` values, the template contributors copy to `appsettings.Local.json` to try the package live. Use non-empty values: an empty one overrides the placeholder and can stop the site starting.
- The contributor's own `appsettings.Local.json`, if they have one (it's git-ignored): add the package's keys with the same `your-...` stubs, so they only have to fill them in. Only add missing keys; never change, print or repeat the values already there, which are real credentials.

Real credentials never go in a tracked file; they go in `appsettings.Local.json` or user secrets.

**2. Test page.** The Demo creates the package's connection by itself, aliased after the connection type (`community.bluesky` becomes `bluesky`, `community.examples.simple` becomes `examples-simple`). The test page proves it works: publishing it runs a *Test:* automation that checks the connection and shows a green or red message. Copy the Pushover files in `Umbraco.Community.Automate.Demo/uSync/v17/` and change the name, alias, icon and every `Key` and step `Id` (a new GUID each):
- `ContentTypes/automatetestpushover.config`: the page type.
- `Content/pushover.config`: the page; its `ContentType` is the new type's alias.
- `Automate-Automations/testpushover.config`: its trigger's `contentTypes` is the new type's key, its Test Connection step's `connectionAlias` is the package's connection alias, and its two Notify Editor steps, after the If step, name the package.

Then add the new type to the `<Structure>` list in `ContentTypes/automatetests.config`, so it's allowed under *Automate tests*, and the connection alias to `<AllowedConnections>` in `Automate-Workspaces/demo.config` (with a new GUID as its `Key`), so the Demo workspace's automations can use it. A package with no connection gets a test page whose automation uses one of its own actions or triggers instead of Test Connection, or none if there's nothing to show; say which in the pull request.

**3. Check it.** Restart the Demo site on Umbraco 17 (uSync.Automate has no 18 release). On startup it imports `uSync/` whenever the files have a *Test:* automation the database doesn't, so the new page and automation appear without resetting anything. The import republishes every test page, so each *Test:* automation runs once then; that's expected. Run the site, open *Automate tests*, publish the package's page, and check the message: with placeholders a real service shows the red *test failed* message naming the bad credential, which proves the wiring; with real values in `appsettings.Local.json` it's green.

### Keep the Demo in step with the packages

The Demo's files must only ever describe packages that exist, with the keys and aliases they use now. A stray entry gets committed by accident: configuration for a package that's gone, or a test automation pointing at an alias nobody has.

- **A package's keys, aliases or name change**: update its entries in all three configuration files and its test page files in the same change.
- **A package is removed, renamed, or abandoned on a branch**: remove its configuration keys from `appsettings.Development.json` and `appsettings.Local.example.json` (and offer to remove them from the contributor's `appsettings.Local.json`), its three uSync files, its `<Structure>` and `<AllowedConnections>` lines, and the rest of its wiring (solution, Demo `ProjectReference`, root README row, `CODEOWNERS`, bug report entry). Removing the uSync files doesn't delete the page or automation from an existing Demo database; say so, and suggest a fresh database.
- **Before committing**, check both directions: every area under `Umbraco:Automate:Secrets`, `Variables` and `Providers` in the two tracked files, and every `connectionAlias` and `contentTypes` in `uSync/v17/`, belongs to a package that exists; and every package has its keys and test page.

### Pull requests for a new package

Before opening (or writing the description of) a pull request that adds a package, check the Demo setup is complete: placeholders in `appsettings.Development.json`, the template in `appsettings.Local.example.json`, the three uSync test page files, the `<Structure>` and `<AllowedConnections>` lines, and the page checked in the Demo site. If anything is missing, **flag it to the user before creating the pull request** and offer to do it now; testers rely on it to try the package. Tick the matching boxes in the pull request template, and mention in the description how to test it: *run the Demo, publish Automate tests → <Name>*.

## Conventions

### Aliases, names and groups
- **Aliases are permanent once released.** Saved connections and automations store the connection type, action and trigger aliases, so renaming one breaks them. Pick them carefully up front.
- Prefix every alias with `community.`: `community.<area>` for the connection type and `community.<area>.<name>` (camelCase) for actions and triggers, e.g. `community.bluesky` and `community.bluesky.createPost`. The prefix keeps community packages apart from Umbraco's own step types. Every new package uses it, with the `Umbraco.Community.Automate.<Area>` package ID and namespaces. The only exception is a migrated package whose author chooses to keep their own package ID, namespaces and aliases (see [references/migrating.md](references/migrating.md)); the rest of the conventions still apply to it.
- Set `ConnectionTypeAlias` on every action to the connection type alias, so the action only offers matching connections.
- Write the alias, name, group and icon directly on each `[ConnectionType]`, `[Action]` and `[Trigger]` attribute, as Umbraco Automate's own step types do. Don't gather them into an `<Area>Constants` class: the attribute is where a reader looks for them.
- Use a shared `Group` so related connections sit together in the pickers, picking from the ones already in use before inventing one: **Social Networks** (Mastodon, DevTo), **Productivity** (Google Sheets), **Notifications** (Pushover), **Weather** (WeatherApi), **Vehicles** (Skoda). Name a new group after the kind of service, never the service itself, so the next similar package can join it. The group is only a display heading, not stored in automations, so it can change later.
- Use the same icon on the connection type, its actions and triggers (see [Icons](#icons)).
- Register backoffice extensions (icons, editors, modals) in a static `umbraco-package.json`, which Umbraco discovers under `App_Plugins/` by itself. Don't use an `IPackageManifestReader` or list icons in `Client/src/`: one pattern everywhere means a contributor adding an icon only ever touches the same three files.

### Keep names and help text short
Names and descriptions are shown in narrow pickers, step cards and settings panels, where long text wraps, gets cut off and pushes the settings apart. Say what something does, plainly, and stop: anything longer belongs in the package README.

| Text | Aim for | Example |
|---|---|---|
| Connection type, action and trigger names | 2 to 4 words, under about 30 characters: a verb and an object for actions | *Send Notification*, *Append Row*, *Article Published* |
| Their `Description` | one sentence, under about 100 characters, saying what it does | *Sends a push notification to your devices.* |
| Field `Label` | 1 to 3 words | *API key*, *Sheet tab*, *Priority* |
| Field `Description` | one or two short sentences, under about 120 characters: what the value is, its format or an example, and the default | *Column letter used to find an existing row, e.g. A. Leave blank to always add a new row.* |
| Outcome and output names | a word or two | `created`, `notFound`, `articleUrl` |

- Don't repeat what the UI already shows: the group heading, the connection's icon, or the label in its own description (*API key: The API key for...*).
- Leave out edge cases, caveats and how-it-works detail; put them in the README's settings table or Troubleshooting section.
- Add the service's name to an action or trigger name only where it would be unclear without it.
- If a description needs a third sentence, the setting is probably doing too much, or the detail belongs in the README.

### Icons
Choose the icon by what the package talks to:

- **A commercial vendor or named service** (Pushover, Google Sheets, Mastodon, a car maker's API): use the vendor's logo, so editors recognise the service in the pickers. Most contributors won't have the SVG to hand, so search the web for it yourself, then offer it alongside the choice of their own file (see [new-package.md](references/new-package.md#step-2-how-it-connects-and-how-much-it-needs)):
  1. The vendor's brand, press or media kit (search for "<vendor> brand assets" or "<vendor> press kit"). It has the official artwork and colours, and says how the logo may be used.
  2. Otherwise [Simple Icons](https://simpleicons.org), which has a single-colour 24×24 SVG for thousands of brands at `https://cdn.jsdelivr.net/npm/simple-icons/icons/<slug>.svg`. Its files are CC0, but the logos are still the vendors' trademarks, so check the brand guidelines it links to.

  Prefer the square mark (the symbol on its own) over a wordmark. Icons are drawn in a small square, so a wide logo with the name spelled out shrinks until it's hard to read. If the only SVG is a wordmark, say so when offering it, and offer a built-in icon alongside it. Download the SVG rather than redrawing or tracing it, and note its source URL in a comment at the top of the `.icon.js` file (as Mastodon's does). Tell the user where it came from, and that it's the vendor's trademark, used only to identify the service. If there's no usable SVG (only a PNG, or the guidelines forbid this use), use a built-in icon instead and say why.
- **Everything else**: utility packages, and actions or triggers with no vendor behind them (Test Connection, an Umbraco-side trigger, a text formatter, the httpbin examples). Use a built-in Umbraco icon, such as `icon-link`, `icon-paper-plane`, `icon-message` or `icon-calendar`, from the backoffice icon picker. It needs no files and nothing to maintain.
- **The contributor's own icon**: whatever the package, if they have an SVG they'd rather use, use it. Prepare it the same way as a downloaded logo.

Preparing a logo:
- Name it `icon-automate-<area>`, and register it as in [full.md](references/full.md#icons-and-umbraco-packagejson). A logo only adds those icon files, a `wwwroot/` and the Razor SDK; it doesn't make a package Full.
- Strip anything the backoffice doesn't need: `<title>`, `<script>`, editor metadata (Inkscape, Figma or Sketch attributes) and comments.
- Prefix every `id` inside the SVG (gradients, clip paths) with the area name, because icons are inlined into the backoffice page and another icon's identical id can hijack it.
- Replace any `<style>` block with attributes on the elements (e.g. `.cls-1{fill:#3761a8}` becomes `fill="#3761a8"` on each element with that class), then remove the `class` attributes. Inlined into the page, the styles would apply to every element with those class names, restyling other icons; Illustrator exports (`cls-1`, `cls-2`...) all use the same names. PokeApi's logo is an example.
- A single-colour SVG (Simple Icons has no `fill`) needs `fill="currentColor"` on the `<svg>`, so it follows the backoffice theme instead of showing black on a dark background. Keep a full-colour vendor logo in its own colours.
- Add the icon test from [full.md](references/full.md#tests): a wrong path or name just shows a blank icon.

### Settings fields
Pick the right editor for every setting rather than leaving everything a text box: **read [references/fields.md](references/fields.md) whenever you write a settings class.** It lists Umbraco's editors, their `EditorConfig`, the C# type each binds to, and examples. In short:
- Use `[Field(Label = ..., Description = ..., SortOrder = ...)]` on every setting; the description is the user's only help text, so say what the value is, its range or format, and the default, briefly (see [Keep names and help text short](#keep-names-and-help-text-short)).
- **Could the value come from the trigger or an earlier step?** Keep it text with `SupportsBindings = true` (titles, messages, IDs, URLs). Bindings are text, so they can't go in a dropdown, toggle or number input.
- **One of a fixed set?** `Umb.PropertyEditorUi.Dropdown`, or `RadioButtonList` for two to five options, bound to `string` (or `CheckBoxList` and `List<string>` for several). Use `{ "name": "Friendly label", "value": "STORED_VALUE" }` items when the service's values aren't readable; the stored value can never change once released.
- **Longer text?** `Umb.PropertyEditorUi.TextArea` with `rows`. **A number?** `Integer` or `Decimal` with `min`/`max`, plus `[Range]`. `bool` gets a toggle and `DateTime` a date picker automatically. Media folders and items use `MediaPicker`; content, media type and member group filters use the type pickers.
- Give every option a default in the property initializer, so a new step works without changes.
- Use data annotations (`[Required]`, `[Range]`, `[StringLength]`) for single-field rules: Automate checks them before `ExecuteAsync` and fails the step as `Validation`.
- **Optional text must be `string?`.** Automate treats every non-nullable `string` setting as `[Required]`, and an empty string fails it, so `public string Range { get; set; } = string.Empty;` fails the step when left blank, even if its description says it's optional. Required text is `string`; optional text is `string?`, checked with `string.IsNullOrWhiteSpace`.
- **Connection settings with no default.** When a step leaves Automate to pick its connection, Automate loads and validates every connection in the workspace before choosing one of the right type. So a single connection missing a required value fails those steps in every automation, even other packages' steps (*Failed to resolve model 'community.skoda'... The VIN field is required*). The backoffice won't save a connection like that, but code can create one: the Demo fills such settings with a placeholder (`Setup/DemoSetup.cs`). Give connection settings a default where there's a sensible one, and make the rest `string?` if they're genuinely optional.
- Every field needs a `Label` and a `Description`: without them the backoffice shows a raw `#uaFields_...` localization key. A `Group` other than `Advanced` needs a localization file for its heading (see DevTo), so prefer `Advanced`.
- Mark credentials `IsSensitive = true`, and put rarely-changed options in `Group = "Advanced"`.
- Build a custom editor in `Client/` only when no built-in editor can express the value (Google Sheets' column list is the model).

### Configuration and secrets
- Never commit real credentials, not even temporarily. The repo's gitleaks hooks (`.githooks/`) block commits and pushes that contain them.
- Credentials live in configuration, not the database, under Umbraco Automate's **shared** sections, nested under the area name: `Umbraco:Automate:Secrets:<Area>:<Key>` for sensitive values and `Umbraco:Automate:Variables:<Area>:<Key>` for the rest, referenced from fields as `$Umbraco:Automate:Secrets:<Area>:ApiKey`. Automate resolves references from these two sections by default (its allow-list is default-deny for everything else), so **don't register anything** in the composer. Nesting under `<Area>` keeps a package's keys together and avoids clashes. Secrets can only be referenced from `IsSensitive` fields. Keep the paths, and each default reference, as constants in `Configuration/<Area>Configuration.cs`, and use them everywhere the path appears (settings defaults, validator messages), so there's a single place to change them.
- **Pre-fill each credential field with its reference as the default value** (`public string ApiKey { get; set; } = <Area>Configuration.ApiKeyReference;`). A new connection then opens with the reference already filled in, so users who store the key in configuration don't type anything, and anyone who prefers can overwrite it with the value. Don't ask users to copy and paste references from help text, and don't repeat the reference path in the field's description either: the pre-filled value already shows it, so the description only needs to say what the value is and where to get it. Leave per-connection values (like a vehicle VIN) blank.
- Automate resolves references before settings reach your code. If the key is missing it fails the step or the **Test connection** itself, with *Configuration key '...' not found*, so you don't need to handle that. As a cheap safety net, the shared validator can still reject a value that starts with `$` (one that arrived unresolved) with a message naming the key, before any API call.
- Values that are the same for every site, such as the service's API base URL, a default instance URL (DevTo) or an OAuth scope (Google Sheets), are constants in `<Area>Configuration` too, not settings in `appsettings.json`: nobody needs to change them per site, but keeping them in that class means every fixed fact about the service is in one place, and a value used in several files (Google Sheets' API URL, used by every action) is defined once. Implementation details such as page sizes and timeouts stay private in the class that uses them, and a field's default value (e.g. `= 80`) stays on the field. If a package genuinely needs an options class, bind it to the same `Umbraco:Automate:Variables:<Area>` section (`<Area>Configuration.VariablesPath`) so all its configuration lives together.
- The one exception: OAuth providers use `Umbraco:Automate:Providers:<Area>`, which is where Umbraco Automate's OAuth support expects them (see Google Sheets).

### Actions
- Return `Success()` or `Success(output)` on success. Use `SuccessWithOutcome("found", output)`-style outcomes when later steps should branch (e.g. `created`/`updated`, `found`/`notFound`), and document each outcome.
- Return a typed output (`ActionBase<TSettings, TOutput>`) when the action produces data later steps need. Output properties are exposed in camelCase as `${ steps.<alias>.<property> }`.
- Fail with `ActionResult.Failed(exception, StepRunErrorCategory.X)` and pick the category deliberately, because Automate uses it to decide whether to retry: `Validation` and `ConfigurationError` for problems the user must fix, `Authentication` for 401/403, `RateLimiting` for 429, `Timeout`, `ServiceUnavailable` for 5xx, `InvalidResponse` for anything else unexpected. Map HTTP status codes in one place in `Api/` (DevTo's `Classify` method is the model).
- Validate the connection and settings first and fail fast with a clear message telling the user what to fix.
- Send an idempotency key where the service supports one (`$"{context.RunId}:{context.StepId}"`, as Mastodon does), so a retried step doesn't post twice. Many services don't support one (DevTo's Forem API, Google Sheets, Pushover): there, a step retried after a timeout may repeat something that actually succeeded, such as appending a row twice. Prefer an upsert-style action where the API allows one (Google Sheets' *Append or Update Row*), and say so in the README.
- Treat `TaskCanceledException` when the caller didn't cancel as a timeout, and `HttpRequestException` as the service being unreachable.
- Log with structured placeholders, never secrets.

### Triggers
- The simplest trigger is a **notification trigger**: derive from `NotificationTriggerBase<TSettings, TOutput, TNotification>` (note the order: settings, output, then the Umbraco notification) and put `[Trigger("<area>.<name>", "Display Name", Group = ..., Icon = ...)]` on it. See `Packages/_Development/KitchenSink/.../Triggers/`.
- Override `MapEvent(notification)` to return one `TriggerEvent<TOutput>` per affected item, with `TriggerAlias = Alias`, the output, and `IdempotencyKey = GenerateIdempotencyKey(item.Key, 0, item.UpdateDate)` so a duplicate notification doesn't run the automation twice.
- Override `CanHandle(output, settings)` (it's `protected`, and `settings` can be null) to apply each automation's own filters, such as a key prefix.
- Automate discovers the trigger from its attribute and subscribes to the notification for you; there's nothing to register. Output properties reach steps in camelCase as `${ trigger.<property> }`.
- Other kinds exist (`ScheduledTriggerBase` for CRON, `WebhookTriggerBase`, or raising events yourself with `ITriggerDispatcher`), but Automate supplies their output itself; reach for them only when a notification trigger can't do the job.

### Connection validation
- `ValidateAsync` should check required fields, then make one cheap authenticated call and return `Success("Connected as @name")`-style feedback.
- If each check costs the user API quota, make the live check opt-in (see Skoda's `ValidateConnection`).

## Testing

- **xUnit only**: use xUnit's `Assert`, and don't add assertion or mocking libraries (no Shouldly, FluentAssertions, Moq or NSubstitute). One framework keeps tests readable for every contributor and the dependency list short. Some older packages still use Shouldly or Moq; don't copy that into new code.
- Umbraco's own `Umbraco.Automate.Testing` package is fine (it's the official harness, not a mocking library): `ActionTestHarness.For<TAction>().WithService(...).WithSettings(...).WithConnection(alias, settings).ExecuteAsync()` runs an action end to end and returns its `ActionResult`.
- Instead of a mocking library, write small hand-written fakes: a stub `HttpMessageHandler` that returns canned responses and records requests, behind a tiny `IHttpClientFactory` implementation (see `Fakes/StubHttp.cs` in either reference).
- To unit test a connection type's `ValidateAsync`, construct it with `new ConnectionTypeInfrastructure(fakeResolver)`, where the fake implements `IEditableModelResolver` with explicit interface members that throw (validation never uses it). Explicit implementation avoids having to repeat the interface's generic constraint.
- Test each action's success path, each outcome, missing or invalid settings (including an unresolved `$` reference), and how API errors map to categories. Never call the real service.
- Every package has an `<Area>FieldTests` test checking that each setting has a `Label` and `Description` (see the templates), and a package with custom icons has an icon test. Both catch mistakes the backoffice shows silently.
- Group tests in the same folders as the package (`Actions/`, `Connections/`, `Api/`).
- If the connection has a `Client/` front end, add web-test-runner unit tests, and Playwright specs if it needs end-to-end coverage (see Google Sheets).

## README

The package README ships inside the NuGet package and is what nuget.org and the Umbraco Marketplace show, so write it for site developers installing the package. Sections, in order: one-line summary; **Installation** (`dotnet add package`); **Setup** (getting credentials from the service; storing them under `Umbraco:Automate:Secrets:<Area>` / `Umbraco:Automate:Variables:<Area>` with an appsettings example and the environment-variable equivalents, e.g. `Umbraco__Automate__Secrets__<Area>__ApiKey`; creating the connection under **Automation → Settings → Connections** and noting the fields come pre-filled with references); **Actions** (a table of every setting); **Outcomes and outputs**; **Troubleshooting** (the error messages users will actually see, including Automate's *Configuration key '...' not found* for a missing key); **Compatibility**; **Links**. Use absolute URLs for anything outside the package folder, because relative links break on nuget.org.

## Packages without a provider

Triggers and actions with no external service (extra content steps, text and date helpers) are packages in `Packages/` like the rest; there are no top-level folders per kind, because the unit people install and release is a package and most contain several kinds.

- Name the package after its purpose (`Packages/ContentTools/`, `Packages/Text/`) and keep it to one purpose. Never create a catch-all (`Common`, `Utilities`): users would install every step to get one, and every change would release them all.
- Before building a general-purpose step, check it isn't already in Umbraco Automate or in an existing package here; if a step fits an existing general package, add it there.
- Don't create a shared library between packages; duplicate small helpers instead.

Umbraco Automate 17 already includes these, so check the list before building something general-purpose:

| | Built in |
|---|---|
| Content | Triggers: Content Published, Saved and Unpublished (and their batch versions). Actions: Find Content, Get Content, Get Content Property, Update Content Property, Publish Content, Unpublish Content, Notify Editor |
| Media | Triggers: Media Saved, Deleted and Trashed (and batch versions). Action: Update Media Property |
| Members and users | Triggers: Member Saved and Deleted; User Saved, Deleted, Locked, Login Success, Login Failed and Password Changed |
| General | Triggers: Manual, Scheduled, Webhook. Actions: HTTP Request, Send Email, Log Message, Delay, Request Approval, Set Variable, plus the If, Switch, While and For Each steps |

## What a change may touch

Packages are independent: work on one package must never change how another builds, behaves or is released. When creating or changing a package, only change:

- everything under its own `Packages/<Area>/` folder;
- its two lines in `Umbraco.Community.Automate.Demo.slnx`, and its `ProjectReference` in `Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj`;
- its configuration keys, under its own name, in `Umbraco.Community.Automate.Demo/appsettings.Development.json` (placeholders) and `appsettings.Local.example.json` (the template for real values);
- its test page's three new files in `Umbraco.Community.Automate.Demo/uSync/v17/`, its line in `ContentTypes/automatetests.config` and its connection's line in `Automate-Workspaces/demo.config`;
- its stub keys in the contributor's own git-ignored `appsettings.Local.json`;
- **new** entries in `Directory.Packages.props` for libraries no package uses yet;
- its row in the root `README.md`, its line in `.github/CODEOWNERS`, its entry in `.github/ISSUE_TEMPLATE/bug-report.yml`, and its `wwwroot/` line in `.gitignore` if it has a `Client/`.

Don't change other packages, anything in `Packages/_Development/` (the examples and Test Connection), `.github/workflows/`, `.githooks/`, root build or editor settings, this skill, or the version of a library other packages already use. If one of those is genuinely necessary (the package can't build without it), stop and tell the user what and why, and suggest a separate issue or pull request, rather than changing it as part of the package. The exception is when the user explicitly asks for a repo-wide change.

## Adding to an existing connection

- **New action**: add it to `Actions/` with its settings and output (Automate discovers it from its `[Action]` attribute), add tests, and document it in the package README. One action per PR is the norm.
- **New trigger**: same, in `Triggers/`.
- Keep existing aliases, namespaces and public types stable. They're used by people's saved automations and code.

## Migrating an existing package

To bring in a package that lives in its own repo (as WeatherApi did from `SA.Automate.WeatherApi`), or to bring a package already here up to the current conventions, follow [references/migrating.md](references/migrating.md). The essentials:

- Work from the source's latest default branch, and inventory it against these conventions before changing anything.
- Ask first whether it adopts the community naming (recommended) or keeps the author's own package ID, namespaces and aliases. Either way it follows every other convention.
- If the package has been released, ask before renaming aliases, moving configuration or changing the package ID, because saved automations depend on them, and document every such change in a **Migrating from <old package>** README section (see the Mastodon and WeatherApi READMEs).
- Reshape it to the layout and conventions here, keep its behaviour, keep its tests passing, and drop what the repo handles centrally (release workflows, `umbraco-marketplace.json`, `NuGet.config`, licence and icon copies).

## Working notes

This skill is a living document. Add new conventions and lessons learned here (or in the sections above) as the team agrees on them.

- Front-end packages pin Playwright to the version Google Sheets uses (`"overrides": { "playwright": "1.61.0", "playwright-core": "1.61.0" }` in `Client/package.json`), so every `Client/` shares one downloaded browser.
- In a package with a `Client/`, `Client/public/` is copied unchanged into `wwwroot/` by Vite, so `umbraco-package.json` and `icons/` live there and the manifest adds one `bundle` extension for whatever Vite builds (`Client/src/manifests.ts`). Hand-written editors without a build step go straight in `umbraco-package.json` (DevTo).
- Automate discovers `[ConnectionType]`, `[Action]` and `[Trigger]` classes on its own (checked in the Demo site: with the Kitchen Sink's registrations removed, its trigger, actions and connection type all still appear). So composers register services only; registering the types as well is redundant.
- .NET accepts any well-formed culture name (e.g. `xx-YY`), so `CultureInfo.GetCultureInfo` only throws for malformed input. Don't rely on it to reject unknown cultures.
