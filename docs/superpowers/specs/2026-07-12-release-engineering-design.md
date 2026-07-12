# Versioning & Release Pipeline Design

## Context

All 13 Google Sheets provider PRs are merged to `main` and the package is feature-complete for a 1.0.0 release, but there is currently no way to actually ship it: no version number is pinned anywhere in the repo (no `Directory.Build.props` `<Version>`, no csproj `<Version>`), there is no CHANGELOG, and the only GitHub Actions workflow (`.github/workflows/ci.yml`) runs tests on PRs — there is no publish or release workflow at all.

This repo is also intentionally a **monorepo** that will hold more than one Umbraco Automate provider package over time (Google Sheets today; Google Drive, Google Docs, or others later, per hints already in the GoogleSheets README about future OpenIddict provider registrations). Any release mechanism must let a single package ship a new version **without** forcing every other package in the repo to release at the same time, and must scale to N future packages without requiring new workflow files per package.

This design covers the versioning scheme and the release pipeline only. Three related but independent pieces of work were explicitly decomposed out and deferred to their own future brainstorm/plan cycles:
- An Astro + Starlight documentation site covering the monorepo's multiple packages.
- A Claude Code skill to help automate/drive the release flow (this depends on the pipeline in this design existing first).
- An automated changelog-bot workflow (e.g. release-please-style) — explicitly rejected in favor of GitHub's built-in auto-generated release notes, to avoid an added bot dependency and to keep "when a release happens" a deliberate, human-initiated action rather than something a bot decides via auto-opened PRs.

## Versioning — MinVer, per package

Each provider package gets a [MinVer](https://github.com/adamralph/minver) package reference added to its own `Directory.Build.props` (this file already exists per-package today — e.g. `Umbraco.Community.Automate.GoogleSheets/Directory.Build.props` — and MSBuild's `Directory.Build.props` cascade means it only governs that package's own project tree, not the whole repo):

```xml
<PropertyGroup>
  <!-- Tags this repo uses for GoogleSheets releases, e.g. googlesheets-v1.0.0.
       IgnoreHeight: this package is only ever versioned via a deliberate tag
       push, never by auto-incrementing between tags, so MinVer's commit-
       height-based pre-release suffixing — which would also react to
       unrelated packages' commits in this monorepo's shared history — is
       turned off. See MinVer's own README FAQ: "Independent versioning of
       multiple projects within a single repository is achievable by using
       distinct tag prefixes... mitigated by ignoring height." -->
  <MinVerTagPrefix>googlesheets-v</MinVerTagPrefix>
  <MinVerIgnoreHeight>true</MinVerIgnoreHeight>
</PropertyGroup>
```

No `<Version>` property is ever hand-maintained anywhere. The git tag *is* the version. The tag prefix (`<package-slug>-v`) is the mechanism that scopes each package to its own private slice of the repo's shared tag namespace — MinVer building GoogleSheets ignores a `googledrive-v1.0.0` tag entirely, even if it's more recent in commit history, because it doesn't match GoogleSheets' configured prefix.

Package slug convention: lowercase, strip the common `Umbraco.Community.Automate.` prefix from the folder name (e.g. `Umbraco.Community.Automate.GoogleSheets` → `googlesheets`).

## Release trigger

Pushing a tag matching `<package-slug>-v<semver>` (e.g. `googlesheets-v1.0.0`) to the real `umbraco-community/Umbraco.Community.Automate` repo is the **only** way a release starts. Nothing else triggers it — not merging to `main`, not a schedule, not a bot. This is a deliberate design choice: it decouples "code is merged" from "a release happens," so multiple PRs (including unrelated packages' work, or doc-only PRs) can land on `main` without forcing a release, and it gives a clean, scriptable hook for a future release-flow skill (which would do nothing more than push a tag).

## Release workflow

New `.github/workflows/release.yml`, a single reusable/parameterized workflow (not one file per package):

1. **Trigger**: `on: push: tags: '*-v[0-9]+.[0-9]+.[0-9]+*'`
2. **Resolve package**: grep `**/Directory.Build.props` in the repo for the one file whose `<MinVerTagPrefix>` matches the pushed tag's prefix. This makes the workflow self-describing — adding a future package requires zero changes to this workflow file, only a new `MinVerTagPrefix` in that package's own `Directory.Build.props` plus a new NuGet Trusted Publishing policy on nuget.org (see below).
3. **Test**: check out the exact tagged commit and run that package's full test suite fresh — does not trust or skip based on the merge-time CI run that already passed on the PR.
4. **Pack**: `dotnet pack` that package's `.csproj`, producing both `.nupkg` and `.snupkg` (symbol package).
5. **Approval gate**: the publish job targets a `nuget-publish` GitHub Environment configured with required reviewers. The workflow run pauses here until a designated reviewer approves in the GitHub UI — this is a deliberate second confirmation on top of "had push access to push the tag," catching an accidental or premature tag push before anything actually ships.
6. **Publish**: on approval, authenticate via NuGet Trusted Publishing (`NuGet/login@v1` — exchanges the workflow's GitHub OIDC token for a short-lived NuGet API key; no long-lived API key is ever stored as a repo secret) and `dotnet nuget push` both the `.nupkg` and `.snupkg`.
7. **GitHub Release**: create a GitHub Release for the pushed tag, with:
   - **Title**: `<package Title> v<version>` (e.g. `Google Sheets v1.0.0`), derived from that package's `<Title>` MSBuild property (already set today, e.g. `Umbraco Community Automate Google Sheets`) rather than the raw tag — since GitHub's Releases list is a single flat, chronological timeline across the whole repo with no native per-package grouping, a clear title is what keeps the list scannable as more packages are added.
   - **Notes**: GitHub's built-in auto-generated release notes (categorized merged-PR summary since the last tag matching that package's prefix).
   - **Assets**: the built `.nupkg` and `.snupkg` attached as downloadable release artifacts.

## Security model

**Can a fork trigger a real release? No — for two independent, overlapping reasons:**

1. **GitHub Actions execution boundary**: a tag pushed to a fork only triggers workflows *within that fork*, running with the fork's own Actions permissions. It never triggers or executes against the upstream repo.
2. **Trusted Publishing is identity-bound**: the NuGet Trusted Publishing policy configured on nuget.org trusts only `umbraco-community/Umbraco.Community.Automate` plus the specific workflow file (and, per the design above, the `nuget-publish` environment). A fork's OIDC token identity claim would read `<forker>/Umbraco.Community.Automate`, which nuget.org's policy rejects outright — a copied workflow file is useless without also owning the real repo.

**The actual access boundary is therefore: who has push access to the real repo** — the same permission bar that already lets someone merge to `main` today, i.e. no new attack surface is introduced. The `nuget-publish` environment's required-reviewer gate adds a second, deliberate human confirmation on top of that baseline, independent of who has push access.

## CI safety net

Extend the existing `.github/workflows/ci.yml` with a `dotnet pack` step (pack only, no publish, no Trusted Publishing involvement) for each package, run on every PR. This surfaces packaging-configuration mistakes (missing files, bad metadata, a broken `.nuspec`-equivalent) at PR review time, rather than only discovering them when a tag-triggered release fails partway through.

## Documentation

Add a new section to `CONTRIBUTING.md` explaining, for both the repo maintainer and future contributors:
- What MinVer is and why it's a dependency (derives version from git tags, no hand-maintained `<Version>`).
- What `MinVerTagPrefix` and `MinVerIgnoreHeight` do and why both are needed specifically because this is a monorepo with independently-versioned packages (not typical single-package-repo config).
- The end-to-end release flow: push a tag → workflow resolves the package → tests run fresh → pack → **manual approval required** → publish to NuGet via Trusted Publishing → GitHub Release created with artifacts attached.
- The security model: why forks cannot trigger a real release (OIDC identity binding + GitHub Actions execution boundary), and what the approval gate adds on top of repo push access.
- How to add a new package to this scheme in the future (add `MinVerTagPrefix`/`MinVerIgnoreHeight` to its own `Directory.Build.props`, register a matching Trusted Publishing policy on nuget.org — no workflow file changes needed).

## Explicitly out of scope

- Astro + Starlight documentation site — separate future project.
- A Claude Code skill to drive the release flow — separate future project, depends on this pipeline existing.
- Any changelog-generation bot (e.g. release-please) — deliberately not used; GitHub's built-in auto-generated release notes cover this need without adding a bot dependency or auto-opened PRs.

## Open item to verify during implementation

Whether NuGet's Trusted Publishing policy can be configured against a package ID that has never been published before (bootstrapping the very first release of a brand-new package), or whether an initial manual `dotnet nuget push` with a personal API key is required to claim the package ID first, after which Trusted Publishing takes over for all subsequent releases. This wasn't confirmed against nuget.org's current UI/docs during brainstorming and should be checked when implementing the first real release.

## Verification plan (for the implementation phase)

- Confirm `dotnet pack` on the GoogleSheets package locally produces a version derived from a locally-created test tag (e.g. `git tag googlesheets-v0.0.1-test` on a throwaway branch, `dotnet pack`, inspect the resulting `.nupkg`'s version, then delete the test tag) before relying on it in CI.
- Confirm the release workflow's package-resolution step (grep for matching `MinVerTagPrefix`) correctly identifies the GoogleSheets package from a `googlesheets-v*` tag, and would correctly reject/ignore a tag with no matching prefix.
- Dry-run the full workflow once (test → pack → approval gate) without actually approving the publish step, to confirm the gate holds as expected before the first real 1.0.0 release.
- After the first real release, confirm: the package appears on nuget.org, the GitHub Release has the correct title/notes/attached `.nupkg`+`.snupkg`, and a second test tag pushed from a fork (if feasible to test) is confirmed to fail at the Trusted Publishing step rather than silently succeeding.
