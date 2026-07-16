# Umbraco Version Support Policy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement and document the version-range-based policy for how `Umbraco.Community.Automate.GoogleSheets` declares and maintains compatibility with `Umbraco.Automate`, including the branching mechanics for patching an older still-supported major after development has moved on.

**Architecture:** Convert the `Umbraco.Automate` package family in `Directory.Packages.props` from an exact pin to an explicit version range; extend `ci.yml`'s PR trigger to also run on maintenance branches; document the whole policy (with a concrete worked example) in `CONTRIBUTING.md`; add a compatibility line to the package's own README. Every non-obvious claim this plan relies on (NuGet range resolution behavior, GitHub Actions glob matching, tag-push trigger branch-independence) is verified empirically against the real toolchain before being written into permanent config or docs.

**Tech Stack:** .NET 10 SDK, NuGet Central Package Management (`Directory.Packages.props`), MinVer, GitHub Actions (`ci.yml`, `release.yml`), git.

## Global Constraints

- `Umbraco.Community.Automate.Demo/appsettings.Development.json` may have an uncommitted, local-only working-tree modification (real credentials) on this checkout. If present when a task runs, it must never be staged, committed, or altered by any step in this plan. Stage files explicitly by path in every commit — never `git add -A` or `git add .`. If the working tree is clean when a task runs, that's fine too — never assume a blanket `git add` is safe regardless.
- **Never use the real `googlesheets-v` tag prefix, or any tag/branch name that could cause `release.yml`'s `publish` job to run against real infrastructure, for verification purposes in this plan.** The `nuget-publish` GitHub Environment's required-reviewer protection is part of a separate, still-deferred piece of work (release-engineering Task 5) and may not be configured yet — if it isn't, a workflow run reaching the `publish` job would attempt a real `NuGet/login@v1` + `dotnet nuget push` against the real NuGet.org and a real `gh release create` against the real repo, with no safety net. Task 4 below verifies the tag-trigger claim using a deliberately non-matching prefix specifically to stay clear of this risk — do not "improve realism" by substituting the real prefix.
- Every empirical verification step in this plan must produce real, inspectable output (command output, a file's actual resolved content, a real GitHub Actions run) — not a claim based on reading documentation alone. Where a step says "confirm," that means run the command and look at what it actually printed.
- This plan targets `Umbraco.Community.GoogleSheets` specifically as the only package currently in the monorepo needing this policy applied; the `CONTRIBUTING.md` documentation is written generically (for any future provider package) per the spec, but no other package's files are touched.

**Deliberate deviation from the spec's literal verification wording, flagged here for visibility:** the spec's verification plan says to confirm `release.yml`'s tag trigger "resolves the correct package/version from a tag pushed on a non-`main` branch." Task 3 below deliberately does *not* do this literally — it uses a tag prefix that matches no real package's `MinVerTagPrefix`, specifically so the run fails fast at the resolution step instead of proceeding into packing and the `publish` job's approval gate (whose configuration status is a separate, deferred concern — see the safety constraint above). This proves the narrower, actually-load-bearing claim — the workflow triggers from a non-`main` branch and correctly executes its tag-parsing logic — without the risk of a real-prefix tag reaching the real publish pipeline. If a full end-to-end resolution test against the real `googlesheets-v` prefix is wanted later, that should happen only once the `nuget-publish` environment's approval gate is confirmed configured — not as part of this plan.

---

### Task 1: `Umbraco.Automate` version range — implement and verify the resolution/override mechanics

**Files:**
- Modify: `Directory.Packages.props`
- Create (temporary, deleted by end of task): a scratch console project under a temp directory outside the repo (e.g. `/tmp/nuget-range-verify`)

**Interfaces:**
- Produces: `Directory.Packages.props`'s `Umbraco.Automate`, `Umbraco.Automate.Core`, `Umbraco.Automate.OpenIddict`, and `Umbraco.Automate.Testing` entries expressed as an explicit version range (not an exact pin), consumed by Task 5/6's documentation (which describes this exact mechanism) and by any future package that adds a `VersionOverride`.

- [ ] **Step 1: Empirically confirm NuGet version ranges resolve to their lowest satisfying version, using a real package with multiple published versions**

This proves the general mechanism the whole "widen only after verifying" policy depends on — do this with a real, disposable scratch project outside this repo, using a well-known NuGet package that has several published versions inside a narrow range (so the test can distinguish "lowest" from "not lowest"). `Newtonsoft.Json` works well for this: versions `13.0.1`, `13.0.2`, and `13.0.3` are all real, published NuGet.org releases.

```bash
mkdir -p /tmp/nuget-range-verify
cd /tmp/nuget-range-verify
dotnet new console --no-restore
```

Edit the generated `.csproj` (find its exact name via `ls /tmp/nuget-range-verify/*.csproj`) and add inside a new `<ItemGroup>`:

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="[13.0.1, 13.0.3)" />
</ItemGroup>
```

```bash
cd /tmp/nuget-range-verify
dotnet restore
grep -A2 '"Newtonsoft.Json"' obj/project.assets.json | head -10
```

Expected: the resolved version shown in `project.assets.json` is `13.0.1` (the range's lower bound), **not** `13.0.2` (which also satisfies `[13.0.1, 13.0.3)` and is the newer of the two versions the range actually spans). This is the concrete proof that a range resolves to its floor by default, not its ceiling or "latest available."

- [ ] **Step 2: Clean up the scratch project**

```bash
rm -rf /tmp/nuget-range-verify
```

- [ ] **Step 3: Change the real `Umbraco.Automate` family entries to an explicit range**

Open `Directory.Packages.props`. Current relevant lines:

```xml
<PackageVersion Include="Umbraco.Automate" Version="17.0.0-beta.1" />
<PackageVersion Include="Umbraco.Automate.Core" Version="17.0.0-beta.1" />
<PackageVersion Include="Umbraco.Automate.OpenIddict" Version="17.0.0-beta.1" />
<PackageVersion Include="Umbraco.Automate.Testing" Version="17.0.0-beta.1" />
```

Replace with:

```xml
<!-- Umbraco.Automate, .Core, .OpenIddict, and .Testing are released together as
     one version family upstream and always move in lockstep. Expressed as a
     range (not an exact pin) so this package can declare "supports the whole
     17.x line" rather than one specific patch — see CONTRIBUTING.md's
     "Supporting multiple Umbraco versions" section for the full policy this
     implements. Only 17.0.0-beta.1 exists upstream today, so this range
     currently has exactly one satisfying version; the range still resolves to
     it correctly (NuGet allows prerelease versions within a range whose own
     bound is itself a prerelease). Widen the "18.0.0" ceiling only after
     manually verifying this package still builds and passes its test suite
     against whatever new Umbraco.Automate version prompted the change - see
     the "widening only happens after verifying" policy in CONTRIBUTING.md. -->
<PackageVersion Include="Umbraco.Automate" Version="[17.0.0-beta.1, 18.0.0)" />
<PackageVersion Include="Umbraco.Automate.Core" Version="[17.0.0-beta.1, 18.0.0)" />
<PackageVersion Include="Umbraco.Automate.OpenIddict" Version="[17.0.0-beta.1, 18.0.0)" />
<PackageVersion Include="Umbraco.Automate.Testing" Version="[17.0.0-beta.1, 18.0.0)" />
```

`Umbraco.Automate.Testing` gets the same range as the rest of the family rather than a separate policy: it's test-only (never ships inside the published package), and giving it its own divergent pinning rule would be one more thing to remember for no real benefit — keeping the whole family on one range is simpler to reason about and document.

- [ ] **Step 4: Verify the real repo still restores and builds correctly against the new range**

```bash
cd /Users/warren/Code/Personal/Umbraco.Community.Automate
dotnet restore
dotnet build --configuration Release 2>&1 | tail -10
```

Expected: `0 Error(s)`, same as before this change (some `NU1902`/`NU1903` security-advisory warnings are pre-existing and unrelated to this change — only new *errors* would indicate a problem).

- [ ] **Step 5: Confirm the resolved version is still `17.0.0-beta.1`**

```bash
grep -A3 '"Umbraco.Automate/' GoogleSheets/Umbraco.Community.Automate.GoogleSheets/obj/project.assets.json | head -20
```

Expected: shows `17.0.0-beta.1` as the resolved version — confirming the range change didn't silently pull in something unexpected (there's nothing else for it to resolve to today, but this confirms the range syntax itself didn't break resolution).

- [ ] **Step 6: Verify `VersionOverride` genuinely takes precedence over the central range — using a temporary, throwaway edit, not a permanent one**

No package needs an override today (nothing has forked yet), but the mechanism itself needs proving before it's documented as available. Temporarily add an override to the GoogleSheets `.csproj` that would only succeed if `VersionOverride` is actually honored:

```bash
cd /Users/warren/Code/Personal/Umbraco.Community.Automate
git status --porcelain=v1
```

Confirm `Umbraco.Community.Automate.Demo/appsettings.Development.json` is the only thing shown modified (if anything) — do not touch it in the steps below.

Temporarily edit `GoogleSheets/Umbraco.Community.Automate.GoogleSheets/Umbraco.Community.Automate.GoogleSheets.csproj`, changing:

```xml
<PackageReference Include="Umbraco.Automate.Core" />
```

to:

```xml
<PackageReference Include="Umbraco.Automate.Core" VersionOverride="17.0.0-beta.1" />
```

(Using the exact same version as the central range's floor is deliberate — the point isn't to pull in a *different* version, since only one exists upstream; it's to prove NuGet accepts and resolves via the override at all, distinguishing it from the central range mechanism.)

```bash
dotnet restore
grep -A3 '"Umbraco.Automate.Core' GoogleSheets/Umbraco.Community.Automate.GoogleSheets/obj/project.assets.json | head -10
dotnet build GoogleSheets/Umbraco.Community.Automate.GoogleSheets --configuration Release 2>&1 | tail -5
```

Expected: restore and build both still succeed, and the resolved version is `17.0.0-beta.1` — confirming NuGet accepted the `VersionOverride` syntax without error (a typo'd or unsupported attribute would surface as a restore error, not a silent no-op).

- [ ] **Step 7: Revert the temporary `VersionOverride` — this must not be a permanent change**

```bash
cd /Users/warren/Code/Personal/Umbraco.Community.Automate
git diff GoogleSheets/Umbraco.Community.Automate.GoogleSheets/Umbraco.Community.Automate.GoogleSheets.csproj
git checkout -- GoogleSheets/Umbraco.Community.Automate.GoogleSheets/Umbraco.Community.Automate.GoogleSheets.csproj
git status --porcelain=v1
dotnet restore
```

Expected: the `git diff` before reverting shows exactly the one-line `VersionOverride` addition and nothing else; after `git checkout --`, `git status` shows the `.csproj` is no longer modified (only `appsettings.Development.json`, if it was already modified before this task started, should remain).

- [ ] **Step 8: Commit the real `Directory.Packages.props` change**

```bash
git add Directory.Packages.props
git status --porcelain=v1
git commit -m "chore: express Umbraco.Automate family as a version range instead of an exact pin

Only 17.0.0-beta.1 exists upstream today so this doesn't change what
actually resolves, but establishes the range-based compatibility
mechanism the version support policy depends on - see CONTRIBUTING.md's
Supporting multiple Umbraco versions section (added in a later commit on
this branch)."
```

Confirm the `git status --porcelain=v1` shown before committing lists only `Directory.Packages.props` as staged (`M  Directory.Packages.props`) and does not list `appsettings.Development.json`.

---

### Task 2: `ci.yml` maintenance-branch trigger — implement and verify the glob match live

**Files:**
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Consumes: none from Task 1.
- Produces: `ci.yml`'s `pull_request.branches` filter includes the `*-v[0-9]+` pattern, which Task 3's CONTRIBUTING.md documentation references as already implemented.

- [ ] **Step 1: Make the change**

In `.github/workflows/ci.yml`, change:

```yaml
on:
  pull_request:
    branches: [main]
  workflow_dispatch:
  workflow_call:
```

to:

```yaml
on:
  pull_request:
    branches: [main, '*-v[0-9]+']
  workflow_dispatch:
  workflow_call:
```

- [ ] **Step 2: Commit this change on its own, before verifying — the live verification needs it pushed**

```bash
cd /Users/warren/Code/Personal/Umbraco.Community.Automate
git add .github/workflows/ci.yml
git status --porcelain=v1
git commit -m "ci: run pull_request checks against maintenance branches too

Maintenance branches (named <package-slug>-vN, e.g. googlesheets-v1) are
part of the version support policy being added on this branch - a PR
targeting one needs the same CI coverage a PR targeting main gets."
```

- [ ] **Step 3: Push this branch so the updated `ci.yml` exists on a real ref GitHub can evaluate**

```bash
git push -u origin chore/umbraco-version-support-policy
```

- [ ] **Step 4: Create a scratch base branch matching the glob, from this branch's current HEAD**

Use an obviously-scratch name that could never collide with a real future maintenance branch (real ones follow `<package-slug>-vN` for an actual package slug — this uses a name no real package will ever have):

```bash
git branch verify-glob-match-v0
git push -u origin verify-glob-match-v0
```

- [ ] **Step 5: Open a real, throwaway PR targeting the scratch branch**

```bash
git checkout -b verify-glob-match-head
printf '# scratch verification commit, will be deleted\n' > VERIFY_GLOB_SCRATCH.md
git add VERIFY_GLOB_SCRATCH.md
git commit -m "test: scratch commit for ci.yml glob verification, will not be merged"
git push -u origin verify-glob-match-head

gh pr create --base verify-glob-match-v0 --head verify-glob-match-head \
  --title "SCRATCH: verify ci.yml glob match (do not merge)" \
  --body "Throwaway PR to confirm ci.yml's pull_request.branches glob \`'*-v[0-9]+'\` actually matches a branch named like \`verify-glob-match-v0\`. Will be closed and both branches deleted immediately after confirming CI triggered."
```

- [ ] **Step 6: Confirm CI actually triggered on this PR**

```bash
sleep 15
gh pr checks verify-glob-match-head 2>&1 || gh run list --branch verify-glob-match-head --limit 5
```

Expected: at least one `CI` workflow run appears associated with this PR (status `pending`/`in_progress`/`queued` is sufficient proof it triggered — no need to wait for full completion). If nothing appears after a few seconds, wait and re-check once (`gh run list --branch verify-glob-match-head --limit 5`) before concluding it didn't trigger.

- [ ] **Step 7: Also confirm the glob does NOT match unrelated branches — check via the already-existing evidence**

The current branch (`chore/umbraco-version-support-policy`) and `refactor/google-sheets-dry-shared-helpers` do not end in `-v<digits>`, so they're negative-match evidence by construction — confirm this reasoning holds by checking no spurious CI run exists against either from *this task's own commits* (there shouldn't be, since PRs against `main` were never opened here, but confirm the glob syntax itself is precise):

```bash
gh api repos/umbraco-community/Umbraco.Community.Automate/branches --jq '.[].name' | grep -E -- '-v[0-9]+$'
```

Expected: only `verify-glob-match-v0` (and, if it ever exists, a real `googlesheets-vN` branch) should match this pattern — confirming no existing branch name accidentally collides with the maintenance-branch convention.

- [ ] **Step 8: Clean up everything from this verification — nothing scratch may be left behind**

```bash
gh pr close verify-glob-match-head --delete-branch
git push origin --delete verify-glob-match-v0
git branch -D verify-glob-match-v0 verify-glob-match-head
git checkout chore/umbraco-version-support-policy
gh api repos/umbraco-community/Umbraco.Community.Automate/branches --jq '.[].name' | grep -E -- '-v[0-9]+$'
```

Expected: the final `gh api` check shows no scratch branches remain (only a real `googlesheets-vN` branch would ever legitimately show up here, and none exists yet). Confirm via `gh pr view verify-glob-match-head` that the PR shows as `CLOSED` (it will still exist in GitHub's history as a closed PR — that's fine and expected, only the *branches* need deleting, not the PR record itself).

---

### Task 3: `release.yml` tag-trigger branch-independence — verify without touching the real publish pipeline

**Files:** none modified — this task is verification-only, confirming the spec's claim that no change to `release.yml` is needed.

**Interfaces:**
- Consumes: none.
- Produces: verified evidence (referenced by Task 4's documentation) that a tag pushed from a non-`main` branch triggers `release.yml` the same way a tag pushed from `main` does.

- [ ] **Step 1: Create a scratch branch and a tag that will trigger `release.yml` but is guaranteed to fail fast, before reaching pack/publish**

**Read the Global Constraints section above again before this step.** The tag prefix used here must never be `googlesheets-v` or any other real package's `MinVerTagPrefix` — using a real prefix would let this run proceed into `resolve-and-pack` (which packs the real project) and then pause at the `publish` job, which requires the `nuget-publish` GitHub Environment's approval gate to actually be configured to stay safe — and that gate's setup is separately deferred, unconfirmed work. A non-matching prefix instead makes `resolve-and-pack`'s own tag-resolution step fail cleanly with "found 0" *before* any packing happens, while still proving the one thing this step needs to prove: that the workflow triggers at all from a non-`main` branch, and that its tag-parsing logic runs against the real ref.

```bash
cd /Users/warren/Code/Personal/Umbraco.Community.Automate
git checkout -b verify-tag-trigger-scratch
git push -u origin verify-tag-trigger-scratch
git tag verify-nonmain-trigger-v0.0.1
git push origin verify-nonmain-trigger-v0.0.1
```

- [ ] **Step 2: Confirm the workflow actually triggered**

```bash
sleep 15
gh run list --workflow=release.yml --limit 5
```

Expected: a run appears, triggered by the `verify-nonmain-trigger-v0.0.1` tag push, with a recent timestamp.

- [ ] **Step 3: Confirm it reached and failed at the expected tag-resolution step — not somewhere unexpected**

```bash
RUN_ID=$(gh run list --workflow=release.yml --limit 1 --json databaseId --jq '.[0].databaseId')
gh run watch "$RUN_ID" --exit-status 2>&1 | tail -40 || true
gh run view "$RUN_ID" --log 2>&1 | grep -i "Expected exactly one package\|MinVerTagPrefix" | head -5
```

Expected: the run fails, and the log contains the message `Expected exactly one package with MinVerTagPrefix 'verify-nonmain-trigger-v', found 0` — confirming (a) the workflow triggered from a tag whose target commit lives only on `verify-tag-trigger-scratch`, not `main`, proving tags aren't branch-scoped for this purpose, and (b) it correctly reached and executed the tag-parsing/resolution logic before failing, rather than failing for some unrelated reason (a different failure message here would mean this test didn't prove what it's meant to prove — investigate rather than treat it as a pass).

- [ ] **Step 4: Clean up — delete the scratch tag and branch, locally and on origin**

```bash
git push origin --delete verify-nonmain-trigger-v0.0.1
git tag -d verify-nonmain-trigger-v0.0.1
git push origin --delete verify-tag-trigger-scratch
git checkout chore/umbraco-version-support-policy
git branch -D verify-tag-trigger-scratch
git tag -l | grep verify-nonmain || echo "confirmed: no scratch tags remain locally"
git ls-remote --tags origin | grep verify-nonmain || echo "confirmed: no scratch tags remain on origin"
```

Expected: both "confirmed" messages print, and the failed scratch workflow run itself is left in GitHub's Actions history (runs can't be deleted via `gh` without extra permissions, and leaving a record of a failed scratch run is harmless — it's not a release, not a tag, not a branch, doesn't appear anywhere a consumer would see it).

---

### Task 4: `CONTRIBUTING.md` — document the whole policy

**Files:**
- Modify: `CONTRIBUTING.md`

**Interfaces:**
- Consumes: the real, verified mechanics from Tasks 1–3 (this task documents what was actually proven, not what was merely planned).
- Produces: a new `## Supporting multiple Umbraco versions` section, the canonical reference for this policy going forward.

- [ ] **Step 1: Add the new section, placed after "Releasing a package" and before "License"**

In `CONTRIBUTING.md`, insert the following between the end of the "Releasing a package" section (after its "Adding a new package to this scheme" subsection) and the `## License` heading:

````markdown
## Supporting multiple Umbraco versions

This package depends on `Umbraco.Automate`/`Umbraco.Automate.Core`/`Umbraco.Automate.OpenIddict` directly — never on `Umbraco.Cms` itself, which is a transitive dependency pulled in through `Umbraco.Automate`. Compatibility is tracked against `Umbraco.Automate`'s own version, not Umbraco.Cms's: `Umbraco.Automate` already aligns its own major version number with the Umbraco.Cms major it targets (e.g. `17.0.0-beta.1` is built for Umbraco 17), so a provider package here inherits that signal for free without needing to duplicate a Umbraco.Cms compatibility claim of its own.

### Two independent version numbers

A package has two things that can each change independently:

1. **Its own SemVer** (`major.minor.patch`) — the package's own public API/behavior contract.
2. **Its declared `Umbraco.Automate` compatibility range** — expressed as the `Umbraco.Automate`/`.Core`/`.OpenIddict` `PackageReference` version range in `Directory.Packages.props` (or a package-specific `VersionOverride` if it needs to diverge from the repo-wide default — see NuGet's [Central Package Management docs](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management#overriding-package-versions)), and documented in the package's own README.

These deliberately don't move together, and a package's own major version does **not** mirror `Umbraco.Automate`'s major number. If it did, every `Umbraco.Automate` major release would force a matching package release just to keep the numbers in sync — even when nothing in the package actually needed to change. The default expectation: a new `Umbraco.Automate` version with zero impact on a package means only a documentation update (widen the range), not a new release.

### Widening the range only after verifying it

A NuGet version range like `[17.0.0-beta.1, 18.0.0)` resolves to its *lowest* satisfying version during restore/build, not the newest one inside the range — widening the range's ceiling doesn't make CI start testing against it automatically. So when a new `Umbraco.Automate` version ships: build and run the full test suite against it first (locally, or via a one-off local `VersionOverride` bump), and only widen the declared range once that passes. Every version inside a declared range should have been verified at least once at the point it was added.

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

Say a package currently declares `Umbraco.Automate` support as `[17.0.0-beta.1, 19.0.0)` (verified against 17.0.0 and 18.0.0/18.1.0 at various points), and is on package version `1.6.0`.

1. `Umbraco.Automate` 18.2.0 ships. A bug report comes in: something that worked on 18.1.0 and earlier now throws on 18.2.0.
2. **First move — defensive patch:** narrow the range in `Directory.Packages.props` from `[17.0.0-beta.1, 19.0.0)` to `[17.0.0-beta.1, 18.2.0)`, bump the package to `1.6.1`, tag `googlesheets-v1.6.1`, push. This ships immediately, before any real fix exists — it just stops new installs on 18.2.0+ from pulling in a package version already known not to work for them.
3. **Decide: fork or not?** Investigate the break. If it's fixable with a version check or a try/fallback that still works on 17.x–18.1.x too: fix it, restore the range to `[17.0.0-beta.1, 19.0.0)`, ship `1.6.2`. Done — no fork.
4. **If it's not fixable in one build** (say 18.2.0 removed something the code genuinely needs, with no compatible shim): this is the fork point.
   - Cut `googlesheets-v1` from the `googlesheets-v1.6.0` tag (the last version that was genuinely 17.x–18.1.x-compatible, *before* the defensive narrowing in step 2).
   - On `main`, adapt the code for 18.2.0's change, set the range to `[18.2.0, 19.0.0)`, and release this as `2.0.0` — the package major bump reflects that the compatibility contract genuinely changed.
   - `googlesheets-v1` keeps declaring `[17.0.0-beta.1, 18.2.0)` (from the defensive patch in step 2) and can still receive its own patches (tagged `googlesheets-v1.6.x`) if something else needs fixing for 17.x–18.1.x users, independently of whatever happens on `main` going forward.
5. A month later, someone reports a separate, unrelated bug that also affects `googlesheets-v1` users. Fix it on a branch off `googlesheets-v1`, PR against `googlesheets-v1`, tag `googlesheets-v1.6.3`. Since the same bug also exists in `main`'s `2.x` code, that's a second, separate cherry-pick PR into `main`.
````

- [ ] **Step 2: Read the whole file top to bottom and confirm the new section fits without duplicating or contradicting "Releasing a package"**

`CONTRIBUTING.md`'s existing "Releasing a package" section covers *how a release mechanically happens* (tags, MinVer, the approval-gated publish pipeline); the new section covers *when and why a version/range changes in the first place* — confirm these read as complementary, not repetitive, and that the new section's own internal markdown fences are correctly closed (this section has nested triple-backtick `bash`/`xml`/`json` blocks inside the worked example and elsewhere — verify none of them leak into surrounding prose by rendering/re-reading the file carefully).

- [ ] **Step 3: Commit**

```bash
cd /Users/warren/Code/Personal/Umbraco.Community.Automate
git add CONTRIBUTING.md
git status --porcelain=v1
git commit -m "docs: document the Umbraco version support policy in CONTRIBUTING.md

Covers why the compatibility range targets Umbraco.Automate rather than
Umbraco.Cms, why package versioning stays independent of Umbraco.Automate's
major number, the range-widen-after-verify practice and its residual risk,
the single fork trigger (one build can no longer serve the whole range) and
its three causes, the defensive patch practice, and the maintenance-branch
naming/creation/patch/cross-port/retirement mechanics, with a worked
example."
```

---

### Task 5: GoogleSheets `README.md` — state the supported range

**Files:**
- Modify: `GoogleSheets/Umbraco.Community.Automate.GoogleSheets/README.md`

**Interfaces:**
- Consumes: the range established in Task 1 (`[17.0.0-beta.1, 18.0.0)`) and the CONTRIBUTING.md anchor added in Task 4.

- [ ] **Step 1: Add a "Requirements" section right after "Overview" and before "Key Features"**

In `GoogleSheets/Umbraco.Community.Automate.GoogleSheets/README.md`, current relevant lines:

```markdown
Umbraco.Community.Automate.GoogleSheets is a provider package that adds Google Sheets connectivity to Umbraco Automate. It contributes a Google Sheets connection type (authenticated via OAuth) and ten actions covering the spreadsheet lifecycle — creating spreadsheets and sheet tabs, appending/finding/updating/deleting/upserting rows, and reading or clearing ranges — so an automation can manage spreadsheet data end-to-end without leaving the workflow builder.

## Key Features
```

Change to:

```markdown
Umbraco.Community.Automate.GoogleSheets is a provider package that adds Google Sheets connectivity to Umbraco Automate. It contributes a Google Sheets connection type (authenticated via OAuth) and ten actions covering the spreadsheet lifecycle — creating spreadsheets and sheet tabs, appending/finding/updating/deleting/upserting rows, and reading or clearing ranges — so an automation can manage spreadsheet data end-to-end without leaving the workflow builder.

## Requirements

Supports `Umbraco.Automate` 17.x (declared range: `[17.0.0-beta.1, 18.0.0)`). See [CONTRIBUTING.md](../../CONTRIBUTING.md#supporting-multiple-umbraco-versions) for how this range is maintained and when it changes.

## Key Features
```

- [ ] **Step 2: Confirm the relative link resolves correctly**

```bash
cd /Users/warren/Code/Personal/Umbraco.Community.Automate
test -f GoogleSheets/Umbraco.Community.Automate.GoogleSheets/../../CONTRIBUTING.md && echo "link path resolves correctly"
```

Expected: `link path resolves correctly` prints — confirming `../../CONTRIBUTING.md` from `GoogleSheets/Umbraco.Community.Automate.GoogleSheets/README.md` correctly reaches the repo-root `CONTRIBUTING.md` (same depth as the existing `../../LICENSE` link already in this file).

- [ ] **Step 3: Commit**

```bash
git add GoogleSheets/Umbraco.Community.Automate.GoogleSheets/README.md
git status --porcelain=v1
git commit -m "docs: state the supported Umbraco.Automate range in the package README"
```

---

### Task 6: Final verification pass

**Files:** none modified — this task re-confirms the whole branch is coherent after all prior tasks.

- [ ] **Step 1: Full clean build and test run from scratch**

```bash
cd /Users/warren/Code/Personal/Umbraco.Community.Automate
dotnet restore
dotnet build --configuration Release 2>&1 | tail -10
dotnet test GoogleSheets/Umbraco.Community.Automate.GoogleSheets.Tests --no-build --configuration Release 2>&1 | tail -10
```

Expected: `0 Error(s)` on build, and `Failed: 0` with `184` (or however many currently exist) passing tests.

- [ ] **Step 2: Confirm the working tree is clean except for the pre-existing, untouched `appsettings.Development.json` modification (if it was present at the start)**

```bash
git status --porcelain=v1
git log --oneline main..HEAD
```

Expected: `git status` shows nothing except possibly ` M Umbraco.Community.Automate.Demo/appsettings.Development.json` (untouched throughout this whole plan), and the commit log shows exactly 4 new real commits — one each from Task 1 (Step 8), Task 2 (Step 2), Task 4 (Step 3), and Task 5 (Step 3); Task 3 is verification-only and produces no commit — on top of this branch's earlier spec commits.

- [ ] **Step 3: Confirm no scratch artifacts leaked into real repo state**

```bash
gh api repos/umbraco-community/Umbraco.Community.Automate/branches --jq '.[].name' | grep -E -- '-v[0-9]+$' || echo "confirmed: no maintenance-branch-pattern branches exist"
git tag -l | grep -i verify || echo "confirmed: no scratch tags remain locally"
gh pr list --state open --json headRefName --jq '.[].headRefName' | grep -i verify || echo "confirmed: no scratch PRs remain open"
```

Expected: all three "confirmed" messages print.
