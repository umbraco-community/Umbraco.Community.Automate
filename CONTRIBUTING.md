# Contributing

Thanks for your interest in contributing to Umbraco.Community.Automate! This is a monorepo of community-maintained provider packages for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate), each contributing connections, triggers, and/or actions.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22.x](https://nodejs.org/) — only needed for packages with a backoffice `Client/` folder (custom property editors, etc.)

## Project layout

Each provider package lives under its own top-level folder (e.g. `GoogleSheets/`, containing `Umbraco.Community.Automate.GoogleSheets` and its matching `*.Tests` project) — this groups a provider's package, tests, and any `Client/` frontend together as more providers are added. `Umbraco.Community.Automate.Demo` is a throwaway Umbraco site used to manually exercise every package end-to-end, and also hosts the Playwright E2E suite.

## Building and testing

```bash
dotnet build
dotnet test
```

Run a single package's tests directly, e.g.:

```bash
dotnet test GoogleSheets/Umbraco.Community.Automate.GoogleSheets.Tests
```

If a package has a `Client/` folder, run its frontend unit tests with `npm test` from that folder (`npm ci` first). CI also runs a Playwright E2E pass against the Demo site — see `.github/workflows/ci.yml` for the exact steps if you need to reproduce it locally.

## Making a change

1. Branch off `main` — branch names follow `<type>/<short-description>`, e.g. `feat/google-sheets-clear-range`, `fix/oauth-token-refresh`, `docs/readme-cleanup`.
2. Commit messages follow `type(scope): Description`, e.g. `add(action): AppendRowAction — append a row of values to a sheet`, `fix(action): require LookupValue in UpdateRowAction`, `test(action): FindRowActionTests — found/notFound outcomes`. Common types: `add`, `fix`, `refactor`, `test`, `docs`, `chore`.
3. Open a PR against `main`. CI runs backend/frontend unit tests and the Playwright E2E suite — all three must pass before merge.
4. Keep PRs scoped to one action/feature where practical; a new action typically lands as its own PR with its own tests.

## Preventing secret leaks

Never put real credentials (OAuth Client IDs/Secrets, API keys, etc.) into a git-tracked file like `appsettings.Development.json` — even locally, even temporarily. Use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) instead, which stores them outside the repo entirely:

```bash
dotnet user-secrets set "Umbraco:Automate:Providers:GoogleSheets:ClientId" "<your-real-client-id>" --project Umbraco.Community.Automate.Demo
dotnet user-secrets set "Umbraco:Automate:Providers:GoogleSheets:ClientSecret" "<your-real-client-secret>" --project Umbraco.Community.Automate.Demo
```

These values transparently override the tracked placeholder values in `appsettings.Development.json` at runtime when running the Demo site in Development — no code changes needed, and nothing about them ever touches git.

### One-time setup

Run the setup script for your platform once per clone:

```bash
./SetupRepo.sh        # macOS/Linux
```

```powershell
.\SetupRepo.ps1       # Windows
```

This installs [Lefthook](https://github.com/evilmartians/lefthook) and wires up `pre-commit`/`pre-push` git hooks that run [gitleaks](https://github.com/gitleaks/gitleaks) against your changes automatically — a second, local layer of defense in case a real secret ever ends up staged despite the above.

### If a commit or push is blocked

If gitleaks finds something that looks like a secret, your commit or push will fail with output identifying the file and line. To resolve it:

1. Unstage or remove the offending change (`git restore --staged <file>` or edit the file to remove the real value).
2. If it's a real credential you need locally, set it via `dotnet user-secrets set` instead (see above).
3. Re-commit/re-push.

## Releasing a package

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

## Supporting multiple Umbraco versions

This package depends on `Umbraco.Automate`/`Umbraco.Automate.Core`/`Umbraco.Automate.OpenIddict` directly — never on `Umbraco.Cms` itself, which is a transitive dependency pulled in through `Umbraco.Automate`. Compatibility is tracked against `Umbraco.Automate`'s own version, not Umbraco.Cms's: `Umbraco.Automate` already aligns its own major version number with the Umbraco.Cms major it targets (e.g. `17.0.0` is built for Umbraco 17), so a provider package here inherits that signal for free without needing to duplicate a Umbraco.Cms compatibility claim of its own.

### Two independent version numbers

A package has two things that can each change independently:

1. **Its own SemVer** (`major.minor.patch`) — the package's own public API/behavior contract.
2. **Its declared `Umbraco.Automate` compatibility range** — expressed as the `Umbraco.Automate`/`.Core`/`.OpenIddict` `PackageReference` version range in `Directory.Packages.props` (or a package-specific `VersionOverride` if it needs to diverge from the repo-wide default — see NuGet's [Central Package Management docs](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management#overriding-package-versions)), and documented in the package's own README.

These deliberately don't move together, and a package's own major version does **not** mirror `Umbraco.Automate`'s major number. If it did, every `Umbraco.Automate` major release would force a matching package release just to keep the numbers in sync — even when nothing in the package actually needed to change. The default expectation: a new `Umbraco.Automate` version with zero impact on a package means only a documentation update (widen the range), not a new release.

### Widening the range only after verifying it

A NuGet version range like `[17.0.0, 18.0.0)` resolves to its *lowest* satisfying version during restore/build, not the newest one inside the range — widening the range's ceiling doesn't make CI start testing against it automatically. So when a new `Umbraco.Automate` version ships: build and run the full test suite against it first (locally, or via a one-off local `VersionOverride` bump), and only widen the declared range once that passes. Every version inside a declared range should have been verified at least once at the point it was added.

This doesn't catch everything: a *later* patch release inside an already-widened range can still break something nobody explicitly re-tested against (see the worked example below). That's a real, accepted residual risk — continuously re-testing against every new `Umbraco.Automate` patch isn't worth the process overhead here. The defensive patch practice below is how that risk gets handled when it actually materializes, not a way to prevent it.

### When to fork: one build can no longer serve the whole range

Bumping a package's own major version and creating a maintenance branch is only warranted when **one build can no longer serve the whole currently-declared range**. Three different things can cause that, and all three are handled the same way:

- A new `Umbraco.Automate` major breaks something.
- A breaking change lands mid-minor, inside an already-supported range.
- The package wants to adopt a new `Umbraco.Automate`/Umbraco capability (backend or client-side/npm) that doesn't exist across the whole currently-declared range.

Before forking, always ask: **can one build still serve the whole range?** (Runtime/compile-time feature detection, a conditional code path, a try/fallback — backend: reflection or a version check; client-side/npm: check the API exists before using it, e.g. `typeof someNewThing !== 'undefined'`, and gracefully degrade on older versions.) If yes, do that instead — ship a normal patch or minor release, keep one range, no fork. Reach for a fork only when the change is central enough that dual-path logic genuinely isn't worth maintaining. This is a per-change judgment call, not a fixed rule.

(True multi-targeting — one package with conditionally-compiled code per `Umbraco.Automate` version — is deliberately not used here. It adds real, permanent build complexity for a benefit almost always achievable more cheaply via feature detection.)

### If a break is discovered: patch the range defensively, first

The moment a break is discovered in a version inside the currently-declared range — CI failure, bug report, manual testing — the first move, before any real fix exists, is a low-ceremony patch release of the *current* line that narrows the range's ceiling to exclude the broken version. This stops NuGet from letting a new install on the broken version pull in a package release that's already known not to work for it. Do this regardless of whether the eventual fix turns out to need a fork or not — it's a protective measure, not a decision about the fix.

### Maintenance branches

When a fork is actually needed:

- **Naming:** `<package-slug>-v<major>` (e.g. `googlesheets-v1`) — matches the existing tag prefix convention, not a specific `Umbraco.Automate`/Umbraco version, since one package major can span multiple `Umbraco.Automate` majors under the range-widening policy above.
- **Creation:** cut from the last tag that was genuinely still compatible with the full old range, *before* any defensive narrowing or adaptation commits. `main` moves forward with the new floor and the new package major; the maintenance branch keeps the old, narrower range.
- **Patching the old line:** a short-lived branch off the maintenance branch → PR *targets the maintenance branch*, not `main` → merge → tag a patch release from the maintenance branch's HEAD (e.g. `googlesheets-v1.4.3`) → push the tag. Tags aren't branch-scoped, so the release workflow fires exactly as it does from `main`.
- **Cross-porting:** if a maintenance-branch fix also matters for current `main`, that's a separate, manual cherry-pick PR — not automatic.
- **Retirement:** maintenance branches are never deleted. A line that stops getting patches just goes inert; it stays as an accurate historical record and can resume receiving patches later without recreating anything.

### Worked example: a hypothetical `Umbraco.Automate` 18.2 break

Say a package currently declares `Umbraco.Automate` support as `[17.0.0, 19.0.0)` (verified against 17.0.0 and 18.0.0/18.1.0 at various points), and is on package version `1.6.0`.

1. `Umbraco.Automate` 18.2.0 ships. A bug report comes in: something that worked on 18.1.0 and earlier now throws on 18.2.0.
2. **First move — defensive patch:** narrow the range in `Directory.Packages.props` from `[17.0.0, 19.0.0)` to `[17.0.0, 18.2.0)`, bump the package to `1.6.1`, tag `googlesheets-v1.6.1`, push. This ships immediately, before any real fix exists — it just stops new installs on 18.2.0+ from pulling in a package version already known not to work for them.
3. **Decide: fork or not?** Investigate the break. If it's fixable with a version check or a try/fallback that still works on 17.x–18.1.x too: fix it, restore the range to `[17.0.0, 19.0.0)`, ship `1.6.2`. Done — no fork.
4. **If it's not fixable in one build** (say 18.2.0 removed something the code genuinely needs, with no compatible shim): this is the fork point.
   - Cut `googlesheets-v1` from the `googlesheets-v1.6.0` tag (the last version that was genuinely 17.x–18.1.x-compatible, *before* the defensive narrowing in step 2).
   - On `main`, adapt the code for 18.2.0's change, set the range to `[18.2.0, 19.0.0)`, and release this as `2.0.0` — the package major bump reflects that the compatibility contract genuinely changed.
   - `googlesheets-v1` keeps declaring `[17.0.0, 18.2.0)` (from the defensive patch in step 2) and can still receive its own patches (tagged `googlesheets-v1.6.x`) if something else needs fixing for 17.x–18.1.x users, independently of whatever happens on `main` going forward.
5. A month later, someone reports a separate, unrelated bug that also affects `googlesheets-v1` users. Fix it on a branch off `googlesheets-v1`, PR against `googlesheets-v1`, tag `googlesheets-v1.6.3`. Since the same bug also exists in `main`'s `2.x` code, that's a second, separate cherry-pick PR into `main`.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
