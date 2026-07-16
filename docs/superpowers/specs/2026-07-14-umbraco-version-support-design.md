# Umbraco Version Support Policy Design

## Context

`Umbraco.Community.Automate.GoogleSheets` does not reference `Umbraco.Cms` directly — its `.csproj` only has `PackageReference`s to `Umbraco.Automate.Core` and `Umbraco.Automate.OpenIddict`. `Umbraco.Cms` is a transitive dependency, pulled in through those. Only one file in the package touches `Umbraco.Cms` namespaces directly (`GoogleSheetsComposer.cs`), and it gets those types transitively through `Umbraco.Automate.Core` rather than any direct reference of its own — everything else in the package codes against `Umbraco.Automate`'s own abstractions.

`Directory.Packages.props` currently pins four packages to the exact same version, `17.0.0-beta.1`: `Umbraco.Automate`, `Umbraco.Automate.Core`, `Umbraco.Automate.OpenIddict`, and `Umbraco.Automate.Testing`. They move as a family, released together upstream. `Umbraco.Cms` itself is pinned separately (`17.4.2`) — that pin belongs to `Umbraco.Community.Automate.Demo`, the throwaway dev/test site, which *does* reference `Umbraco.Cms` directly (it's a full Umbraco site, not a provider package) and needs a concrete version to actually run against. The Demo site's `Umbraco.Cms` version is dev/test infrastructure, not a published compatibility declaration — it should simply track whatever `Umbraco.Automate` actually requires for local dev/CI, with no separate range policy of its own.

No release has happened yet — `MinVer`-based per-package versioning and a tag-triggered `release.yml` exist (from the release-engineering work), but no tag has been pushed. Before the first release, this design settles how the package's own version number relates to the `Umbraco.Automate` version(s) it supports, and how to structure branches so an older, still-supported line can be patched after development has moved on to a newer one, documented in `CONTRIBUTING.md` so it's easy to follow without re-deriving it each time.

Worth noting: `Umbraco.Automate` has already adopted a "major number = Umbraco.Cms major it targets" convention itself (`17.0.0-beta.1` → built for Umbraco 17). This package inherits that signal for free by depending on `Umbraco.Automate` directly, without needing to also declare (or duplicate) a `Umbraco.Cms` range itself — whatever floor `Umbraco.Automate` actually requires from `Umbraco.Cms` is `Umbraco.Automate`'s own nuspec's problem to get right, not this package's.

## Decided: independent versioning, not numeric alignment

The package's own major version number does **not** mirror `Umbraco.Automate`'s major number directly (e.g. it won't force a `17.x`/`18.x` package release just because `Umbraco.Automate` ships `17.x`/`18.x`). Considered and rejected: direct numeric alignment would mean every zero-impact `Umbraco.Automate` major release forces a matching package release just to keep the numbers in sync, which is exactly the wasted-release problem this policy exists to avoid — the two can't both be true, since skipping a release necessarily makes the numbers drift apart. Instead, compatibility is communicated via a declared `Umbraco.Automate` version range (README + NuGet listing), independent of the package's own SemVer.

## Policy: when does the package's version number move?

Two independent things can each change on their own:

1. **The package's own SemVer** (`major.minor.patch`) — governs the package's own public API/behavior contract for its consumers.
2. **The package's declared `Umbraco.Automate` compatibility range** — which `Umbraco.Automate` version(s) it's known to work against, expressed as the `Umbraco.Automate`/`Umbraco.Automate.Core`/`Umbraco.Automate.OpenIddict` `PackageReference` version range (moved together as a family) and documented in the package's README (e.g. "Supports Umbraco.Automate 17.x–18.x").

These don't have to move together. A new `Umbraco.Automate` major shipping with zero impact on the package means only documentation changes (widen the range) — not a new package release at all.

**The core trigger for a package major bump + a maintenance-branch fork is not "Umbraco.Automate shipped a new major."** It's: *the currently-declared range can no longer be served by a single build.* This can happen for three different reasons, and all three are handled by the exact same mechanism:

- A new `Umbraco.Automate` major breaks something.
- A breaking change lands mid-minor within an already-supported range (e.g. `Umbraco.Automate` 18.2 breaks something that worked on 18.0–18.1, inside a declared `[17.0.0, 19.0.0)` range).
- The package wants to deliberately adopt a new capability (backend or client-side/npm) that doesn't exist across the whole currently-declared range.

Framing it this way means there's one rule to remember, not three special cases.

## Zero-impact releases (the common case)

When a new `Umbraco.Automate` version (major or minor) ships and the package still builds and passes its test suite against it with no code changes: widen the `Umbraco.Automate` family's range upper bound in `Directory.Packages.props` (or the package's own `VersionOverride`, see below) to include it, and update the package's README's supported-versions statement. No package version bump, no new tag, no branch, no release. This is the default expectation — forking is the exception, not the norm.

## Widening the range only happens after verifying it, not ahead of it

A NuGet version range like `[17.0.0, 19.0.0)` resolves to its *lowest* satisfying version by default during restore/build — so simply widening the range doesn't make CI start exercising the new ceiling automatically. To avoid the range ever claiming support for something never actually tested: when a new `Umbraco.Automate` version ships, manually (or via a one-off CI run with a local `VersionOverride` bump) build and run the full test suite against it *first*, and only widen the declared range once that passes. This means every version inside the declared range has been verified at least once at the point it was added.

The residual risk this doesn't close is a *later* patch release inside an already-widened range breaking something (the 18.2 scenario) — there's no way to catch that proactively without continuously re-testing against every new `Umbraco.Automate` patch release, which isn't a proportionate amount of process to add here. The defensive range-narrowing patch (below) is the mitigation for when that residual risk materializes, not a substitute for prevention.

## Per-package version ranges in the monorepo

`Directory.Packages.props`'s `Umbraco.Automate` family entries become a **range**, not an exact pin — e.g. `[17.0.0, 19.0.0)` applied consistently across `Umbraco.Automate`, `Umbraco.Automate.Core`, and `Umbraco.Automate.OpenIddict` (they move together, so the range should too) — representing the repo-wide default supported range. `Umbraco.Automate.Testing` (test-only) can follow the same range or stay pinned more tightly to whatever's actually used in CI; it never ships as part of the published package.

If a specific package needs to diverge from that default (e.g. it has already forked to a newer major with a narrower range while other packages in the monorepo haven't), its own `.csproj` overrides it locally:

```xml
<PackageReference Include="Umbraco.Automate.Core" VersionOverride="[18.2.0, 19.0.0)" />
<PackageReference Include="Umbraco.Automate.OpenIddict" VersionOverride="[18.2.0, 19.0.0)" />
```

Verified via current NuGet documentation: `VersionOverride` takes precedence over the centrally-defined version and is enabled by default under Central Package Management (only disabled repo-wide if `CentralPackageVersionOverrideEnabled` is explicitly set to `false`, which this repo doesn't set). This means different packages in the monorepo can support different `Umbraco.Automate` ranges simultaneously, all living on `main` together — divergence between *packages* never requires branching, only divergence *within a single package's own supported range* does.

`Umbraco.Cms` itself is not touched by this policy — it stays out of the package's own dependency declarations entirely, and its pin on the Demo site is dev/test infrastructure, adjusted only as needed to keep the Demo site running against whatever `Umbraco.Automate` version is current, with no independent range logic of its own.

## Discovering a break: patch the range defensively, first

The moment a break is discovered (CI failure, bug report, manual testing) in an `Umbraco.Automate` version that falls inside the currently-declared range, the first action — before any real fix exists — is a low-ceremony patch release of the *current* package line that narrows the range's upper bound to exclude the broken version (e.g. `[17.0.0, 19.0.0)` → `[17.0.0, 18.2.0)` once `18.2.0` is known-broken). This stops NuGet's normal dependency resolution from letting a new install on the broken version pull in a package release that's already known not to work for it. This happens regardless of whether the underlying break turns out to be fixable in one build (step below) or requires a fork — it's a protective measure, not a decision about the eventual fix.

## Deciding whether a fork is actually needed

After the defensive range-narrowing patch above, decide:

- **Can one build still serve the whole original range?** (Runtime/compile-time feature detection, a conditional code path, a try/fallback.) If yes: implement it, restore the wider range, ship a normal patch or minor release. No fork, no new package major, no branch.
- **If not** — this is the real fork point, covered below.

The same decision applies when the trigger is *wanting* a new feature rather than fixing a break: if the new capability is optional/additive, feature-detect it (backend: reflection or a version check against `Umbraco.Automate`/`Umbraco.Cms`; client-side/npm: check the API exists before using it, e.g. `typeof someNewThing !== 'undefined'`) and gracefully degrade on older versions — one build, one range, no fork. Reach for a fork only when the feature is central enough that dual-path logic isn't worth maintaining. This is a judgment call made per-change, not a fixed rule — the mechanism below is what happens once that call is made either way (bugfix-driven or feature-driven).

*(A third option — true multi-targeting, i.e. one package with conditionally-compiled code per `Umbraco.Automate` version — exists in principle via MSBuild conditional `ItemGroup`s or multiple `TargetFrameworks`, but is deliberately not adopted here. It adds real, permanent build complexity for a benefit almost always achievable more cheaply via feature detection, and is a poor fit for "easy to follow and maintain.")*

## Branching mechanics (when a fork is actually needed)

- **Naming:** `<package-slug>-v<major>` (e.g. `googlesheets-v1`) — mirrors the existing `MinVerTagPrefix` tag convention (`googlesheets-v1.2.3`) rather than naming after a specific `Umbraco.Automate`/Umbraco version, since a single package major can legitimately span multiple `Umbraco.Automate` majors under the range-widening policy above. Naming a branch after a specific upstream version would become misleading the moment that happens.
- **Creation:** cut directly from the last tag that was genuinely still compatible with the full old range — *before* any defensive range-narrowing or adaptation commits. `main` then moves forward: its `Umbraco.Automate` range/floor is raised to the new minimum, and since the compatibility contract changed, that change is what bumps the package's own major version on `main` (e.g. `1.x → 2.x`). The maintenance branch keeps declaring the old, narrower range.
- **Patching the old line later:** a short-lived PR branch off the maintenance branch (e.g. `fix/googlesheets-v1-something`) → PR targets the maintenance branch, not `main` → merge → tag a new patch version from the maintenance branch's HEAD (e.g. `googlesheets-v1.4.3`) → push the tag. Tags aren't branch-scoped in git, so `release.yml`'s existing tag-push trigger fires exactly as it does for `main` today — this needs no workflow change, but the implementation plan should verify it empirically (push a real tag from a non-`main` branch in a disposable scratch scenario) rather than assume it, consistent with how other pieces of this repo's tooling have been verified live rather than by inspection alone.
- **Cross-porting a fix:** if a patch made on a maintenance branch is also relevant to the current `main` line, that is a **separate, manual cherry-pick PR** into `main`. This is the one real ongoing cost of this model and should be stated plainly in the documentation rather than implied to be automatic.
- **Retirement:** maintenance branches are never deleted once created. If a line stops receiving patches, the branch simply goes inert — it stays as an accurate historical record of exactly what that major looked like, and can resume receiving patches later without any recreation step.

## Required workflow change

`.github/workflows/ci.yml` currently only runs `pull_request` checks against `branches: [main]`. A PR targeting a maintenance branch (e.g. `googlesheets-v1`) would get no CI at all today. This needs to become:

```yaml
on:
  pull_request:
    branches: [main, '*-v[0-9]+']
```

Verified via current GitHub Actions documentation: `branches` filters support glob character classes like `[0-9]+`, and quoting is required for patterns starting with `*`.

`release.yml`'s existing tag-push trigger (`'*-v[0-9]+.[0-9]+.[0-9]+*'`) needs no change — git tags are not bound to the branch they were created from, so a tag pushed from a maintenance branch triggers the same workflow the same way a tag pushed from `main` does.

## Documentation

A new `CONTRIBUTING.md` section, placed after "Releasing a package" (the natural sibling topic), covering:

- Why the compatibility range targets `Umbraco.Automate` (the direct dependency, moved as a family of packages) rather than `Umbraco.Cms` (transitive, and not `Umbraco.Automate`'s own concern to duplicate).
- The decision to keep the package's own SemVer independent of `Umbraco.Automate`'s major number, and why (the wasted-zero-diff-release tension).
- The two independent axes (package SemVer vs. declared `Umbraco.Automate` range) and the "zero-impact release" default.
- Why widening the range happens only after verifying against the new version, not ahead of it, and the residual risk that leaves (a later patch release breaking something inside an already-widened range).
- The unifying fork trigger ("one build can no longer serve the whole declared range") and its three causes (major break, mid-minor break, deliberate feature adoption), rather than presenting them as separate rules.
- The defensive range-narrowing patch practice.
- The feature-detection-first decision framework before choosing to fork.
- The maintenance-branch naming convention, creation process, patch/tag workflow, cross-port cost, and non-deletion policy.
- A concrete worked example walking through a hypothetical `Umbraco.Automate` 18.2 break end-to-end, since this is easier to internalize as a narrative than as a set of abstract rules.

## Explicitly out of scope

- True multi-targeting / conditional compilation per `Umbraco.Automate` version within a single package (considered and deliberately rejected above).
- Automating cross-port cherry-picks between `main` and maintenance branches — this stays a manual, deliberate step.
- Retroactively restructuring any existing package's version history — this policy applies going forward; no release has happened yet, so there's nothing to reconcile.
- Deciding *today* whether any specific upcoming `Umbraco.Automate` release will need a fork — this is a policy for handling that decision when it actually arises, not a prediction about when it will.
- Changing how the Demo site pins `Umbraco.Cms` beyond keeping it in sync with whatever `Umbraco.Automate` version is current for local dev/CI — it's infrastructure, not a published compatibility declaration.

## Verification plan (for the implementation phase)

- Confirm `Directory.Packages.props`'s `Umbraco.Automate`/`Umbraco.Automate.Core`/`Umbraco.Automate.OpenIddict` entries accept a version range and that `dotnet restore`/`dotnet build` succeed against it.
- Confirm empirically that this range resolves to its lowest satisfying version by default (not the ceiling) — this is the behavior the "widen only after verifying" practice above depends on, so it should be demonstrated, not assumed.
- Confirm a per-package `VersionOverride` genuinely takes precedence over the central range (a real, live test — not just documentation review).
- Confirm the `ci.yml` branch-filter glob actually matches a real branch named like `googlesheets-v1` and does not accidentally match unrelated branches (e.g. `chore/umbraco-version-support-policy` itself, or `refactor/google-sheets-dry-shared-helpers`).
- Confirm `release.yml`'s tag trigger genuinely fires and resolves the correct package/version from a tag pushed on a non-`main` branch, using a disposable scratch tag/branch, not production infrastructure.
- Confirm the new `CONTRIBUTING.md` section renders correctly and doesn't duplicate or contradict the existing "Releasing a package" section.
