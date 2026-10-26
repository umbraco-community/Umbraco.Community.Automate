## What this changes

<!-- One or two sentences, and a link to the issue it closes, e.g. "Closes #12". -->

## Package

<!-- Which package(s) this touches, or "repo" for CI, docs or shared files. -->

## Checklist

- [ ] Tests added or updated for every action, outcome and trigger changed, and `dotnet test` passes
- [ ] Front end built and its tests pass (`npm ci && npm run build && npm test` in `Client/`), if the package has one
- [ ] Package README updated: settings, outputs and troubleshooting
- [ ] No real credentials anywhere in the change (placeholders only; real values via [.NET User Secrets](CONTRIBUTING.md#preventing-secret-leaks))
- [ ] Scoped to one package where practical; anything else is explained below
