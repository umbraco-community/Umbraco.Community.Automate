---
name: repo-setup
description: Performs initial repository setup including git hooks, demo site creation, and dependency installation. Use when cloning the repository for the first time, onboarding a new developer, or resetting the development environment.
allowed-tools: Bash, AskUserQuestion, Read, Glob
---

# Setup Repository

You are helping set up the Umbraco.Community.Automate repository for local development.

## Task

Perform initial repository setup by running the appropriate setup scripts based on the user's platform and preferences.

## Available Setup Tasks

1. **Git Hooks** - Install git hooks for commit message / branch naming enforcement
    - Linux/Mac: `./scripts/setup-git-hooks.sh`

2. **Demo Site** - Create unified solution with demo Umbraco instance, referencing the published `Umbraco.Automate` package plus any provider packages already in this repo
    - Linux/Mac: `./scripts/install-demo-site.sh`
    - Options: `--skip-template-install` / `--force`

3. **Dependencies** - Install npm workspace dependencies (only relevant once a package has a `Client/` frontend)
    - Command: `npm install`

4. **Initial Build** - Build the unified solution (only meaningful once the demo site exists)
    - Command: `dotnet build Umbraco.Community.Automate.local.slnx`

## Workflow

1. **Detect platform** - Check if Windows or Linux/Mac (only Bash scripts exist today; flag if on Windows and no PowerShell equivalent has been added yet)
2. **Ask user what to set up** - Use AskUserQuestion with options:
    - "Git hooks only"
    - "Git hooks + demo site"
    - "Full setup (hooks + demo + dependencies + build)"
    - Allow custom selections
3. **Execute selected tasks** in order
4. **Report results** - Show what was completed and any errors

## Important Notes

- Always run scripts from the repository root
- Check if demo site already exists before running install script
- Provide clear feedback after each step
- If any step fails, report it but continue with remaining tasks if possible

## Example Flow

```
User invokes: /repo-setup

You ask: "What would you like to set up?"
- Git hooks only
- Git hooks + demo site
- Full setup (hooks + demo + dependencies + build)

User selects: "Full setup"

You execute:
1. Run setup-git-hooks.sh
2. Run install-demo-site.sh
3. Run npm install
4. Run dotnet build

Report: "Setup complete!"
```
