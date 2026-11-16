# Claude Skill: rename and update — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port Paul's `.claude/skills/umbraco-automate/` skill into this repo as `.claude/skills/umbraco-community-automate/`, updated for everything the repo breakup changed underneath it, with PR #46's Kitchen Sink example reconciled into a CI-checked skill fixture instead of a `Packages/` example.

**Architecture:** One branch, one PR, small sequential commits. Each commit is independently buildable/testable where it touches code; doc-only commits are independently readable. The branch forks from `feat/demo-test-connection-automations` (PR #48's branch — the tip that actually has the Demo-setup code this skill needs to describe).

**Tech Stack:** Markdown (skill + references + rules), bash (generation/CI scripts matching `tools/test-umbraco-compat.sh`'s style), the existing .NET 10 / xUnit package conventions for the fixture.

**Spec:** `docs/superpowers/specs/2026-11-01-claude-skill-rename-and-update-design.md` — this plan implements its 8 Decisions; read it alongside this plan.

## Global Constraints

- Branch name: `feat/claude-skill-rename-and-update`, created from `origin/feat/demo-test-connection-automations` (fetch first; if `feat/repo-overhaul` has absorbed #47/#48 by the time this starts, branch from `feat/repo-overhaul` instead — check `git log --oneline feat/repo-overhaul..origin/feat/demo-test-connection-automations` is empty before switching base).
- Skill root: `.claude/skills/umbraco-community-automate/` (was `umbraco-automate` on the `-pr32` reference worktree at `/Users/warren/Code/Personal/Umbraco.Community.Automate-pr32`, which is **read-only reference material**, never edited).
- Fixture root: `.claude/skills/umbraco-community-automate/fixtures/kitchen-sink/` — no `Directory.Build.props`, never packed, never added to `Umbraco.Community.Automate.slnx` or the Demo's `.csproj`.
- `simple.md` is generated from `Packages/WeatherApi/Umbraco.Community.Automate.WeatherApi/` (a real, already-shipped package) — no second fixture.
- Every package convention already documented in the ported `SKILL.md` (aliases permanent once released, `community.` prefix, xUnit only, `string?` for optional settings, etc.) carries over unchanged except where a task below explicitly updates it.
- Close PR #46 (`feat/kitchensink-example`) once this branch supersedes it; don't merge it.
- Commit trailer on every commit: `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`. The Kitchen Sink fixture commit additionally gets `Co-authored-by: suedeapple <paul@suedeapple.co.uk>` (it substantially reproduces Paul's #46 work).

## Review Focus

- **A contributor runs the generation script against a markdown file whose code-block count doesn't match its marker count** (a hand-edited reference doc drifts out of sync with its own markers) — the script must fail loudly naming the mismatch, not silently skip a block or write past the end of the file.
- **CI's existing package-discovery (`find . -mindepth 4 -maxdepth 4 -name Directory.Build.props`) must not pick up the fixture** after it moves under `.claude/skills/.../fixtures/kitchen-sink/` — Task 2 verifies the new path's depth doesn't match, and that no `Directory.Build.props` remains there.
- **`full.md`/`simple.md`'s `--check` CI step must actually fail on drift**, not just on missing files — Task 6 verifies the check fails when a fixture/WeatherApi source file is edited without regenerating, not only when a file is deleted.
- **The new `.claude/rules/` files must not duplicate content already in `SKILL.md`** in a way that can silently diverge — Task 10 verifies by grep that each extracted invariant's wording exists in exactly one of the two places, with the other pointing at it.
- **A contributor who has never cloned `Umbraco.Automate` before** follows the tag-pinned-clone guidance (Task 7) and it still produces a usable local reference without assuming a pre-existing personal directory layout — Task 7's step verifies the instructions work from a fresh `/tmp` clone, not just from this session's existing `/Users/warren/Code/Umbraco.Automate` checkout.

---

### Task 1: Fork the branch and port + rename the skill

**Files:**
- Create: `.claude/skills/umbraco-community-automate/SKILL.md`, `.claude/skills/umbraco-community-automate/references/{fields,full,migrating,new-package,simple}.md` (copied from `-pr32`'s `.claude/skills/umbraco-automate/`)

**Interfaces:**
- Produces: the skill's final directory root (`.claude/skills/umbraco-community-automate/`) that every later task edits in place.

- [ ] **Step 1:** `git fetch origin feat/demo-test-connection-automations feat/repo-overhaul`; confirm with `git log --oneline feat/repo-overhaul..origin/feat/demo-test-connection-automations` that #47/#48 haven't merged yet (non-empty output expected); create `feat/claude-skill-rename-and-update` from `origin/feat/demo-test-connection-automations`.
- [ ] **Step 2:** Copy the five files above verbatim from the `-pr32` worktree into the new path, preserving directory structure.
- [ ] **Step 3:** In `SKILL.md`'s frontmatter, change `name: umbraco-automate` to `name: umbraco-community-automate`. Leave the `description` text as-is for now (later tasks edit its content, not this task).
- [ ] **Step 4:** Grep all six files for the literal string `umbraco-automate` outside the frontmatter `name` already changed; there should be none (Paul's doc never names the skill by its own folder name internally) — confirms no stray self-reference needs fixing.
- [ ] **Step 5:** Commit.

```bash
git add .claude/skills/umbraco-community-automate
git commit -m "feat(skill): port umbraco-automate skill, renamed to umbraco-community-automate"
```

---

### Task 2: Move the Kitchen Sink reference into the skill's fixtures

**Files:**
- Move: `Packages/_Development/KitchenSink/Umbraco.Community.Automate.Examples.KitchenSink/` → `.claude/skills/umbraco-community-automate/fixtures/kitchen-sink/Umbraco.Community.Automate.Examples.KitchenSink/`
- Move: `Packages/_Development/KitchenSink/Umbraco.Community.Automate.Examples.KitchenSink.Tests/` → `.claude/skills/umbraco-community-automate/fixtures/kitchen-sink/Umbraco.Community.Automate.Examples.KitchenSink.Tests/`
- Delete: the moved project's `Directory.Build.props` (no package marker, per spec Decision 3)

**Interfaces:**
- Produces: a standalone-buildable fixture at the path above, with `dotnet test <path>/Umbraco.Community.Automate.Examples.KitchenSink.Tests` passing — Task 3 depends on this exact path.

- [ ] **Step 1:** `git checkout origin/feat/kitchensink-example -- Packages/_Development/KitchenSink` to pull PR #46's current files into the working tree at their old path.
- [ ] **Step 2:** `mkdir -p .claude/skills/umbraco-community-automate/fixtures/kitchen-sink` then `git mv` both project folders (package + `.Tests`) into it, keeping their internal structure and relative `ProjectReference` untouched.
- [ ] **Step 3:** `git rm .claude/skills/umbraco-community-automate/fixtures/kitchen-sink/Umbraco.Community.Automate.Examples.KitchenSink/Directory.Build.props`.
- [ ] **Step 4:** `rmdir Packages/_Development/KitchenSink` (now empty; leave `Packages/_Development/TestConnection/` untouched).
- [ ] **Step 5:** Verify: `dotnet test .claude/skills/umbraco-community-automate/fixtures/kitchen-sink/Umbraco.Community.Automate.Examples.KitchenSink.Tests --configuration Release`. Expected: all tests pass (34/34 per PR #46's own test plan), with no reference to the deleted `Directory.Build.props` needed.
- [ ] **Step 6:** Verify CI's existing discovery query no longer matches the new path: `find . -mindepth 4 -maxdepth 4 -name Directory.Build.props -not -path './.git/*'` must not list anything under `.claude/skills/`.
- [ ] **Step 7:** Commit (include the `suedeapple` co-author trailer, this reproduces Paul's #46 work).

```bash
git add Packages/_Development/KitchenSink .claude/skills/umbraco-community-automate/fixtures
git commit -m "feat(skill): move the Kitchen Sink reference into the skill's fixtures, out of Packages/

Co-authored-by: suedeapple <paul@suedeapple.co.uk>"
```

---

### Task 3: Build and test the fixture in CI

**Files:**
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Consumes: the fixture path from Task 2 (`.claude/skills/umbraco-community-automate/fixtures/kitchen-sink/Umbraco.Community.Automate.Examples.KitchenSink.Tests`).
- Produces: a `skill-fixtures` job whose name Task 6 extends with the diff-check step, and which the final `ci` gating job must depend on.

- [ ] **Step 1:** Add a `skill-fixtures` job to `ci.yml` (sibling to `discover`/`package`, not routed through `package-ci.yml` since the fixture is never packed or version-matrixed): checkout, `actions/setup-dotnet@v5` at `10.0.x`, then `dotnet test .claude/skills/umbraco-community-automate/fixtures/kitchen-sink/Umbraco.Community.Automate.Examples.KitchenSink.Tests --configuration Release`.
- [ ] **Step 2:** Add `skill-fixtures` to the `ci` job's `needs` list and to its result check (treat it the same as `package`: `success` passes, anything else fails).
- [ ] **Step 3:** Verify: run `dotnet test .claude/skills/umbraco-community-automate/fixtures/kitchen-sink/Umbraco.Community.Automate.Examples.KitchenSink.Tests --configuration Release` locally again to confirm the exact command the workflow now runs still passes.
- [ ] **Step 4:** Commit.

```bash
git add .github/workflows/ci.yml
git commit -m "ci: build and test the skill's fixtures separately from package discovery"
```

---

### Task 4: Generation script, with an inline-marker contract

**Files:**
- Create: `tools/generate-skill-reference.sh`

**Interfaces:**
- Consumes: nothing from earlier tasks (standalone tool).
- Produces: `tools/generate-skill-reference.sh <source-dir> <markdown-file> [--check]` — Tasks 5, 6 and 7 all call this. Exit 0 and (without `--check`) rewrites `<markdown-file>` in place; with `--check`, exits 1 and prints a diff if regenerating would change the file; exits 2 with a clear message if a marker's path doesn't exist under `<source-dir>`, or if `<markdown-file>` has a fenced code block with no marker immediately above it that this script owns (see marker format below), or a marker whose file was never matched to a block.

**Marker format:** immediately above any fenced code block the script owns, a line of the exact form `` `<relative/path/from/source-dir>` `` (a markdown "code span" containing only the path, alone on its line, directly followed by the opening ` ``` `). The script:
1. Scans `<markdown-file>` top to bottom for lines matching `^\`[^\`]+\`$` immediately followed by a fence line (` ```lang` or ` ``` `).
2. For each match, reads `<source-dir>/<path>` and replaces the fenced block's body with that file's exact contents (preserving the fence's language tag).
3. Fails per the Interfaces block above on any mismatch.
4. Leaves every other line of the markdown (headings, prose, non-matching code blocks such as the `## Files` directory-tree block, which stays hand-written) untouched.

- [ ] **Step 1:** Write the script per the contract above, in the style of `tools/test-umbraco-compat.sh` (`set -euo pipefail`, a usage check, `::group::`-free since this isn't a multi-stage CI script).
- [ ] **Step 2:** Manually test it against a throwaway two-line fixture (a scratch dir with one `.cs` file and a one-marker `.md` file) to confirm the replace, `--check`-pass, `--check`-fail-on-drift, and all three error paths each behave as specified. Do this in the scratchpad directory, not committed.
- [ ] **Step 3:** Commit.

```bash
git add tools/generate-skill-reference.sh
git commit -m "feat(skill): add the reference-doc generation script"
```

---

### Task 5: Regenerate `full.md` from the fixture, `simple.md` from WeatherApi

**Files:**
- Modify: `.claude/skills/umbraco-community-automate/references/full.md`, `.claude/skills/umbraco-community-automate/references/simple.md`

**Interfaces:**
- Consumes: `tools/generate-skill-reference.sh` (Task 4); the fixture path (Task 2); `Packages/WeatherApi/Umbraco.Community.Automate.WeatherApi/`.

- [ ] **Step 1:** In `full.md`, add one marker line (per Task 4's format) above each existing per-file fenced code block, pointing at the matching real file under `.claude/skills/umbraco-community-automate/fixtures/kitchen-sink/Umbraco.Community.Automate.Examples.KitchenSink{,.Tests}/`. Leave the `## Files` directory-tree block unmarked (hand-written, not generated). Where a heading's prose uses the placeholder names `Example`/`example` (Paul's convention for a copy-and-rename template), change it to name the fixture's real identifiers instead (`KitchenSink`/`kitchenSink`) with one added sentence near the top of the doc telling the reader to substitute their own area name for `KitchenSink` — same mechanical step as before, now anchored to real, compiled names instead of a fictitious placeholder.
- [ ] **Step 2:** Run `tools/generate-skill-reference.sh .claude/skills/umbraco-community-automate/fixtures/kitchen-sink/Umbraco.Community.Automate.Examples.KitchenSink .claude/skills/umbraco-community-automate/references/full.md` (and again with the `.Tests` project path for marker lines under it, or point every marker's relative path at the fixture's shared parent directory `.claude/skills/umbraco-community-automate/fixtures/kitchen-sink/` so one invocation covers both projects — prefer this single-parent form).
- [ ] **Step 3:** Read the regenerated `full.md` end to end: confirm prose still reads correctly with the real names substituted, and that no section lost its explanatory text (the script only touches fenced bodies).
- [ ] **Step 4:** Repeat Steps 1–3 for `simple.md`, with markers pointing at `Packages/WeatherApi/Umbraco.Community.Automate.WeatherApi/` (and `.Tests`), substituting `WeatherApi`/`weatherApi` for Paul's `Example`/`example` placeholders.
- [ ] **Step 5:** Verify: `tools/generate-skill-reference.sh <fixture-dir> .../full.md --check` and the equivalent for `simple.md` against WeatherApi both exit 0 (clean — proves Step 2/4's output matches what's committed).
- [ ] **Step 6:** Commit.

```bash
git add .claude/skills/umbraco-community-automate/references/full.md .claude/skills/umbraco-community-automate/references/simple.md
git commit -m "feat(skill): generate full.md and simple.md from real, tested code"
```

---

### Task 6: Wire the generate-and-diff check into CI

**Files:**
- Modify: `.github/workflows/ci.yml` (the `skill-fixtures` job from Task 3)

**Interfaces:**
- Consumes: `tools/generate-skill-reference.sh --check` (Task 4); both regenerated reference docs (Task 5).

- [ ] **Step 1:** Add two steps to the `skill-fixtures` job (after the `dotnet test` step): `tools/generate-skill-reference.sh .claude/skills/umbraco-community-automate/fixtures/kitchen-sink .claude/skills/umbraco-community-automate/references/full.md --check`, and `tools/generate-skill-reference.sh Packages/WeatherApi/Umbraco.Community.Automate.WeatherApi .claude/skills/umbraco-community-automate/references/simple.md --check`.
- [ ] **Step 2:** Rename the job from `skill-fixtures` to something that still reads correctly now it also covers a real package's doc (e.g. keep `skill-fixtures` as the job id but note in a comment that the second check isn't fixture-specific), or split the diff-checks into their own job — either is fine; pick one and keep the `ci` gating job's `needs`/result-check in sync with whichever name is used.
- [ ] **Step 3:** Verify by deliberately editing one source file the fixture's markers point at (in a scratch commit, not kept) and re-running the `--check` command locally to confirm it now fails with a diff; then revert the edit.
- [ ] **Step 4:** Commit.

```bash
git add .github/workflows/ci.yml
git commit -m "ci: fail when full.md or simple.md drifts from the code they're generated from"
```

---

### Task 7: Tag-pinned framework clone reference material

**Files:**
- Modify: `.claude/skills/umbraco-community-automate/SKILL.md` (new short subsection, e.g. under "Working notes" or its own "Checking the framework's current API" section)

**Interfaces:**
- Produces: documented guidance only — no new script is required by the spec, but the guidance must name a concrete command for resolving the installed version.

- [ ] **Step 1:** Add a short section explaining: when the fixture and references don't cover something, check a clone of `https://github.com/umbraco/Umbraco.Automate` at the release tag matching what's actually resolved in this repo — never `main`/`dev`. Give the exact command to find the resolved version (`dotnet list Umbraco.Community.Automate.Demo/Umbraco.Community.Automate.Demo.csproj package | grep Umbraco.Automate.Core`, or reading `**/obj/project.assets.json`), and the tag format (`Umbraco.Automate@<version>`).
- [ ] **Step 2:** Document where the clone goes: a gitignored scratch location (e.g. the Claude Code scratchpad directory, or any `git clone --branch Umbraco.Automate@<version> --depth 1 https://github.com/umbraco/Umbraco.Automate <somewhere-outside-the-repo>`), not an assumption about any contributor's personal directory layout.
- [ ] **Step 3:** Verify from a clean shell (not reusing `/Users/warren/Code/Umbraco.Automate`): run the documented resolve-version command against this repo, then the documented `git clone --branch ... --depth 1` command into a fresh `/tmp` directory, and confirm both succeed exactly as written.
- [ ] **Step 4:** Commit.

```bash
git add .claude/skills/umbraco-community-automate/SKILL.md
git commit -m "docs(skill): document checking a tag-pinned Umbraco.Automate clone for framework API gaps"
```

---

### Task 8: Mention the official template and MCP dev server

**Files:**
- Modify: `.claude/skills/umbraco-community-automate/SKILL.md`

- [ ] **Step 1:** Add one short paragraph each (near the Task 7 section, or in "Working notes"): `dotnet new umbraco-automate-actions` (Umbraco HQ's minimal single-Action scaffold — useful context when migrating a package that started from it, doesn't match this repo's layout) and `@umbraco-automate/mcp-dev` (beta Management-API bridge for driving a live Automate instance — an optional power tool for validating a finished package end-to-end, not for scaffolding one; never point it at production).
- [ ] **Step 2:** Cross-reference from `migrating.md`'s Step 0 inventory: add one line asking whether the source package was built from the official template, as a cheap way to spot likely convention mismatches early (per the design doc's "what's left to the builder").
- [ ] **Step 3:** Commit.

```bash
git add .claude/skills/umbraco-community-automate/SKILL.md .claude/skills/umbraco-community-automate/references/migrating.md
git commit -m "docs(skill): mention the official dotnet new template and the Automate dev MCP server"
```

---

### Task 9: Rewrite the Demo-setup section for `DemoSetupHandler`

**Files:**
- Modify: `.claude/skills/umbraco-community-automate/SKILL.md` ("Demo setup", "Keep the Demo in step with the packages", "Pull requests for a new package", "What a change may touch" sections)

**Interfaces:**
- Consumes: `Umbraco.Community.Automate.Demo/Setup/DemoSetup.cs` (already shipped on this branch) — `DemoSetupHandler.EnsureConnectionsAsync`/`EnsureTestAutomationsAsync`, the `test-<connection-alias>` automation alias and `Test: <Name>` display name, the `community.testConnection` action.

- [ ] **Step 1:** Rewrite "Demo setup" step 1 (Configuration) to match current behaviour — this part is unchanged in substance (still `appsettings.Development.json`/`appsettings.Local.example.json`/the contributor's own `appsettings.Local.json`), so mostly carries over; confirm wording still matches by re-reading the three files as they exist today.
- [ ] **Step 2:** Replace step 2 ("Test page", the uSync/Pushover-file-copying instructions) with: `DemoSetupHandler` auto-creates one connection per `community.`-prefixed connection type on startup (pre-filled from the package's configuration references) and one `Test: <Name>` automation per connection (manual trigger → `community.testConnection` → `If` → `logMessage`), the first time the Demo boots after the package's connection type exists. Nothing to copy or wire by hand; a package with no connection type gets no automatic test automation, and should say in its PR description how it was tested instead (mirrors the old "no connection → use one of its own actions" guidance, now manual rather than file-based).
- [ ] **Step 3:** Replace step 3 ("Check it", restart-on-17-only/uSync-import language) with: restart the Demo (any supported Umbraco major; no uSync major-version restriction applies anymore), open the Automate workspace, run the package's `Test: <Name>` automation with "Run now", and read its steps' output — green `connectionWorks`/log message with real values, or the named-credential failure message with placeholders.
- [ ] **Step 4:** Update "Keep the Demo in step with the packages": drop the three uSync-file bullet entirely; keep the configuration-key bidirectional-check bullet; add that a removed/renamed package's stale connection/automation in an existing local Demo database isn't cleaned up automatically (same "suggest a fresh database" guidance, now about the SQLite file instead of uSync XML).
- [ ] **Step 5:** Update "Pull requests for a new package" checklist to reference the new verification (run "Test: <Name>", not "publish Automate tests → <Name>").
- [ ] **Step 6:** Update "What a change may touch" to drop the three `uSync/v17/` file bullets (no file wiring exists to touch) and the `ContentTypes`/`Automate-Workspaces` config bullets, since `DemoSetupHandler` needs no per-package file changes at all beyond the configuration-key bullet already there.
- [ ] **Step 7:** Verify by re-reading `Umbraco.Community.Automate.Demo/Setup/DemoSetup.cs` once more against the new prose line by line — every mechanism named in the rewritten section must match a real method/behavior in that file.
- [ ] **Step 8:** Commit.

```bash
git add .claude/skills/umbraco-community-automate/SKILL.md
git commit -m "docs(skill): rewrite Demo setup for DemoSetupHandler, replacing the uSync-based instructions"
```

---

### Task 10: Extract short, always-true invariants into `.claude/rules/`

**Files:**
- Create: `.claude/rules/umbraco-community-automate-conventions.md`
- Create: `.claude/rules/umbraco-community-automate-testing.md`
- Modify: `.claude/skills/umbraco-community-automate/SKILL.md` ("Aliases, names and groups", "Configuration and secrets", "Testing" sections)

**Interfaces:**
- Produces: two rule files; the first with no `paths:` frontmatter (always loaded), the second scoped with `paths: ["Packages/**/*.Tests/**/*.cs", ".claude/skills/**/fixtures/**/*.Tests/**/*.cs"]`.

- [ ] **Step 1:** Create `.claude/rules/umbraco-community-automate-conventions.md` (no frontmatter `paths:` key) containing exactly: "Aliases are permanent once released" (one sentence, the existing SKILL.md wording) and "Never commit real credentials — the repo's `.githooks/` gitleaks pre-commit hook enforces this once a contributor has run `.githooks/setup.sh`/`setup.ps1`" (pointing at the real, already-merged hook from PR #16, not an aspirational one). Each with one line pointing back at `.claude/skills/umbraco-community-automate/SKILL.md` for the full rationale/process.
- [ ] **Step 2:** Create `.claude/rules/umbraco-community-automate-testing.md` with frontmatter `paths: ["Packages/**/*.Tests/**/*.cs", ".claude/skills/**/fixtures/**/*.Tests/**/*.cs"]` containing: "xUnit only — no Shouldly, FluentAssertions, Moq or NSubstitute, even if an older package in this repo still uses one" plus the one-line pointer to the skill.
- [ ] **Step 3:** In `SKILL.md`, replace the "Aliases are permanent once released" and "Never commit real credentials" bullets, and the "xUnit only" testing bullet, with a short pointer to the matching rule file instead of restating the rule's wording.
- [ ] **Step 4:** Verify: `grep -r "Aliases are permanent once released" .claude/` and the same for the credentials and xUnit sentences each return exactly one hit (the rule file), confirming no duplication.
- [ ] **Step 5:** Commit.

```bash
git add .claude/rules .claude/skills/umbraco-community-automate/SKILL.md
git commit -m "feat(skill): move always-true package conventions into .claude/rules/"
```

---

### Task 11: Drift pass and PR

**Files:**
- Modify: `.claude/skills/umbraco-community-automate/SKILL.md` (as needed), any reference doc (as needed)

- [ ] **Step 1:** Re-read `SKILL.md` end to end once more; check every example-package citation (WeatherApi/Google Sheets/DevTo as "idiomatic today") against what's now simplest/most representative given Pushover and Skoda also exist — change only if a citation is actually misleading, not for its own sake.
- [ ] **Step 2:** Confirm `new-package.md`, `migrating.md` (beyond Task 8's addition) and `fields.md` need no further changes (per the design doc's non-goals, these stay as-is unless something above actually touched them).
- [ ] **Step 3:** `dotnet build`/`dotnet test` the whole solution once, plus `tools/test-umbraco-compat.sh .claude/skills/umbraco-community-automate/fixtures/kitchen-sink/Umbraco.Community.Automate.Examples.KitchenSink`, to confirm nothing else in the repo broke.
- [ ] **Step 4:** Commit any Step 1 edits (skip this commit if none were needed).
- [ ] **Step 5:** Push the branch; open one PR against `main` titled e.g. "Rename and update the Claude skill; fold Kitchen Sink into its fixtures", body summarizing the 8 decisions and linking the design doc and this plan, closing PR #46 in the description (`Closes #46` only if GitHub should actually close it as superseded — confirm with Warren first if unsure, since `Closes` wording auto-closes on merge).
- [ ] **Step 6 (post-merge, not part of this PR):** per spec Decision 7, do a live dry run asking the renamed skill to build a small real package end-to-end, and report how the guided process felt.

```bash
git push -u origin feat/claude-skill-rename-and-update
gh pr create --title "Rename and update the Claude skill; fold Kitchen Sink into its fixtures" --body "..."
```
