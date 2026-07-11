# Contributing

Thanks for your interest in contributing to Umbraco.Community.Automate! This is a monorepo of community-maintained provider packages for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate), each contributing connections, triggers, and/or actions.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22.x](https://nodejs.org/) — only needed for packages with a backoffice `Client/` folder (custom property editors, etc.)

## Project layout

Each provider package is a top-level folder (e.g. `Umbraco.Community.Automate.GoogleSheets`), with a matching `*.Tests` project alongside it. `Umbraco.Community.Automate.Demo` is a throwaway Umbraco site used to manually exercise every package end-to-end, and also hosts the Playwright E2E suite.

## Building and testing

```bash
dotnet build
dotnet test
```

Run a single package's tests directly, e.g.:

```bash
dotnet test Umbraco.Community.Automate.GoogleSheets.Tests
```

If a package has a `Client/` folder, run its frontend unit tests with `npm test` from that folder (`npm ci` first). CI also runs a Playwright E2E pass against the Demo site — see `.github/workflows/ci.yml` for the exact steps if you need to reproduce it locally.

## Making a change

1. Branch off `main` — branch names follow `<type>/<short-description>`, e.g. `feat/google-sheets-clear-range`, `fix/oauth-token-refresh`, `docs/readme-cleanup`.
2. Commit messages follow `type(scope): Description`, e.g. `add(action): AppendRowAction — append a row of values to a sheet`, `fix(action): require LookupValue in UpdateRowAction`, `test(action): FindRowActionTests — found/notFound outcomes`. Common types: `add`, `fix`, `refactor`, `test`, `docs`, `chore`.
3. Open a PR against `main`. CI runs backend/frontend unit tests and the Playwright E2E suite — all three must pass before merge.
4. Keep PRs scoped to one action/feature where practical; a new action typically lands as its own PR with its own tests.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
