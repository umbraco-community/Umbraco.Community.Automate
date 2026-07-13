# Secret Leak Prevention Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent real secrets (specifically Google OAuth `ClientId`/`ClientSecret`) from ever being committed to this repo, via a structural fix (move real credentials out of the tracked file entirely) plus two local git-hook layers that catch anything anyway.

**Architecture:** Real OAuth credentials move to .NET User Secrets (stored outside the repo). Lefthook (installed via a new root-level `package.json`) wires up `pre-commit` and `pre-push` git hooks that run `gitleaks` against staged/outgoing changes using a repo-specific `.gitleaks.toml` ruleset. Two `SetupRepo` scripts (bash + PowerShell) give contributors a one-command setup path with friendly prerequisite checks.

**Tech Stack:** .NET user-secrets CLI, npm, Lefthook, gitleaks (CLI, not the GitHub Action wrapper), bash, PowerShell.

## Global Constraints

- No CI/server-side scanning layer, and no `gitleaks/gitleaks-action` wrapper — deliberately out of scope per the design.
- `Umbraco.Community.Automate.Demo/appsettings.Development.json`'s tracked placeholder values (`"e2e-test"` for `ClientId`/`ClientSecret`) must not change — CI needs a non-empty string there.
- **Every commit in every task must stage files explicitly by path — never `git add -A` or `git add .`.** There is an uncommitted, untracked local modification to `Umbraco.Community.Automate.Demo/appsettings.Development.json` on this branch containing real Google OAuth credentials the repo owner uses for manual testing. It must never be staged or committed by any step in this plan.
- Root-level `package.json` is deliberately separate from `Umbraco.Community.Automate.GoogleSheets/Client/package.json` (repo-wide tooling vs. one package's frontend).
- `SetupRepo.sh`/`SetupRepo.ps1` stay narrow in scope: prerequisite checks + `npm install` + `npx lefthook install`. Not a general bootstrap script.

**Deliberate deviation from the design spec, flagged here for visibility:** the spec's `SetupRepo.sh`/`SetupRepo.ps1` only check for Node.js before running `npm install`/`npx lefthook install`. `npm install` only installs Lefthook itself — it does not install `gitleaks`, which is a standalone Go binary with no npm distribution. Without a Node **and** gitleaks check, a contributor could run the setup script successfully, believe hooks are active, then have their very first commit fail with a cryptic "gitleaks: command not found" instead of the friendly failure the whole design is about. Task 3 below adds a second prerequisite check for `gitleaks`, structurally identical to the Node check (same `command -v`/`Get-Command` pattern), with an install pointer per platform. This is a narrow, same-shape addition to the already-agreed "check prerequisites, fail with a clear message if missing" pattern — not new scope.

---

### Task 1: Move real OAuth credentials to .NET User Secrets

**Files:**
- Modify: `Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj`

**Interfaces:**
- Produces: a `UserSecretsId` GUID in the csproj, which later tasks and future contributors rely on being present for `dotnet user-secrets` commands to work against this project without needing `--id` passed explicitly.

- [ ] **Step 1: Initialize User Secrets for the Demo project**

```bash
dotnet user-secrets init --project Umbraco.Community.Automate.Demo
```

Expected output: `Set UserSecretsId to '<some-guid>' for MSBuild project 'Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj'.`

- [ ] **Step 2: Verify the csproj was modified correctly**

```bash
git diff Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj
```

Expected: a new `<UserSecretsId>{guid}</UserSecretsId>` line added inside the first `<PropertyGroup>` (alongside `TargetFramework`, `ImplicitUsings`, etc.). No other lines changed.

- [ ] **Step 3: Verify User Secrets actually works and overrides the tracked placeholder**

```bash
dotnet user-secrets set "Umbraco:Automate:Providers:GoogleSheets:ClientId" "plan-verification-test-value" --project Umbraco.Community.Automate.Demo
dotnet user-secrets list --project Umbraco.Community.Automate.Demo
```

Expected: the list output includes `Umbraco:Automate:Providers:GoogleSheets:ClientId = plan-verification-test-value`.

- [ ] **Step 4: Confirm the tracked file is untouched**

```bash
git status Umbraco.Community.Automate.Demo/appsettings.Development.json
```

Expected: this file does NOT appear as newly modified by Steps 1-3 (it may still show as modified from the pre-existing, unrelated local credential change already on this machine — that's expected and must be left alone; the point of this check is that Steps 1-3 added zero *additional* changes to it).

- [ ] **Step 5: Clean up the verification secret**

```bash
dotnet user-secrets remove "Umbraco:Automate:Providers:GoogleSheets:ClientId" --project Umbraco.Community.Automate.Demo
dotnet user-secrets list --project Umbraco.Community.Automate.Demo
```

Expected: list output is now empty (`No secrets configured for this application.`) — this was a verification-only value, not meant to persist.

- [ ] **Step 6: Verify the project still builds**

```bash
dotnet build Umbraco.Community.Automate.Demo
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 7: Commit**

```bash
git add Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj
git commit -m "chore(demo): initialize User Secrets so real OAuth credentials never need to go in appsettings.Development.json"
```

---

### Task 2: Hook tooling — Lefthook + gitleaks configuration

**Files:**
- Create: `package.json`
- Create: `.gitleaks.toml`
- Create: `lefthook.yml`
- Create: `.gitignore` entries for `node_modules/` at the repo root (verify this isn't already covered — see Step 1)

**Interfaces:**
- Produces: working `pre-commit` and `pre-push` git hooks in this local checkout, which Task 3's `SetupRepo` scripts install for every other contributor via `npx lefthook install`.

- [ ] **Step 1: Check whether root-level `node_modules/` is already gitignored**

```bash
grep -n "^node_modules" .gitignore | head -5
```

If this prints at least one match, `node_modules/` at any depth (including the repo root, once Step 2 creates a root `package.json`) is already ignored — skip to Step 3. If it prints nothing, proceed to Step 2 to add it.

- [ ] **Step 2 (only if Step 1 found no match): Add `node_modules/` to `.gitignore`**

Add this line to `.gitignore` (anywhere in the file; check existing structure first with `head -20 .gitignore` and place it near any other `node_modules` or build-output entries for consistency):

```
node_modules/
```

- [ ] **Step 3: Create the root-level `package.json`**

Create `package.json`:

```json
{
  "name": "umbraco-community-automate-tooling",
  "private": true,
  "devDependencies": {
    "lefthook": "^1"
  }
}
```

- [ ] **Step 4: Create `.gitleaks.toml`**

Create `.gitleaks.toml`:

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

- [ ] **Step 5: Create `lefthook.yml`**

Create `lefthook.yml`:

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

- [ ] **Step 6: Install Lefthook and wire up the hooks in this checkout**

```bash
npm install
npx lefthook install
```

Expected: `npm install` creates `node_modules/` and `package-lock.json`. `npx lefthook install` prints something like `sync hooks: [pre-commit pre-push]` and creates `.git/hooks/pre-commit` and `.git/hooks/pre-push`.

- [ ] **Step 7: Verify the pre-commit hook blocks a synthetic secret**

```bash
python3 -c "print('const leaked = \"GOCSPX-' + 'A' * 28 + '\";')" > gitleaks-verify-test.js
git add gitleaks-verify-test.js
git commit -m "test: this commit must be blocked by gitleaks"
```

Expected: the commit FAILS. gitleaks' output should show a finding for `google-oauth-client-secret` in `gitleaks-verify-test.js`, and the commit does not get created (`git log -1` afterward should NOT show this test commit).

- [ ] **Step 8: Clean up the test file**

```bash
git reset HEAD gitleaks-verify-test.js
rm gitleaks-verify-test.js
git status
```

Expected: `gitleaks-verify-test.js` no longer appears in `git status` (neither staged nor untracked).

- [ ] **Step 9: Verify the pre-push hook blocks a secret that bypassed pre-commit**

This confirms the second local layer catches what `git commit --no-verify` lets through. Uses a genuinely local, throwaway bare repo as a scratch "remote" — this never touches the real GitHub origin.

```bash
git init --bare /tmp/gitleaks-verify-remote.git
git remote add gitleaks-verify-remote /tmp/gitleaks-verify-remote.git

python3 -c "print('const leaked = \"GOCSPX-' + 'B' * 28 + '\";')" > gitleaks-verify-test.js
git add gitleaks-verify-test.js
git commit --no-verify -m "test: bypasses pre-commit, pre-push must still catch this"

git push gitleaks-verify-remote HEAD:refs/heads/verify-test
echo "push exit code: $?"
```

Expected: `git commit --no-verify` succeeds (pre-commit is bypassed on purpose). The `git push` to the scratch remote FAILS — gitleaks' output shows the same `google-oauth-client-secret` finding, and the exit code is non-zero.

- [ ] **Step 10: Clean up the pre-push test — reset the bad commit and remove the scratch remote**

Use a **mixed** reset (not `--hard`) here. `Umbraco.Community.Automate.Demo/appsettings.Development.json` has an uncommitted, local-only working-tree modification (real OAuth credentials a contributor uses for manual testing) that must never be touched by this plan. `git reset --hard` rewrites the working tree to match `HEAD~1` and would silently discard that modification along with the test commit. `git reset` (mixed, the default) only moves HEAD and the index — it never touches the working tree — so the uncommitted modification survives untouched. The test commit's only file (`gitleaks-verify-test.js`) becomes untracked again and is removed manually, the same pattern Step 8 already uses for the pre-commit test cleanup.

```bash
git reset HEAD~1
rm gitleaks-verify-test.js
rm -rf /tmp/gitleaks-verify-remote.git
git remote remove gitleaks-verify-remote
git log -1 --oneline
git status
```

Expected: `git log -1` no longer shows the "bypasses pre-commit" test commit — you're back to the commit from before Step 9 (Task 1's commit). `git status` should show the same clean/pre-existing state as before Step 9 (the config files from Steps 3-5 are untracked, not yet committed, exactly as they were) — including `Umbraco.Community.Automate.Demo/appsettings.Development.json` still showing as locally modified, unchanged from before this step.

- [ ] **Step 11: Verify a clean commit still succeeds through the hook**

This step's own Step 12 commit IS this verification — if the hook were misconfigured to block everything, Step 12 itself would fail. No separate throwaway commit needed.

- [ ] **Step 12: Commit**

```bash
git add package.json .gitleaks.toml lefthook.yml .gitignore package-lock.json
git commit -m "add(tooling): Lefthook + gitleaks pre-commit/pre-push hooks to catch secrets before they're committed"
```

(Only add `.gitignore` to this command if Step 2 actually modified it — if Step 1 found `node_modules/` was already covered, omit `.gitignore` from this `git add`.)

- [ ] **Step 13: Confirm the commit succeeded and no test artifacts leaked into history**

```bash
git log -1 --oneline
git log --all --oneline | grep -i "gitleaks-verify\|bypasses pre-commit" || echo "clean — no test commits in history"
```

Expected: the first command shows the new commit from Step 12. The second command prints `clean — no test commits in history`, confirming neither Step 7's nor Step 9's test commits made it into real history.

---

### Task 3: `SetupRepo.sh` and `SetupRepo.ps1`

**Files:**
- Create: `SetupRepo.sh`
- Create: `SetupRepo.ps1`

**Interfaces:**
- Consumes: `package.json` and `lefthook.yml` from Task 2 (these scripts' `npm install`/`npx lefthook install` calls only work correctly once those files exist — this task must run after Task 2).

- [ ] **Step 1: Create `SetupRepo.sh`**

Create `SetupRepo.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail

if ! command -v node >/dev/null 2>&1; then
  echo "Node.js is required but not found. Install it from https://nodejs.org, then re-run this script." >&2
  exit 1
fi

if ! command -v gitleaks >/dev/null 2>&1; then
  echo "gitleaks is required but not found. Install it (e.g. 'brew install gitleaks' on macOS, 'scoop install gitleaks' or 'winget install gitleaks' on Windows, or see https://github.com/gitleaks/gitleaks#installing for other options), then re-run this script." >&2
  exit 1
fi

echo "Installing repo tooling (Lefthook)..."
npm install

echo "Installing git hooks..."
npx lefthook install

echo "Setup complete. Git hooks are now active for this clone."
```

- [ ] **Step 2: Make `SetupRepo.sh` executable**

```bash
chmod +x SetupRepo.sh
```

- [ ] **Step 3: Create `SetupRepo.ps1`**

Create `SetupRepo.ps1`:

```powershell
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "Node.js is required but not found. Install it from https://nodejs.org, then re-run this script."
    exit 1
}

if (-not (Get-Command gitleaks -ErrorAction SilentlyContinue)) {
    Write-Error "gitleaks is required but not found. Install it (e.g. 'scoop install gitleaks' or 'winget install gitleaks' on Windows, 'brew install gitleaks' on macOS, or see https://github.com/gitleaks/gitleaks#installing for other options), then re-run this script."
    exit 1
}

Write-Host "Installing repo tooling (Lefthook)..."
npm install

Write-Host "Installing git hooks..."
npx lefthook install

Write-Host "Setup complete. Git hooks are now active for this clone."
```

- [ ] **Step 4: Verify the happy path — `SetupRepo.sh` succeeds when both prerequisites are present**

```bash
./SetupRepo.sh
```

Expected: prints `Installing repo tooling (Lefthook)...`, `Installing git hooks...`, `Setup complete. Git hooks are now active for this clone.` and exits 0 (`echo $?` afterward prints `0`). `npm install`/`npx lefthook install` running again on an already-set-up checkout is expected to be a fast no-op-ish re-sync, not an error.

- [ ] **Step 5: Verify the Node-missing failure path with a real empty PATH**

```bash
env -i PATH=/usr/bin:/bin bash -c 'cd "$(pwd)" && ./SetupRepo.sh' ; echo "exit code: $?"
```

Run this from the repo root. Expected: prints exactly the friendly message `Node.js is required but not found. Install it from https://nodejs.org, then re-run this script.` to stderr (not a raw `node: command not found` or a bash stack trace), and `exit code: 1`.

If your system happens to have a `node` binary reachable from `/usr/bin` or `/bin` directly (uncommon, but possible on some Linux distros with a system-packaged Node), this test will instead proceed past the Node check — if so, note this in your report rather than treating it as a failure, and rely on Step 4 (happy path) plus the gitleaks-missing test below as sufficient coverage.

- [ ] **Step 6: Verify the gitleaks-missing failure path without touching real PATH state**

This tests the same `if ! command -v X` failure-branch logic proven in Step 5, for the second check, without risky system PATH manipulation. Create a temporary copy of the script with the binary name swapped to one guaranteed not to exist:

```bash
sed 's/command -v gitleaks/command -v gitleaks-does-not-exist-verification-check/' SetupRepo.sh > /tmp/SetupRepo-gitleaks-check-test.sh
chmod +x /tmp/SetupRepo-gitleaks-check-test.sh
/tmp/SetupRepo-gitleaks-check-test.sh ; echo "exit code: $?"
rm /tmp/SetupRepo-gitleaks-check-test.sh
```

Expected: since Node is genuinely present (Step 4 already confirmed this), the script passes the Node check, then fails on the swapped-in nonexistent-binary check, printing the friendly gitleaks-not-found message and exiting 1.

- [ ] **Step 7: Review (and test, if PowerShell Core is available) `SetupRepo.ps1`**

```bash
which pwsh || echo "pwsh not available on this machine"
```

If `pwsh` is available, run the equivalent checks to Steps 4-6 using `pwsh -File SetupRepo.ps1` instead of `./SetupRepo.sh`, and the PowerShell equivalents of the PATH/binary-swap tricks (`$env:PATH`, and swapping `gitleaks` for a nonexistent command name in a temp copy of the `.ps1`). If `pwsh` is not available on this machine, skip live execution and instead do a careful manual read-through confirming: the `Get-Command ... -ErrorAction SilentlyContinue` + `if (-not (...))` pattern is valid PowerShell syntax, the `Write-Error`/`exit 1` pair matches the bash script's behavior (message to stderr, non-zero exit), and the two scripts are logically equivalent step-for-step. State explicitly in your report whether this was live-tested or manually reviewed only.

- [ ] **Step 8: Commit**

```bash
git add SetupRepo.sh SetupRepo.ps1
git commit -m "add(tooling): SetupRepo.sh/SetupRepo.ps1 — one-command contributor setup with friendly prerequisite checks"
```

Note: `git add` preserves the executable bit set in Step 2 as part of the file's mode in the commit — no separate action needed to make that stick.

---

### Task 4: Document secret-leak prevention in CONTRIBUTING.md

**Files:**
- Modify: `CONTRIBUTING.md`

- [ ] **Step 1: Add a "Preventing secret leaks" section**

Open `CONTRIBUTING.md` and add this new section after the existing "## Making a change" section and before "## Releasing a package" (placed here since it's setup guidance contributors need before their first commit, right alongside the branch/commit conventions). The block below uses 4 backticks as the outer fence specifically because the content itself contains nested 3-backtick `bash`/`powershell` code blocks — copy everything between the outer ```` ```` ```` markers verbatim, including the nested fences, into `CONTRIBUTING.md`:

````markdown
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
````

- [ ] **Step 2: Verify the doc reads coherently**

Read the full `CONTRIBUTING.md` top to bottom and confirm the new section fits between "Making a change" and "Releasing a package" without duplicating either, and that the fenced code blocks render correctly (the bash/PowerShell pair for the setup-script step should appear as two separate, clearly-labeled blocks, not run together).

- [ ] **Step 3: Commit**

```bash
git add CONTRIBUTING.md
git commit -m "docs: document secret-leak prevention (User Secrets, SetupRepo, what a blocked commit looks like)"
```
