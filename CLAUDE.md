# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Repository Is

`Umbraco.Community.Automate` is a monorepo of **community-contributed provider packages** for [Umbraco.Automate](https://github.com/umbraco/Umbraco.Automate) — the official automation system for Umbraco CMS. Each package here contributes triggers, actions, and/or connection types that plug into Umbraco.Automate, the same way `Umbraco.Automate.Slack` or `Umbraco.Automate.OpenIddict` do in the main repo.

**This repo does not contain the Umbraco.Automate core engine.** Packages here depend on the published `Umbraco.Automate.Core` NuGet package via `<PackageReference>`, not a project reference — there is no sibling core project to reference locally.

## Repository Structure

Each provider package follows the same shape as a product in the main `Umbraco.Automate` repo:

```
ProviderName/
├── src/
│   └── ProviderName/               # Single RCL, or split Core/Web/StaticAssets if it grows
├── tests/
│   └── ProviderName.Tests.Unit/
├── ProviderName.slnx
└── CLAUDE.md                       # Package-specific guidance, references this root file
```

See `Umbraco.Automate`'s own CLAUDE.md and `docs/vocabulary.md` (in that repo) for the full domain vocabulary (Providers, Actions, Triggers, Settings, Connections, etc.) — this repo doesn't redefine those concepts, it builds on them.

## Development Environment

### Prerequisites

- .NET 10.0 SDK
- Node.js 22.x (only needed for packages with a backoffice frontend)
- Git

### Demo Site

A demo Umbraco site lives at `demo/` for manual testing against real packages. Use the `/demo-site-management` skill to start/stop it.

## Build Commands

```bash
dotnet build <ProviderName>/<ProviderName>.slnx
dotnet test <ProviderName>/<ProviderName>.slnx
```

## Coding Standards

These mirror `Umbraco.Automate`'s own conventions, since packages here plug directly into that engine.

### Async Methods: `[Action][Entity]Async`

```csharp
Task<Automation?> GetAutomationAsync(Guid id, CancellationToken ct);
Task<Automation> CreateAutomationAsync(Automation automation, CancellationToken ct);
```

Qualifiers (`ByAlias`, `Paged`, `All`, `Default`) come after the entity: `GetAutomationByAliasAsync`.

### Extension Methods

All extension methods go in a `<ProviderName>.Extensions` namespace for IntelliSense discoverability.

## Commit Message Format

[Conventional Commits](https://www.conventionalcommits.org/): `<type>(<scope>): <description>`

**Valid types:** `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `perf`, `ci`, `revert`, `build`

**Scopes:** one per package (e.g. `googlesheets`), plus `deps`, `ci`, `docs`, `release`, `hooks`, `build`, `config`. Enforced by `commitlint.config.js`, which discovers package scopes dynamically — see that file for the exact mechanism.

**Subject is sentence-case** — capitalize the first word after the scope.

## Adding a New Provider Package

Use the `umbraco-automate-actions` dotnet template in `templates/` (run `/repo-setup` or see its `README.md`) to scaffold a new package with the standard Composer/Action/Settings/Output/test shape, already wired to `PackageReference` the published `Umbraco.Automate.Core`.

## Deferred Tooling

The following exist as mature, battle-tested skills in the main `Umbraco.Automate` repo's `.claude/skills/` but are **deliberately not ported here yet**: `release-management`, `changelog-management`, `release-manifest-management`, `post-release-cleanup`. They orchestrate cascading semver bumps across several already-published, cross-referencing packages — not useful with zero packages and no release history. Revisit once this repo has 2+ published packages with real interdependencies; the main repo's versions are the reference implementation to adapt, not reinvent.

## CI

GitHub Actions (`.github/workflows/build.yml`) — restore/build/test on push and PR. Not a port of the main repo's Azure Pipelines setup, which doesn't fit a public GitHub-hosted community repo.
