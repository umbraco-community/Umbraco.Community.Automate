# One-time setup per clone: turns on this repo's git hooks (a gitleaks secret
# scan before every commit and push) by pointing core.hooksPath at .githooks/.
#
# Usage, from the repo root: .\.githooks\setup.ps1

if (-not (Get-Command gitleaks -ErrorAction SilentlyContinue)) {
    Write-Error "gitleaks is required but not found. Install it (e.g. 'winget install gitleaks' or 'scoop install gitleaks' on Windows, 'brew install gitleaks' on macOS, or see https://github.com/gitleaks/gitleaks#installing), then re-run this script."
    exit 1
}

git config core.hooksPath .githooks
if ($LASTEXITCODE -ne 0) {
    Write-Error "git config failed (exit code $LASTEXITCODE). Setup did not complete."
    exit 1
}

Write-Host "Git hooks enabled for this clone (core.hooksPath = .githooks)."
