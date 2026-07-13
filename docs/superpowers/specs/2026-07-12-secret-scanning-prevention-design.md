# Secret Leak Prevention Design

## Context

While reviewing the Umbraco.Community.Automate repo's git history (as part of investigating a security note raised during a prior code review), a real Google OAuth `ClientId`/`ClientSecret` pair was found to have been staged and gotten as far as a local `git stash` entry on 2026-06-27, before being caught. Investigation (`git ls-remote`, GitHub's commit API, GitHub's commit search API) confirmed this specific incident never reached the public GitHub remote — but the near-miss exposed a real gap: `Umbraco.Community.Automate.Demo/appsettings.Development.json` is tracked in git with placeholder values (`"e2e-test"` for `ClientId`/`ClientSecret`, needed so CI's OpenIddict registration doesn't throw), but developers need real OAuth credentials in that same file locally to manually test the actual Google OAuth consent flow — meaning every local testing session carries a live risk of a real secret ending up staged, committed, and pushed to this public repo.

This repo is a monorepo intended to hold more provider packages over time, each potentially needing its own OAuth-style connection type — so this exact risk class will likely recur per-package, not just for Google Sheets. The fix needs to be structural (reduce how often a real secret is ever near a tracked file) and layered locally (assume the first layer can be bypassed or skipped).

## Approach

Two layers, both local — deliberately no CI/server-side layer (see "Explicitly out of scope"):

1. **Remove the opportunity** — real credentials should never need to go into a git-tracked file at all.
2. **Catch it locally, before a commit exists** — a pre-commit hook scans staged changes; a pre-push hook re-scans outgoing commits as a second local check.

## 1. Structural fix — .NET User Secrets

Add a `<UserSecretsId>` (a generated GUID) to `Umbraco.Community.Automate.Demo.csproj`. ASP.NET Core's `WebApplication.CreateBuilder(args)` (confirmed already in use in this project's `Program.cs`) automatically wires up the User Secrets configuration provider in the Development environment once `UserSecretsId` is present — no other code change needed.

Verified against current ASP.NET Core documentation: default configuration source precedence (lowest to highest) is `appsettings.json` → `appsettings.{Environment}.json` → **User Secrets (Development only)** → environment variables → command line. So a real credential set via:

```bash
dotnet user-secrets set "Umbraco:Automate:Providers:GoogleSheets:ClientId" "<real-client-id>" --project Umbraco.Community.Automate.Demo
dotnet user-secrets set "Umbraco:Automate:Providers:GoogleSheets:ClientSecret" "<real-client-secret>" --project Umbraco.Community.Automate.Demo
```

is stored in a per-user, per-project file under the developer's home directory (`~/.microsoft/usersecrets/<guid>/secrets.json` on macOS/Linux, `%APPDATA%\Microsoft\UserSecrets\<guid>\secrets.json` on Windows) — entirely outside the repository — and transparently overrides the checked-in `"e2e-test"` value at runtime for that key. The tracked `appsettings.Development.json` does not change: it keeps its current placeholder values, since CI still needs a non-empty string there (per the original reason those placeholders were added — OpenIddict throws on an empty `ClientId`/`ClientSecret` during registration).

This doesn't make a leak impossible (a developer could still, in theory, paste a real value into the tracked file instead of using User Secrets) — it removes the *reason* to ever do so, since User Secrets is no harder to use and is the ASP.NET Core-idiomatic answer to exactly this problem.

## 2. Local hooks — Lefthook + gitleaks, distributed via npm

**Why Lefthook:** it's a compiled Go binary (fast, no interpreter startup cost), genuinely cross-platform (Windows/macOS/Linux behave identically), and is installed via `npm install` — which is not a new dependency class for this repo, since `Umbraco.Community.Automate.GoogleSheets/Client` already requires Node for its property-editor frontend and Playwright tests. Verified there is no `.NET`/NuGet-native distribution for Lefthook (checked current install docs) — npm, gem, pipx, Go, and OS package managers (Homebrew/winget/apt/scoop) are the real options — so npm is the lowest-friction choice given this repo's existing tooling.

**Why gitleaks:** verified current GitHub activity across the realistic options (gitleaks, trufflehog, betterleaks, detect-secrets) via the GitHub API rather than assuming from memory:

| Tool | Stars | Latest release | Last commit |
|---|---|---|---|
| gitleaks | 28.1k | ~4 months old | 4 days ago |
| trufflehog | 27k | 3 days old | today |
| betterleaks | 1.4k | 12 days old | 2 days ago |
| detect-secrets | 4.6k | **May 2024 — stale** | ~3 months ago |

`detect-secrets` was ruled out on staleness. `trufflehog` is the most actively released and adds live credential verification (confirms a found secret is still valid by calling the real provider API) but requires network access during the scan — a poor fit for a pre-commit hook, which should stay fast and work offline. `betterleaks` is promising (gitleaks-compatible config format, growing ecosystem) but only 5 months old — too new to be the sole line of defense for a public repo today. **gitleaks** is the most battle-tested tool built specifically for this exact use case, has no network dependency, and has the widest existing integration prior art (pre-commit-framework hooks, CI actions, editor plugins already exist for it).

**Root-level `package.json`** (new — deliberately separate from `GoogleSheets/Client/package.json`, since this is repo-wide tooling, not one package's frontend concern):

```json
{
  "name": "umbraco-community-automate-tooling",
  "private": true,
  "devDependencies": {
    "lefthook": "^1"
  }
}
```

**`lefthook.yml`** (new, repo root):

```yaml
pre-commit:
  commands:
    gitleaks:
      run: gitleaks protect --staged --config .gitleaks.toml --verbose

pre-push:
  commands:
    gitleaks:
      run: gitleaks protect --config .gitleaks.toml --verbose
```

`pre-commit` scans only staged changes (`--staged`) — the earliest possible point, before a commit object exists at all.

`pre-push` is a second local layer intended to still catch anything committed with `git commit --no-verify`. The mechanism this actually requires is `gitleaks detect --log-opts=<range>`, not `gitleaks protect`: `gitleaks protect` (with or without `--staged`) only ever scans the uncommitted working-tree diff, never git history, so it cannot see anything that's already been committed — a bypassed pre-commit hook would sail straight through it. The pre-push hook instead needs to read git's native pre-push stdin protocol (one line per ref being pushed: `<local ref> <local sha1> <remote ref> <remote sha1>`) and run `gitleaks detect --log-opts=<range>` against the actual outgoing commit range (`<remote sha1>..<local sha1>` when the remote already has the ref, falling back through the remote's default branch and finally a whole-history scan for a brand-new ref with no resolvable default branch). This is implemented as a standalone script, `.lefthook/pre-push/gitleaks-scan-range.sh` (referenced via `scripts:` in `lefthook.yml`), rather than an inline `commands:` run string — not because `commands:` can't receive stdin (it can, via `use_stdin: true`), but because the branching logic involved (resolving the default branch, computing commit ranges, handling brand-new vs. existing remote branches) is far more readable, testable, and diffable as its own shell script than crammed into a YAML `run: |` block. Both hooks also pass `--redact` so a caught finding's rule/file/line are reported without ever printing the raw secret value to the console or logs.

**`.gitleaks.toml`** (new, repo root) — extends gitleaks' default ruleset (`[extend] useDefault = true`) with an explicit rule guaranteeing this exact incident class is caught regardless of default-ruleset coverage:

```toml
[extend]
useDefault = true

[[rules]]
id = "google-oauth-client-secret"
description = "Google OAuth 2.0 Client Secret"
regex = '''GOCSPX-[A-Za-z0-9_-]{28}'''
tags = ["google", "oauth", "secret"]

[[rules]]
id = "google-oauth-client-id"
description = "Google OAuth 2.0 Client ID"
regex = '''\d{10,}-[a-z0-9]{32}\.apps\.googleusercontent\.com'''
tags = ["google", "oauth", "client-id"]
```

**One-time contributor setup:** rather than documenting `npm install && npx lefthook install` as manual steps, ship a setup script per platform so the failure mode for a missing prerequisite is a clear message instead of a cryptic `npm: command not found`.

## 3. Setup scripts — `SetupRepo.sh` / `SetupRepo.ps1`

Both at the repo root (maximally discoverable — visible alongside `README.md`/`CONTRIBUTING.md` on first clone, no need to know a `scripts/` folder exists). Deliberately narrow scope: check Node.js is present, then run the two setup commands. Not a general bootstrap script — no `dotnet restore`, no cert trust, nothing beyond what this design needs.

**`SetupRepo.sh`** (macOS/Linux):

```bash
#!/usr/bin/env bash
set -euo pipefail

if ! command -v node >/dev/null 2>&1; then
  echo "Node.js is required but not found. Install it from https://nodejs.org, then re-run this script." >&2
  exit 1
fi

echo "Installing repo tooling (Lefthook)..."
npm install

echo "Installing git hooks..."
npx lefthook install

echo "Setup complete. Git hooks are now active for this clone."
```

**`SetupRepo.ps1`** (Windows):

```powershell
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "Node.js is required but not found. Install it from https://nodejs.org, then re-run this script."
    exit 1
}

Write-Host "Installing repo tooling (Lefthook)..."
npm install

Write-Host "Installing git hooks..."
npx lefthook install

Write-Host "Setup complete. Git hooks are now active for this clone."
```

`SetupRepo.sh` needs its executable bit set (`chmod +x SetupRepo.sh`) when committed, so `./SetupRepo.sh` works without a contributor needing to know to `chmod` it themselves first.

## 4. Documentation

New `CONTRIBUTING.md` section (placed near the existing "Building and testing" section, since it's setup guidance contributors need before their first commit):

- Why real OAuth credentials must never go into `appsettings.Development.json`, and the `dotnet user-secrets set` commands to use instead.
- The one-time setup step: run `./SetupRepo.sh` (macOS/Linux) or `.\SetupRepo.ps1` (Windows).
- What a caught-secret error looks like (gitleaks' console output blocking the commit/push) and how to resolve it (unstage the offending change, move the real value to User Secrets instead, re-commit).

## Explicitly out of scope

- Cleaning up the existing local git stash or the current uncommitted `appsettings.Development.json` working-tree change — user has explicitly said to leave both alone; this design is prevention-only.
- Any git history rewrite (the stash never reached the public remote, so there is nothing in the *pushed* history to scrub — see Context above).
- A CI/server-side scanning layer — deliberately not included. The two local layers (pre-commit, pre-push) are considered sufficient; a CI check that's expected to essentially never fire, given those two layers, was judged not worth the added workflow to maintain. Can be revisited later if the local hooks ever prove insufficient in practice.
- trufflehog, betterleaks, or the `gitleaks/gitleaks-action` wrapper — considered and explicitly not chosen (the latter specifically evaluated for the now-dropped CI layer), with reasoning captured in earlier design discussion so a future revisit doesn't re-litigate from scratch.

## Verification plan (for the implementation phase)

- Confirm `dotnet user-secrets set`/`dotnet user-secrets list` work against the Demo project once `UserSecretsId` is added, and that a value set this way is visible in the running app's configuration while `appsettings.Development.json` itself is untouched.
- Confirm the pre-commit hook actually blocks a commit: stage a file containing a synthetic string matching the custom `GOCSPX-` rule (not a real secret), attempt `git commit`, confirm it's rejected; then unstage/remove it and confirm a normal commit succeeds.
- Confirm the pre-push hook behaves the same way for a commit made with `--no-verify` (bypassing pre-commit) that's then `git push`ed.
- Confirm `./SetupRepo.sh` on macOS/Linux is a complete, working setup path on a fresh clone (no other manual steps required), and that it fails with the friendly message (not a stack trace) when Node isn't on `PATH`.
- Confirm `SetupRepo.ps1` does the same on Windows (or via PowerShell Core on macOS/Linux as a syntax/logic check, if a Windows machine isn't available to test on directly).

## Post-implementation notes

A few details emerged during implementation that this design didn't anticipate. Rather than rewriting the sections above to pretend they were foreseen, they're logged here:

- **Pre-push mechanism redesign.** The original `lefthook.yml` sample in "2. Local hooks" (`gitleaks protect` for both hooks) turned out not to work for pre-push: `gitleaks protect` only ever scans the uncommitted working-tree diff, not git history, so it never sees a commit made with `git commit --no-verify` — the exact case pre-push exists to catch. The corrected mechanism (`gitleaks detect --log-opts=<range>` driven by git's pre-push stdin protocol, implemented as `.lefthook/pre-push/gitleaks-scan-range.sh`) is described in section 2 above; see that script's own header comment for the full reasoning.
- **`--redact` added to both hooks.** Not in the original design; added after a verification run showed an unredacted `--verbose` finding printing a real secret value into tool/terminal output. Now on both `pre-commit` and pre-push.
- **Gitleaks prerequisite check in the `SetupRepo` scripts.** The original scripts (section 3) only checked for Node.js. In practice `npx lefthook install` wires up the hooks but doesn't install gitleaks itself, so a `gitleaks` binary check/friendly-error was added alongside the Node.js check.
- **CONTRIBUTING.md section placement.** Landed under "Building and testing" as planned, but interleaved with the existing prerequisites content rather than as one standalone block, since contributors read setup steps linearly and the two topics overlap there.
- **Known, accepted pre-existing secret allowlisted.** `Umbraco.Community.Automate.Demo/appsettings.json`'s `Imaging:HMACSecretKey` (committed `be847121`, already public on `origin/main`) predates this design and was deliberately not rotated or scrubbed from history — the demo site it belongs to is local-only dev scaffolding, never deployed publicly. Without an allowlist entry, the pre-push hook's whole-history fallback (used when no default-branch remote-tracking ref can be resolved at all) would hard-block on this known finding with no way for a contributor to remediate it. `.gitleaks.toml` now has a narrowly-scoped `[[allowlists]]` entry (commit + path + rule ID, `condition = "AND"`) covering only this one finding.
