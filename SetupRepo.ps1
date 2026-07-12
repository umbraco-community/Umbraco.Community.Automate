if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "Node.js is required but not found. Install it from https://nodejs.org, then re-run this script."
    exit 1
}

if (-not (Get-Command gitleaks -ErrorAction SilentlyContinue)) {
    Write-Error "gitleaks is required but not found. Install it (e.g. 'scoop install gitleaks' or 'winget install gitleaks' on Windows, 'brew install gitleaks' on macOS, or see https://github.com/gitleaks/gitleaks#installing for other options), then re-run this script."
    exit 1
}

Write-Host "Installing repo tooling (Lefthook)..."
npm install

Write-Host "Installing git hooks..."
npx lefthook install

Write-Host "Setup complete. Git hooks are now active for this clone."
