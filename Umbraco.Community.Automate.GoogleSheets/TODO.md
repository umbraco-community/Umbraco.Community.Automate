# Google Sheets provider — backlog

Tracked in priority order. Check items off as they land; add new ones at the bottom of their section unless they're genuinely higher priority.

## Now

- [x] **CI workflow** — `.github/workflows/ci.yml` runs backend unit tests, frontend unit tests, and the full Playwright E2E suite against the Demo site, on PRs to `main` and via manual dispatch.
- [x] **Package README.md** — ships in the NuGet package and drives the Umbraco Marketplace listing's description.

## Next

- [x] **Test coverage gaps** — malformed/partial Google API JSON responses in `AppendRowAction`.
- [ ] **Friendlier error surfacing** — `AppendRowAction` currently dumps the raw Google API error JSON into the exception message. Parse it into a cleaner, user-facing message.

## New actions

(Per the original design spec — Append Row was the deliberately scoped MVP slice.)

- [ ] **Read Rows action** — read a range back out of a sheet.
- [ ] **Update Row action** — update an existing row by index/key instead of always appending.
- [ ] **Clear Rows action** — clear a range without deleting the sheet/tab.
- [ ] **Create Spreadsheet action** — create a new spreadsheet and return its ID, separate from Append Row (matches n8n/Zapier's separation of file creation from row operations).

## Bigger UX work

- [ ] **Header-aware column mapping** — read the selected tab's header row via a backend endpoint and render one binding field per named column (Zapier-style), as an alternative to the current ordered column-list editor. Assumes row 1 holds headers. Bigger lift than the items above — needs its own design pass.
