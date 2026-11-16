These apply to any Automate package in this repo, whether or not a request looks like it's
about building or changing one. Full rationale, process and examples are in the
[umbraco-community-automate skill](../skills/umbraco-community-automate/SKILL.md).

- **Aliases are permanent once released.** Saved connections and automations store the
  connection type, action and trigger aliases, so renaming one breaks them. Pick them carefully
  up front.
- **Never commit real credentials, not even temporarily.** The repo's gitleaks pre-commit hook
  (`.githooks/pre-commit`, enabled via `.githooks/setup.sh`/`setup.ps1`) blocks commits and
  pushes that contain them — real, local enforcement, not just this reminder.
