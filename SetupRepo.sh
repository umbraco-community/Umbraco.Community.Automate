#!/usr/bin/env bash
set -euo pipefail

if ! command -v node >/dev/null 2>&1; then
  echo "Node.js is required but not found. Install it from https://nodejs.org, then re-run this script." >&2
  exit 1
fi

if ! command -v gitleaks >/dev/null 2>&1; then
  echo "gitleaks is required but not found. Install it (e.g. 'brew install gitleaks' on macOS, 'scoop install gitleaks' or 'winget install gitleaks' on Windows, or see https://github.com/gitleaks/gitleaks#installing for other options), then re-run this script." >&2
  exit 1
fi

echo "Installing repo tooling (Lefthook)..."
npm install

echo "Installing git hooks..."
npx lefthook install

echo "Setup complete. Git hooks are now active for this clone."
