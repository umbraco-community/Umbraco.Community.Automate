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

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
