## What this changes

<!-- One or two sentences, and a link to the issue it closes, e.g. "Closes #12". -->

## Package

<!-- Which package(s) this touches, or "repo" for CI, docs or shared files. -->

## Checklist

- [ ] Follows the layout and conventions in [CONTRIBUTING](https://github.com/umbraco-community/Umbraco.Community.Automate/blob/main/.github/CONTRIBUTING.md) (standard folders, `community.<area>` aliases, configuration references under `Umbraco:Automate:Secrets` / `Variables`)
- [ ] Tests added or updated for every action, outcome and trigger changed, and `dotnet test` passes
- [ ] Front end built and its tests pass (`npm ci && npm run build && npm test` in `Client/`), if the package has one
- [ ] Package README updated: settings, outcomes and outputs, troubleshooting
- [ ] Checked in the Demo site: the connection, action or trigger appears with its icon and works
- [ ] No real credentials anywhere in the change (placeholders only; real values in user secrets)
- [ ] Only changes this package's folder and its wiring ([what a package's pull request may change](https://github.com/umbraco-community/Umbraco.Community.Automate/blob/main/.github/CONTRIBUTING.md#what-a-packages-pull-request-may-change)); anything else is explained below

### For a new package

- [ ] Projects under `Packages/<Area>/`, with a `Directory.Build.props` and its own `MinVerTagPrefix`
- [ ] Added to `Umbraco.Community.Automate.Demo.slnx` and referenced from the Demo site
- [ ] Every configuration key it reads is in the Demo's `appsettings.Development.json` (placeholders) and `appsettings.Local.example.json` (template)
- [ ] Has a test page in the Demo site (`uSync/v17/`: page type, page and *Test:* automation, plus its lines in `automatetests.config` and `demo.config`), checked by publishing it on a fresh database
- [ ] Row added to the packages table in the root `README.md`
- [ ] Added to the package list in `.github/ISSUE_TEMPLATE/bug-report.yml` and to `.github/CODEOWNERS`
- [ ] Aliases chosen carefully: they're stored in people's automations and can't change once released
- [ ] Doesn't duplicate a step Umbraco Automate already includes, or an existing package here
