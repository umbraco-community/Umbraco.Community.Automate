---
paths:
  - "Packages/**/*.Tests/**/*.cs"
  - ".claude/skills/**/fixtures/**/*.Tests/**/*.cs"
---

**xUnit only.** Use xUnit's `Assert`; don't add assertion or mocking libraries (no Shouldly,
FluentAssertions, Moq or NSubstitute), even though some older packages still use one — don't
copy that into new code. One framework keeps tests readable for every contributor and the
dependency list short. Full rationale and patterns (hand-written fakes instead of mocks,
`ActionTestHarness`) are in the
[umbraco-community-automate skill](../skills/umbraco-community-automate/SKILL.md#testing).
