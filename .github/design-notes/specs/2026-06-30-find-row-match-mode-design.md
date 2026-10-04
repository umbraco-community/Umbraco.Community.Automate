# FindRow Match Mode & Case Sensitivity

**Date:** 2026-06-30
**Branch:** feat/google-sheets-find-row

## Context

`FindRowAction` currently searches a Google Sheet column using `StringComparison.Ordinal` — exact, case-sensitive matching only. This mirrors Zapier's behaviour but falls short of n8n, which exposes contains/startsWith/endsWith operators. Users searching for email addresses, names, or product codes frequently need partial matching and case-insensitive comparisons. Adding match mode and a case sensitivity toggle closes this gap without requiring any custom TypeScript UI.

## Design

### New enum — `FindRowMatchMode`

Defined in the GoogleSheets project (not reusing `FindContentMatchMode` from core, which lacks `EndsWith` because its Lucene backend makes leading-wildcard queries expensive; string comparison has no such constraint).

```csharp
public enum FindRowMatchMode
{
    Exact = 0,
    Contains = 1,
    StartsWith = 2,
    EndsWith = 3,
}
```

### Settings — `FindRowSettings`

Two new fields after `SearchValue` (SortOrder 4 and 5), following the `FindContentSettings` pattern exactly:

| Property | CLR type | EditorUiAlias | Default |
|----------|----------|---------------|---------|
| `MatchMode` | `string` | `Umb.PropertyEditorUi.Dropdown` | `"Exact"` |
| `CaseSensitive` | `bool` | `Umb.PropertyEditorUi.Toggle` | `false` |

`MatchMode` is stored as a string (not the enum type) so Umbraco's Dropdown editor round-trips cleanly — the editor persists selections as single-element arrays, and the framework's `SingleValueArrayConverterFactory` unwraps them. The enum is parsed at execute time via `Enum.TryParse`, falling back to `Exact` on any unrecognised value.

`CaseSensitive` defaults to `false` (case-insensitive) to match the most natural user expectation when searching a spreadsheet. The existing test `ExecuteAsync_search_is_case_sensitive` will be updated to reflect the new default.

### Execute logic — `FindRowAction`

Replace the single `string.Equals(cellValue, settings.SearchValue, StringComparison.Ordinal)` comparison with:

```csharp
var comparison = settings.CaseSensitive
    ? StringComparison.Ordinal
    : StringComparison.OrdinalIgnoreCase;

var matchMode = Enum.TryParse<FindRowMatchMode>(settings.MatchMode, ignoreCase: true, out var m)
    ? m : FindRowMatchMode.Exact;

var isMatch = matchMode switch
{
    FindRowMatchMode.Contains   => cellValue.Contains(settings.SearchValue, comparison),
    FindRowMatchMode.StartsWith => cellValue.StartsWith(settings.SearchValue, comparison),
    FindRowMatchMode.EndsWith   => cellValue.EndsWith(settings.SearchValue, comparison),
    _                           => string.Equals(cellValue, settings.SearchValue, comparison),
};

if (!isMatch) continue;
```

### Files to change

| File | Change |
|------|--------|
| `Actions/FindRowSettings.cs` | Add `MatchMode` (string) and `CaseSensitive` (bool) fields |
| `Actions/FindRowAction.cs` | Replace ordinal comparison with match mode switch |
| `Actions/FindRowMatchMode.cs` | New file — the companion enum |
| `Tests/Actions/FindRowActionTests.cs` | Add theory covering all 4 modes × case sensitivity; update existing case-sensitive test |

### Tests to add

A `[Theory]` with `[InlineData]` covering:

- All 4 match modes with `CaseSensitive = false` (e.g. "bob" matches "Bob" for Exact, "Bob Smith" for Contains, "Bob S" for StartsWith, "ob Smith" for EndsWith)
- `CaseSensitive = true` — each mode respects case (e.g. "bob" does NOT match "Bob")
- Unrecognised `MatchMode` string falls back to Exact
- Empty `SearchValue` with Contains/StartsWith/EndsWith returns found (every non-empty cell contains the empty string)

The existing `ExecuteAsync_search_is_case_sensitive` test is updated: it now verifies that the default (`CaseSensitive = false`) matches "alice" against "Alice", and a separate test verifies case-sensitive mode rejects the mismatch.

## Verification

1. `dotnet test` — all existing and new tests pass
2. Manually open the Find Row action in the Umbraco backoffice, confirm the Match mode dropdown shows Exact/Contains/StartsWith/EndsWith and the Case sensitive toggle renders below it
3. Run an automation end-to-end: search for a partial value (e.g. "bob" in a sheet containing "Bob Smith") with Contains + case-insensitive — confirm the `found` outcome fires
