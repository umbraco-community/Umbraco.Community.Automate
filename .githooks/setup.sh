#!/usr/bin/env bash
#
# One-time setup per clone: turns on this repo's git hooks (a gitleaks secret
# scan before every commit and push) by pointing core.hooksPath at .githooks/.
#
# Usage, from the repo root: ./.githooks/setup.sh

set -euo pipefail

if ! command -v gitleaks >/dev/null 2>&1; then
  echo "gitleaks is required but not found. Install it (e.g. 'brew install gitleaks' on macOS, 'winget install gitleaks' or 'scoop install gitleaks' on Windows, or see https://github.com/gitleaks/gitleaks#installing), then re-run this script." >&2
  exit 1
fi

git config core.hooksPath .githooks

echo "Git hooks enabled for this clone (core.hooksPath = .githooks)."
