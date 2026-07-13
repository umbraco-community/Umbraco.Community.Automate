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

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
