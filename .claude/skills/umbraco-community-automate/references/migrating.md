# Migrating an existing package into this repo

Many Automate packages start life in their author's own repository (like `SA.Automate.WeatherApi` or `OC.Automate.Mastodon`) and later move here. Migrating means more than copying files: the package is reshaped to this repo's layout and conventions, so it sits alongside the others and contributors can find their way around it. As the conventions change, packages already here are brought up to date the same way, so this process is also how to update an older package.

Like building a new package, take the user through choices before changing anything ([new-package.md](new-package.md#how-to-ask) explains how to ask). The decisions that matter most are the ones that affect people already using the package.

## Step 0: Read the source (before asking anything)

- **Get the latest code.** Ask where it lives if you weren't told (a local path or a git URL). Fetch and work from the default branch's latest commit (`origin/main`), not a possibly stale local checkout, and note the commit you migrated from.
- **Inventory it.** For each part, note what it is and where it will go:
  - Connection type and settings, actions, triggers, their aliases and display names.
  - Configuration: which keys it reads, from where (`appsettings` sections, `$` references), and whether anything is registered in the composer.
  - API client, models, error handling, retries.
  - Front end: icons, custom editors, how they're registered (`umbraco-package.json`, an `IPackageManifestReader`, a `Client/` build).
  - Tests and test libraries.
  - Packaging: package ID, version, NuGet metadata, marketplace files, release workflow, licence.
- **Check whether it's been released.** Look it up on NuGet (`https://www.nuget.org/packages/<PackageId>`). A released package has users whose saved connections and automations store its aliases and settings, which changes how careful the migration must be.
- **Compare it with this repo's conventions** (SKILL.md, [fields.md](fields.md)) and list every difference: folder layout, aliases, configuration paths, settings editors, error categories, icons, tests.

Present the inventory and the list of differences briefly, then ask.

## Step 1: Identity and scope

Ask, in one round:

1. **Identity: community naming or the author's own?** Ask this first: it decides most of what follows. A migrated package can either adopt this repo's identity (package ID `Umbraco.Community.Automate.<Area>`, namespaces `Umbraco.Community.Automate.<Area>.*`, aliases `community.<area>...`) or keep its own "signature" (e.g. `SA.Automate.Pushover`, `SA.Automate.Pushover.*`, `pushover...`). Options:
   - **"Adopt the community naming (Recommended)"**: it matches every other package, so contributors and users find it the same way, and it's published and maintained as part of the community set.
   - **"Keep my own package ID, namespaces and aliases"**: for compatibility. Existing users upgrade in place with no reinstall, no code changes and no broken automations, and the author's name stays on the package. The only cost is that its names differ from the other packages'.

   Recommend the community naming, but present keeping their own as a fully accepted choice, not a fallback to talk them out of: it's the right call when people already depend on the package and a rename would break their sites or automations. Don't push back once they've chosen. If they keep it, everything else still follows the conventions (layout, folders, configuration class, error categories, settings editors, tests, wiring); only the names stay theirs, and Step 2's alias and package ID questions fall away. The folder is still `Packages/<Area>/`, and the tag prefix is still `<area>-v`.
2. **How far to go**: "Full migration to the conventions (Recommended)", or "Move it as it is first, align it in a follow-up". Recommend full unless the package is large and the user wants a quick first PR.
3. **Area name**: propose one (the service name, PascalCase), which sets `Packages/<Area>/` and the tag prefix (and, with the community naming, the package ID `Umbraco.Community.Automate.<Area>`).
4. **Credits**: who goes in `<Authors>` and in `CODEOWNERS` (default: the original author, plus "Umbraco Community").

## Step 2: Decisions that affect existing users

Only ask these if the package has been released and is adopting the community naming. If it hasn't been released, apply the conventions and say so. If it keeps its own identity, the aliases and package ID stay as they are; only ask about the configuration path.

1. **Aliases**: saved connections and automations store the connection type and action aliases, so renaming them to `community.<area>` breaks every existing automation. Options: "Adopt `community.<area>` and document the breaking change (Recommended)", or "Keep the old aliases". (WeatherApi and Mastodon adopted the new aliases; their READMEs explain the move.)
2. **Configuration path**: if the package read credentials from somewhere else (WeatherApi used `Umbraco:Automate:Secrets:WeatherApiKey`), moving to `Umbraco:Automate:Secrets:<Area>:<Key>` only changes the default for new connections; existing connections keep their saved values or references. Options: "Move to the shared section and document it (Recommended)", "Keep the old path".
3. **Package ID**: the new ID is `Umbraco.Community.Automate.<Area>`. Ask whether the author will deprecate the old NuGet package and point it at the new one (Recommended: deprecate with a message linking the new package).

## Step 3: What to change

Present the planned changes as a checklist and ask "Make these changes (Recommended)" or "Change the plan" (multiSelect if they want to drop some). The usual list:

- **Layout**: move into `Packages/<Area>/Umbraco.Community.Automate.<Area>/` and `.Tests/`, with the standard folders (`Actions/`, `Connections/`, `Composers/`, `Configuration/`, plus `Api/`, `Models/`, `Triggers/`, `Client/`, `wwwroot/` if needed). Namespaces follow the folders.
- **Aliases** prefixed `community.`, written directly on the attributes (no constants classes), if chosen in Step 2.
- **Configuration**: an `<Area>Configuration` class with the config paths, default references and fixed values (base URL); credentials default to their `$Umbraco:Automate:Secrets:<Area>:<Key>` reference; nothing registered in the composer for configuration.
- **Composer**: services only. Remove any registration of the connection type, actions or triggers (e.g. `builder.ConnectionTypes().Add<...>()`, `.Actions().Append<...>()`, a `WithCollectionBuilder` call): Automate discovers them from their `[ConnectionType]`, `[Action]` and `[Trigger]` attributes, so check every one of those classes has its attribute before deleting the registration.
- **Settings editors**: each setting reviewed against [fields.md](fields.md): fixed choices become dropdowns or radio lists, messages text areas, numbers get limits and `[Range]`, credentials `IsSensitive`.
- **Errors**: failures return `ActionResult.Failed(..., StepRunErrorCategory.X)` with deliberate categories, mapped in one place.
- **Icons**: keep the package's own icon, moved to static `umbraco-package.json` + `icons/` files (replacing any `IPackageManifestReader`). If it has none, follow Icons in SKILL.md: the vendor's logo for a commercial service, a built-in icon for a utility.
- **Packaging**: central package versions in `Directory.Packages.props` (no versions in the `.csproj`), a `Directory.Build.props` with `MinVerTagPrefix`, and the repo's README/icon packing. Remove what the repo handles centrally: release workflows, `umbraco-marketplace.json`, `NuGet.config`, separate licence and icon copies.
- **Tests**: keep the existing tests passing. New tests use xUnit's `Assert` and hand-written fakes; converting existing Shouldly or Moq tests is optional and the user's call (ask).
- **Wiring**: solution, Demo site reference, the package's configuration keys in the Demo's `appsettings.Development.json` (placeholders) and `appsettings.Local.example.json` (template for real values), root README table, `CODEOWNERS`, the bug report's package list.
- **README**: rewrite to the repo's structure (see SKILL.md), with a **Migrating from <old package>** section if it was released: the `dotnet remove package <OldId>` / `dotnet add package Umbraco.Community.Automate.<Area>` commands, what moved (aliases, configuration keys) and what users must do. Describe changes against the old package only, not intermediate states.

## Step 4: Migrate, then verify

- Make the changes, touching only the package's own folder and its wiring ("What a change may touch" in SKILL.md), and preserving behaviour: the package should do exactly what it did before, apart from the decisions above. Where behaviour changes on purpose, note it in the README's migration section.
- Bring the history across only if the user wants it (ask; `git subtree` or a fresh copy noting the source commit in the PR description are both fine).
- Verify: `dotnet build`, `dotnet test`, `dotnet pack` (check the front end lands in `staticwebassets/` if there is one), then run the Demo site and check the connection type, actions and icon appear and **Test connection** works with placeholder values.
- Report what changed, anything you couldn't migrate or verify, and the follow-ups for the author (deprecate the old package, update links, tell users).

## Updating a package that's already here

When the conventions change (a new rule in SKILL.md or fields.md), packages in `Packages/` are brought up to date with Step 0's comparison and Step 3's checklist, limited to what changed. Released packages still get Step 2's care: never rename an alias or change a stored setting value without asking and documenting it.
