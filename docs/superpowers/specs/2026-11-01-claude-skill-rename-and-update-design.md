# Claude Skill: rename and update — Design Spec

Date: 2026-11-01
Status: Draft, pending review

## Summary

Port Paul's `.claude/skills/umbraco-automate/` skill (written on the original PR #32 /
`Umbraco.Community.Automate-pr32` reference branch, never yet merged into this repo) into
`Umbraco.Community.Automate` as `.claude/skills/umbraco-community-automate/`, updated for
everything that's changed since it was written, and reconciled with the open questions
raised on PR #46's thread.

This is the last substantive chunk of the PR #32 breakup (after the Dev reference examples
and the Demo site overhaul). After this chunk, only final docs polish remains.

## Why

Paul's skill is the house rules for contributing a package to this repo: folder layout,
naming, settings-field conventions, testing conventions, and a guided interview process so
Claude doesn't start coding before the shape of a new package is actually agreed. It's
already been demoed by Paul and works. But it was written against the state of the repo
*before* this breakup project changed several things underneath it, and PR #46 (which the
skill depends on for its "Full" reference example) stalled on an unresolved disagreement
about whether that reference example belongs in `Packages/` at all. Both need resolving
together, since the skill's reference docs point at PR #46's files by path.

## Goals

- The skill works correctly against the repo as it exists today (post #47/#48), not as it
  existed when Paul wrote it.
- `Packages/` only ever contains real, installable community packages — never a synthetic
  teaching example, per Paul's stated concern on the PR #46 thread.
- The skill's worked examples (`simple.md`/`full.md`) can't silently drift from code that
  actually compiles and passes tests, without needing a live package in `Packages/` to
  achieve that.
- The skill's framework-API knowledge doesn't go stale as `Umbraco.Automate` releases new
  versions, without the skill claiming an API that isn't actually released yet.
- The rename removes any risk of confusion with a future official Umbraco HQ skill of a
  similar name.

## Non-goals

- Redesigning the guided interview process, the migration process, or the settings-field
  reference (`new-package.md`, `migrating.md`, `fields.md`). These are already mature and
  demoed; only the specific sections identified below change.
- Building or wiring up `@umbraco-automate/mcp-dev` or the official `dotnet new
  umbraco-automate-actions` template into this repo's own tooling. Both get a short
  explanatory mention in the skill; neither becomes a dependency.
- Re-litigating PR #47/#48's Demo-site mechanism. This chunk documents it, not changes it.

## Anti-goals

- A skill so long or so hedged that Claude stops reading it carefully. Paul's existing prose
  style (terse, example-first, "copy the reference") should be preserved, not diluted.
- A second place for framework knowledge to drift from reality. Every piece of framework-API
  guidance should point at something that's checked (compiled, tested, or read live from a
  tagged clone), not asserted as prose with nothing behind it.

## Constraints

- `github.com/umbraco/Umbraco.Automate` is public; cloning or fetching it needs no auth.
- This repo's `Directory.Packages.props` floors `Umbraco.Automate.Core` on a *range*
  (`[17.0.0, 19.0.0)` / `[18.4.0, 19.0.0)`), not an exact version — "what's actually
  installed" means the resolved version in a real restore, not the floor.
- The framework repo tags releases as `Umbraco.Automate@<version>` (and per-package tags like
  `Umbraco.Automate.OpenIddict@<version>`); `main`/`dev` branches run ahead of any released
  version and can contain APIs not yet in a published NuGet package (e.g. `McpTrigger`, still
  on an unmerged feature branch as of this writing).
- PR #46 (`feat/kitchensink-example`, open, paused) already trimmed Kitchen Sink down to
  outcomes + a notification trigger (dropped the custom field-editor `Client/` and the
  `Simple`/`TestConnection` tiers, the latter because `TestConnection` now ships separately
  via #47/#48). Its current file list (26 files under
  `Packages/_Development/KitchenSink/Umbraco.Community.Automate.Examples.KitchenSink{,.Tests}/`)
  is the starting material for the fixture described below, not something to rebuild from
  scratch.
- No generation tooling tying `simple.md`/`full.md` to compiled, tested code exists anywhere
  yet, on this branch or on the `-pr32` reference branch, despite SKILL.md's claim that this
  is "checked." This chunk has to build that tooling, not port it.

## Decisions (the how, as decided)

1. **Rename.** `.claude/skills/umbraco-automate/` → `.claude/skills/umbraco-community-automate/`.
   Update the `name` and `description` frontmatter, the `SKILL.md` title, and every internal
   reference to the skill's own name. (User decision.)

2. **Demo-setup section rewritten.** Every uSync-based instruction in `SKILL.md` (the "Demo
   setup" section, "Keep the Demo in step with the packages", the pull-request checklist, and
   the "What a change may touch" file list) is replaced with the actual PR #47/#48 mechanism:
   `DemoSetupHandler` auto-provisions the API user and one connection per package on boot;
   verifying a new package means adding its config placeholders to
   `appsettings.Development.json`/`appsettings.Local.example.json` and confirming its
   auto-generated `Test: <Name>` automation (manual trigger, `community.testConnection` →
   `If` → `logMessage`) runs cleanly via "Run now". No uSync files, no content type, no
   content-published trigger, no republish-on-import step. (Follows directly from already-
   shipped #47/#48 work — not a new judgment call.)

3. **Kitchen Sink reconciled.** The example moves out of `Packages/_Development/` entirely —
   `Packages/` holds only real community packages, per Paul's comment on the PR #46 thread.
   In its place: a build-and-test-checked **fixture**, not a package (no `Directory.Build.props`
   package marker, no release tag, never packed or published), living at
   `.claude/skills/umbraco-community-automate/fixtures/kitchen-sink/` — bundled with the skill
   it exists to support, so it's obviously not browsable as a real package. CI gets a
   dedicated step that builds and tests anything under `.claude/skills/*/fixtures/`, separate
   from the existing package-discovery mechanism (which must *not* pick this up). `full.md`'s
   code blocks are generated from the fixture's actual files by a small script, not hand-
   duplicated, so the markdown and the compiled/tested code can never drift from each other.
   **CI re-runs the generator and fails if its output differs from the committed `full.md`**
   (a generate-and-diff check, not just a build/test of the fixture in isolation) — otherwise
   someone can edit the fixture and forget to regenerate, which is exactly the drift this
   fixture exists to prevent. (User decision, reconciling Paul's "not in `Packages/`" with
   the drift concern Owain, Warren, and this conversation independently raised; the diff-check
   detail follows directly from Goal 3 and the Anti-goals section, not a new judgment call.)

   **`simple.md` gets the same protection, from a different source — no second fixture.**
   Paul's original `Packages/_Development/Simple/` was never actually ported into this repo
   (only the trimmed `KitchenSink` and, separately, `TestConnection` made it in via PR #46 and
   #47/#48), so there's nothing today for `simple.md` to be checked against, and a second
   synthetic fixture would be pure added maintenance for a shape that isn't actually
   underserved: unlike outcomes, triggers, and a custom field editor (which no real package
   exercises — PR #46's own reason for existing), the Simple shape (an API key, a connection,
   one or two actions, no `Api/` layer) is already exercised by every basic real package in
   the repo. So `simple.md` is generated the same way as `full.md`, by the same script, just
   pointed at a different source: **`Packages/WeatherApi/`** (already named in `SKILL.md` as
   the simplest real connection) instead of a synthetic fixture. The generation script takes a
   source directory as a parameter rather than being hard-coded to the Kitchen Sink fixture;
   CI's generate-and-diff check covers both `simple.md` and `full.md`. This closes the drift
   gap completely rather than leaving it half-open, at no extra fixture-maintenance cost, and
   a real package's own maintainer has more reason to keep it working than a synthetic
   fixture's would. (User decision.)

4. **Tag-pinned framework clone, as a secondary reference.** For anything the fixture doesn't
   cover, or that may have changed since it was last touched, the skill tells Claude to check
   a clone of `github.com/umbraco/Umbraco.Automate` checked out at the release tag matching
   the version actually resolved in this repo's build — never `main`/`dev` — rather than
   trust static prose about the framework's current API shape. This is a supplement to the
   fixture (which remains primary, since a CI build failure is stronger drift protection than
   a Claude reading pass), not a replacement for it. (User decision.)

5. **Official tooling mentioned, not relied on.** A short note each on `dotnet new
   umbraco-automate-actions` (Umbraco HQ's own minimal single-Action scaffold — useful context
   when migrating a package that started from it, but doesn't match this repo's conventions)
   and `@umbraco-automate/mcp-dev` (a beta Management-API bridge for driving a *live* Automate
   instance — useful as an optional power tool for validating a finished package end-to-end,
   not for scaffolding one). Matches Paul's own framing on the PR #46 thread: the skill is
   specifically for contributing to *this* repo, where conventions differ from a generic
   Automate package. (User decision, informed by Paul's PR comment.)

6. **PR #46 closes.** Its reusable pieces (the trimmed Kitchen Sink actions, trigger, tests)
   move into the fixture location on a fresh branch, as one combined PR alongside the
   renamed/updated skill — not stacked as two PRs, since the skill's reference docs depend on
   the fixture existing at its new path from the start. The rewritten Demo-setup section
   (Decision 2) describes `DemoSetupHandler` and the `Test: <Name>` automations, which don't
   exist on `feat/repo-overhaul`'s own tip yet — only on PR #48's branch, three commits ahead.
   So the fresh branch forks from the tip of the in-flight stack that actually has that code
   (currently `feat/demo-test-connection-automations`, PR #48's branch), the same
   code-dependency stacking rule already used for #48 on #47 — not from `feat/repo-overhaul`
   directly. If #47/#48 merge before this chunk's branch is cut, fork from `feat/repo-overhaul`
   at that point instead, once it actually contains the code being documented. (User decision
   on closing #46 and combining into one PR; the branch-base correction follows from the
   existing stacking rule, not a new judgment call.)

7. **Verification after landing.** Once merged, do a live dry run: ask the renamed skill to
   build a small real package end-to-end and see how the guided process actually feels,
   rather than treating a clean read-through as sufficient. (User decision.)

8. **A small set of short, always-true invariants move into `.claude/rules/`, not just the
   skill.** `.claude/rules/**/*.md` is a separate, documented Claude Code mechanism from
   Skills: an unscoped rule loads into context at the start of every session regardless of
   whether a prompt looks skill-shaped, where a skill's body only loads once Claude (or the
   user) decides the skill itself is relevant. That gap matters here — a request like "add a
   test with Moq" may never read as "the Automate skill," so a convention that only lives in
   `SKILL.md`/`testing.md` can be silently missed on exactly the requests most likely to
   violate it. Concretely: `"aliases are permanent once released"` and `"never commit real
   credentials"` move into an unscoped rule (no `paths:` frontmatter, always loaded);
   `"xUnit only, no mocking libraries (Moq/NSubstitute/FluentAssertions/Shouldly)"` moves into
   a rule scoped via `paths:` to test-project file globs. Everything procedural — folder
   layout, naming, the guided interview, the migration process, the settings-field reference
   — stays in the skill, which is the right place for multi-step process content. Each
   invariant lives in exactly one place: the skill points at the rule rather than restating
   it, so the two can't drift apart. A rule is still just context, not enforcement — it
   doesn't stop a credential from actually being committed the way a hook would — so this is
   a belt to go with the gitleaks pre-commit hook already in `.githooks/` (opt-in per
   contributor via `.githooks/setup.sh`/`setup.ps1`), not a replacement for it. (User
   decision; corrects the design conversation's earlier assumption that this hook was still
   unmerged — it's already in the repo, just not every contributor's `core.hooksPath` points
   at it yet.)

## What's left to the builder

- Exact fixture project/namespace names (keep `Umbraco.Community.Automate.Examples.KitchenSink`
  as-is to minimize churn on the files being moved, or rename now that "Examples" no longer
  needs to read as a `Packages/`-adjacent name — either is fine; note the choice made).
- The generation script's implementation (language/tooling, where it runs — a `tools/`
  shell/PowerShell script matching the repo's existing `tools/test-umbraco-compat.sh` style
  is the obvious fit; it needs a source-directory parameter so it can target both the Kitchen
  Sink fixture and `Packages/WeatherApi/`) and exactly which files it extracts into which
  sections of `full.md` and `simple.md`.
- The CI workflow changes: building/testing `.claude/skills/*/fixtures/` without triggering
  package-discovery, release-tagging, or packing for anything found there, and the
  generate-and-diff check for both `simple.md` and `full.md` (WeatherApi is already built and
  tested by the existing package pipeline, so `simple.md`'s CI addition is just the diff
  check, not a new build/test job).
- The exact mechanism for resolving "the tag matching what's actually installed" (reading
  the restored package version from `obj/project.assets.json` or `dotnet list package` is
  the likely approach) and where a cloned reference copy of `Umbraco.Automate` should live
  when a contributor doesn't already have one (a gitignored location, cloned on demand,
  shallow, at the resolved tag — not assuming any contributor's personal directory layout).
- Line-by-line pass over `SKILL.md` and all five reference docs for anything else that's
  drifted (e.g. example packages cited as "idiomatic today" — currently WeatherApi/Google
  Sheets/DevTo — should be re-checked against what's actually simplest/most representative
  now that Pushover and Skoda also exist).
- Whether `migrating.md`'s Step 0 inventory checklist should explicitly ask whether the
  source package was built from the official `dotnet new` template, as a cheap way to spot
  likely convention mismatches early.
- Exact `.claude/rules/` file names/count for Decision 8 (one file vs. one per invariant),
  the exact `paths:` glob for the xUnit/no-mocking rule (likely `**/*.Tests/**/*.cs`,
  matching this repo's `<Area>.Tests` project naming), and the exact wording each rule uses
  to point back at the skill for the full rationale/process context.
