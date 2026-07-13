# Versioning & Release Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the Google Sheets package (and every future package in this monorepo) a tag-triggered, independently-versioned release pipeline that publishes to NuGet via Trusted Publishing and creates a GitHub Release with attached artifacts, gated behind a manual approval step.

**Architecture:** Each package derives its version from git tags via MinVer, scoped to that package alone by its own `MinVerTagPrefix`. Pushing a matching tag triggers a GitHub Actions workflow that reuses the existing CI workflow (via `workflow_call`) to test the tagged commit, packs the `.nupkg`/`.snupkg`, pauses for manual approval via a GitHub Environment, then publishes via NuGet Trusted Publishing and creates a GitHub Release.

**Tech Stack:** .NET 10 SDK, MinVer (NuGet), GitHub Actions (reusable workflows, environments, OIDC), `NuGet/login@v1` action, GitHub CLI (`gh`).

## Global Constraints

- No `<Version>` property is ever hand-maintained anywhere — version comes from git tags via MinVer, exclusively.
- Tag format: `<package-slug>-v<semver>` where `<package-slug>` is the package folder name minus the `Umbraco.Community.Automate.` prefix, lowercased (e.g. `Umbraco.Community.Automate.GoogleSheets` → `googlesheets`, tag `googlesheets-v1.0.0`).
- `MinVerIgnoreHeight` is always `true` for every package — this repo never wants auto-incrementing pre-release versions between tags.
- The release workflow must re-run the same checks as a normal PR (not skip/trust prior CI) — achieved by calling `ci.yml` as a reusable workflow, not duplicating its jobs.
- No NuGet API key is ever stored as a repository secret — publishing uses Trusted Publishing (OIDC) exclusively.
- Publishing requires manual approval via a GitHub Environment (`nuget-publish`) — a tag push alone must never be sufficient to publish.
- GitHub Action versions used elsewhere in this repo's `ci.yml` must be matched for consistency: `actions/checkout@v7`, `actions/setup-dotnet@v5`, `actions/setup-node@v6`, `actions/upload-artifact@v7`.

---

### Task 1: Add MinVer versioning and symbol packages to the Google Sheets package

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `Umbraco.Community.Automate.GoogleSheets/Umbraco.Community.Automate.GoogleSheets.csproj`
- Modify: `Umbraco.Community.Automate.GoogleSheets/Directory.Build.props`

**Interfaces:**
- Produces: after this task, `dotnet pack Umbraco.Community.Automate.GoogleSheets` at a commit with an ancestor tag `googlesheets-v<semver>` produces `Umbraco.Community.Automate.GoogleSheets.<semver>.nupkg` and a matching `.snupkg`. Task 2 and Task 3 both invoke `dotnet pack` on this project and rely on this behavior.

- [ ] **Step 1: Add the MinVer package reference**

Run from the repo root:

```bash
dotnet add Umbraco.Community.Automate.GoogleSheets/Umbraco.Community.Automate.GoogleSheets.csproj package MinVer
```

Expected: this repo uses central package management (`ManagePackageVersionsCentrally=true` in `Directory.Packages.props`), so the command adds a `<PackageVersion Include="MinVer" Version="X.Y.Z" />` line to `Directory.Packages.props` (X.Y.Z will be whatever the latest stable MinVer version is at the time — that's correct, do not hardcode an older version) and a versionless `<PackageReference Include="MinVer" />` to the GoogleSheets csproj's existing `PackageReference` `ItemGroup`.

- [ ] **Step 2: Mark the MinVer reference as build-only**

Open `Umbraco.Community.Automate.GoogleSheets/Umbraco.Community.Automate.GoogleSheets.csproj` and find the `<PackageReference Include="MinVer" />` line Step 1 just added. Change it to:

```xml
<PackageReference Include="MinVer" PrivateAssets="All" />
```

`PrivateAssets="All"` stops MinVer itself from flowing as a transitive dependency to anyone who installs this package — it's a build-time-only tool, not a runtime dependency.

- [ ] **Step 3: Add MinVer configuration and symbol-package properties**

Open `Umbraco.Community.Automate.GoogleSheets/Directory.Build.props`. It currently looks like:

```xml
<Project>
  <PropertyGroup>
    <Authors>Warren Buckley &amp; Umbraco Community</Authors>
    <PackageProjectUrl>https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Umbraco.Community.Automate.GoogleSheets</PackageProjectUrl>
    <RepositoryUrl>https://github.com/umbraco-community/Umbraco.Community.Automate</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>
</Project>
```

Replace it with:

```xml
<Project>
  <PropertyGroup>
    <Authors>Warren Buckley &amp; Umbraco Community</Authors>
    <PackageProjectUrl>https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Umbraco.Community.Automate.GoogleSheets</PackageProjectUrl>
    <RepositoryUrl>https://github.com/umbraco-community/Umbraco.Community.Automate</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <PropertyGroup>
    <!-- Tags this repo uses for GoogleSheets releases, e.g. googlesheets-v1.0.0.
         IgnoreHeight: this package is only ever versioned via a deliberate tag
         push, never by auto-incrementing between tags, so MinVer's commit-
         height-based pre-release suffixing — which would also react to
         unrelated packages' commits in this monorepo's shared history — is
         turned off. See MinVer's own README FAQ on independent versioning of
         multiple projects in a single repository via distinct tag prefixes. -->
    <MinVerTagPrefix>googlesheets-v</MinVerTagPrefix>
    <MinVerIgnoreHeight>true</MinVerIgnoreHeight>
  </PropertyGroup>

  <PropertyGroup>
    <!-- Produce a .snupkg (portable PDB symbol package) alongside the .nupkg
         so consumers can step into this package's source when debugging. -->
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>
</Project>
```

Note: this file lives at `Umbraco.Community.Automate.GoogleSheets/Directory.Build.props`, and MSBuild's `Directory.Build.props` cascade walks *up* from each project's own directory — since `Umbraco.Community.Automate.GoogleSheets.Tests` is a sibling directory, not a child of `Umbraco.Community.Automate.GoogleSheets`, these properties correctly apply only to the main package project, not its test project.

- [ ] **Step 4: Verify the build still succeeds**

```bash
dotnet build Umbraco.Community.Automate.GoogleSheets
```

Expected: `Build succeeded.` with 0 errors (warnings about other unrelated NuGet advisories are pre-existing and fine).

- [ ] **Step 5: Verify version derivation with a local throwaway tag**

```bash
git tag googlesheets-v9.9.9-planverify
dotnet pack Umbraco.Community.Automate.GoogleSheets --configuration Release --output /tmp/minver-verify
ls /tmp/minver-verify
```

Expected: the output directory contains `Umbraco.Community.Automate.GoogleSheets.9.9.9-planverify.nupkg` and a matching `.snupkg` — confirming MinVer picked up the tag and derived the version from it correctly.

Clean up immediately — this tag must never be pushed:

```bash
git tag -d googlesheets-v9.9.9-planverify
rm -rf /tmp/minver-verify
```

- [ ] **Step 6: Verify version derivation *without* a tag present (the CI/no-tag case)**

```bash
dotnet pack Umbraco.Community.Automate.GoogleSheets --configuration Release --output /tmp/minver-verify-notag
ls /tmp/minver-verify-notag
rm -rf /tmp/minver-verify-notag
```

Expected: pack still succeeds and produces a `.nupkg` with some `0.0.0-alpha.*`-style fallback version (MinVer's default when no matching tag exists in history yet) — confirming Task 2's PR-time pack check (which runs with no release tag present) won't fail just because there's no tag.

- [ ] **Step 7: Commit**

```bash
git add Directory.Packages.props Umbraco.Community.Automate.GoogleSheets/Umbraco.Community.Automate.GoogleSheets.csproj Umbraco.Community.Automate.GoogleSheets/Directory.Build.props
git commit -m "add(build): version GoogleSheets via MinVer, produce .snupkg symbol packages"
```

---

### Task 2: Add a PR-time pack check to CI and make CI reusable

**Files:**
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Produces: a `workflow_call:` trigger on `ci.yml`, which Task 3's `release.yml` consumes via `jobs.ci.uses: ./.github/workflows/ci.yml`.

- [ ] **Step 1: Add a pack-check step to the backend-tests job**

Open `.github/workflows/ci.yml`. Find the `backend-tests` job's steps (currently ending with the `dotnet test` step) and add one more step after it:

```yaml
  backend-tests:
    name: Backend unit tests
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v7

      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - run: dotnet restore

      - run: dotnet build --no-restore --configuration Release

      - run: dotnet test Umbraco.Community.Automate.GoogleSheets.Tests --no-build --configuration Release

      - name: Verify the package still packs
        run: dotnet pack Umbraco.Community.Automate.GoogleSheets --no-build --configuration Release --output ./pack-check
```

This doesn't publish anywhere and doesn't upload the output anywhere — its only purpose is to fail the PR check if a packaging-metadata change (missing file, bad `<PackageReadmeFile>` reference, etc.) would break `dotnet pack` at release time.

- [ ] **Step 2: Add the `workflow_call` trigger**

In the same file, find the `on:` block:

```yaml
on:
  pull_request:
    branches: [main]
  workflow_dispatch:
```

Change it to:

```yaml
on:
  pull_request:
    branches: [main]
  workflow_dispatch:
  workflow_call:
```

This lets `release.yml` (Task 3) invoke this entire workflow as a reusable workflow, so the release pipeline runs the exact same checks as a PR — no duplicated job definitions.

- [ ] **Step 3: Verify the YAML is syntactically valid**

```bash
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml'))" && echo "VALID"
```

Expected: `VALID` printed, no exception.

- [ ] **Step 4: Verify the pack-check step works locally**

```bash
dotnet build Umbraco.Community.Automate.GoogleSheets --configuration Release
dotnet pack Umbraco.Community.Automate.GoogleSheets --no-build --configuration Release --output ./pack-check
ls ./pack-check
rm -rf ./pack-check
```

Expected: a `.nupkg` and `.snupkg` appear in `./pack-check`, matching what the new CI step will do.

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add pack-check safety net, make CI callable from the release workflow"
```

---

### Task 3: Create the tag-triggered release workflow

**Files:**
- Create: `.github/workflows/release.yml`

**Interfaces:**
- Consumes: `ci.yml`'s `workflow_call` trigger from Task 2; MinVer's tag-derived versioning and `.snupkg` output from Task 1.
- Produces: a published NuGet package and a GitHub Release, gated behind the `nuget-publish` GitHub Environment created in Task 5.

- [ ] **Step 1: Write the release workflow**

Create `.github/workflows/release.yml`:

```yaml
name: Release

on:
  push:
    tags:
      - '*-v[0-9]+.[0-9]+.[0-9]+*'

permissions:
  contents: write
  id-token: write

jobs:
  ci:
    uses: ./.github/workflows/ci.yml

  resolve-and-pack:
    name: Resolve package and pack
    needs: ci
    runs-on: ubuntu-latest
    outputs:
      project-dir: ${{ steps.resolve.outputs.project-dir }}
      version: ${{ steps.resolve.outputs.version }}
      title: ${{ steps.resolve.outputs.title }}
    steps:
      - uses: actions/checkout@v7
        with:
          fetch-depth: 0

      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'

      - name: Resolve package from tag
        id: resolve
        run: |
          set -euo pipefail
          TAG="${GITHUB_REF_NAME}"

          if [[ "$TAG" =~ ^(.+-v)([0-9]+\.[0-9]+\.[0-9]+.*)$ ]]; then
            PREFIX="${BASH_REMATCH[1]}"
            VERSION="${BASH_REMATCH[2]}"
          else
            echo "Tag '$TAG' does not match the expected <prefix>-v<semver> format" >&2
            exit 1
          fi

          MATCHES=$(grep -rl "<MinVerTagPrefix>${PREFIX}</MinVerTagPrefix>" --include="Directory.Build.props" .)
          COUNT=$(printf '%s\n' "$MATCHES" | grep -c .)

          if [ "$COUNT" -ne 1 ]; then
            echo "Expected exactly one package with MinVerTagPrefix '$PREFIX', found $COUNT" >&2
            exit 1
          fi

          PROJECT_DIR=$(dirname "$MATCHES")
          TITLE=$(grep -ohP '(?<=<Title>).*(?=</Title>)' "$PROJECT_DIR"/*.csproj | head -1)

          echo "Resolved tag '$TAG' -> package '$PROJECT_DIR' (title: $TITLE, version: $VERSION)"
          echo "project-dir=$PROJECT_DIR" >> "$GITHUB_OUTPUT"
          echo "version=$VERSION" >> "$GITHUB_OUTPUT"
          echo "title=$TITLE" >> "$GITHUB_OUTPUT"

      - run: dotnet restore

      - name: Pack
        run: dotnet pack "${{ steps.resolve.outputs.project-dir }}" --configuration Release --output ./artifacts

      - uses: actions/upload-artifact@v7
        with:
          name: nuget-package
          path: ./artifacts/*.*nupkg

  publish:
    name: Publish to NuGet and create GitHub Release
    needs: resolve-and-pack
    runs-on: ubuntu-latest
    environment: nuget-publish
    permissions:
      id-token: write
      contents: write
    steps:
      - uses: actions/download-artifact@v7
        with:
          name: nuget-package
          path: ./artifacts

      - name: NuGet login (OIDC -> temp API key)
        uses: NuGet/login@v1
        id: login
        with:
          user: ${{ secrets.NUGET_USER }}

      - name: Push to NuGet.org
        run: |
          set -euo pipefail
          for f in ./artifacts/*.nupkg; do
            dotnet nuget push "$f" --api-key "${{ steps.login.outputs.NUGET_API_KEY }}" --source https://api.nuget.org/v3/index.json --skip-duplicate
          done

      - name: Create GitHub Release
        env:
          GH_TOKEN: ${{ github.token }}
        run: |
          gh release create "${{ github.ref_name }}" ./artifacts/*.nupkg ./artifacts/*.snupkg \
            --title "${{ needs.resolve-and-pack.outputs.title }} v${{ needs.resolve-and-pack.outputs.version }}" \
            --generate-notes
```

- [ ] **Step 2: Verify the YAML is syntactically valid**

```bash
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/release.yml'))" && echo "VALID"
```

Expected: `VALID` printed, no exception.

- [ ] **Step 3: Dry-run the package-resolution logic locally**

The `resolve-and-pack` job's core logic is plain bash and can be verified without pushing anything or running GitHub Actions. From the repo root:

```bash
GITHUB_REF_NAME="googlesheets-v1.0.0"
TAG="${GITHUB_REF_NAME}"

if [[ "$TAG" =~ ^(.+-v)([0-9]+\.[0-9]+\.[0-9]+.*)$ ]]; then
  PREFIX="${BASH_REMATCH[1]}"
  VERSION="${BASH_REMATCH[2]}"
else
  echo "Tag '$TAG' does not match the expected <prefix>-v<semver> format" >&2
  exit 1
fi

MATCHES=$(grep -rl "<MinVerTagPrefix>${PREFIX}</MinVerTagPrefix>" --include="Directory.Build.props" .)
COUNT=$(printf '%s\n' "$MATCHES" | grep -c .)

echo "PREFIX=$PREFIX"
echo "VERSION=$VERSION"
echo "MATCHES=$MATCHES"
echo "COUNT=$COUNT"
```

Expected output:
```
PREFIX=googlesheets-v
VERSION=1.0.0
MATCHES=./Umbraco.Community.Automate.GoogleSheets/Directory.Build.props
COUNT=1
```

- [ ] **Step 4: Verify the title extraction**

```bash
grep -ohP '(?<=<Title>).*(?=</Title>)' Umbraco.Community.Automate.GoogleSheets/*.csproj | head -1
```

Expected: `Umbraco Community Automate Google Sheets`

- [ ] **Step 5: Verify a non-matching tag correctly fails resolution**

```bash
GITHUB_REF_NAME="doesnotexist-v1.0.0"
TAG="${GITHUB_REF_NAME}"
if [[ "$TAG" =~ ^(.+-v)([0-9]+\.[0-9]+\.[0-9]+.*)$ ]]; then
  PREFIX="${BASH_REMATCH[1]}"
fi
MATCHES=$(grep -rl "<MinVerTagPrefix>${PREFIX}</MinVerTagPrefix>" --include="Directory.Build.props" . || true)
COUNT=$(printf '%s\n' "$MATCHES" | grep -c . || true)
echo "COUNT=$COUNT"
```

Expected: `COUNT=0` — confirming an unrecognized tag prefix does not silently match the wrong package.

- [ ] **Step 6: Commit**

```bash
git add .github/workflows/release.yml
git commit -m "add(ci): tag-triggered release workflow — test, pack, approval gate, Trusted Publishing, GitHub Release"
```

---

### Task 4: Document the release flow in CONTRIBUTING.md

**Files:**
- Modify: `CONTRIBUTING.md`

- [ ] **Step 1: Add a "Releasing a package" section**

Open `CONTRIBUTING.md` and add this new section before the final `## License` section:

```markdown
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
```

- [ ] **Step 2: Verify the doc reads coherently**

Read the full file top to bottom and confirm the new section doesn't duplicate or contradict the existing "Making a change" section's branch/commit conventions, and that it sits before `## License`.

- [ ] **Step 3: Commit**

```bash
git add CONTRIBUTING.md
git commit -m "docs: document the MinVer versioning scheme and release flow in CONTRIBUTING.md"
```

---

### Task 5: One-time manual setup (GitHub Environment + NuGet Trusted Publishing)

This task is **not fully Claude-executable** — it requires input and actions only the repo owner can provide/perform. Do not attempt the NuGet.org portion or push a real release tag without explicit instruction from the user.

**Files:** none (repo/service configuration, not code)

- [ ] **Step 1: Ask the user which GitHub username(s) should be required reviewers**

The `nuget-publish` GitHub Environment (referenced by `release.yml`'s `publish` job) needs at least one required reviewer configured before any release can complete. Ask the user for the GitHub username(s) to use — do not guess or default to the repo owner without confirming.

- [ ] **Step 2: Create the environment via `gh api` (Claude-executable, once reviewer usernames are known)**

Resolve each reviewer's numeric GitHub user ID:

```bash
gh api users/<username> --jq .id
```

Then create the environment with those ID(s) as required reviewers (repeat the reviewer object per ID for multiple reviewers):

```bash
gh api --method PUT repos/umbraco-community/Umbraco.Community.Automate/environments/nuget-publish \
  --input - <<EOF
{
  "reviewers": [
    { "type": "User", "id": <RESOLVED_USER_ID> }
  ]
}
EOF
```

Verify it was created:

```bash
gh api repos/umbraco-community/Umbraco.Community.Automate/environments/nuget-publish
```

Expected: JSON response showing the environment with the configured `protection_rules` including the required reviewer(s).

- [ ] **Step 3: Register NuGet Trusted Publishing (manual, user must do this on nuget.org)**

This has no CLI or API — it must be done by the user in the nuget.org web UI, on the package's "Manage Package" → "Trusted Publishing" settings, scoped to:
- Repository: `umbraco-community/Umbraco.Community.Automate`
- Workflow file: `.github/workflows/release.yml`
- Environment: `nuget-publish`

**Known open question, verify at this step**: `Umbraco.Community.Automate.GoogleSheets` has never been published before. Check whether nuget.org allows registering a Trusted Publishing policy against a package ID that doesn't exist yet. If it does not, the user will need to do one initial manual `dotnet nuget push` with a personal API key to claim the package ID first, then immediately configure Trusted Publishing for all subsequent releases, then revoke that personal API key.

- [ ] **Step 4: Also set the `NUGET_USER` repository secret**

`release.yml`'s `NuGet/login@v1` step reads `${{ secrets.NUGET_USER }}` for the nuget.org account/profile name (not email) associated with the Trusted Publishing policy. The user must set this themselves via the GitHub UI (Settings → Secrets and variables → Actions) or:

```bash
gh secret set NUGET_USER --repo umbraco-community/Umbraco.Community.Automate
```

This prompts for the value interactively — do not pass the value as a plain CLI argument (e.g. `--body <value>`), since that would leave it in shell history.

- [ ] **Step 5: First real release (manual, user-triggered only)**

Once Steps 1–4 are complete and the user is ready, the first real release is cut by the user (or by Claude, only with explicit, unambiguous instruction to do so in that moment — never proactively) pushing the actual `googlesheets-v1.0.0` tag:

```bash
git tag googlesheets-v1.0.0
git push origin googlesheets-v1.0.0
```

Then watch the workflow run (`gh run watch` or the Actions tab), approve the `nuget-publish` environment gate when prompted, and confirm afterward: the package appears on nuget.org, and the GitHub Release has the correct title, auto-generated notes, and both `.nupkg`/`.snupkg` attached.
