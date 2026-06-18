#!/bin/bash
# Setup script to configure git hooks for Umbraco.Community.Automate monorepo

set -e

echo "========================================="
echo "Umbraco.Community.Automate Git Hooks Setup"
echo "========================================="
echo ""

# Get the repository root
REPO_ROOT="$(git rev-parse --show-toplevel)"

if [ -z "$REPO_ROOT" ]; then
    echo "Error: Not in a git repository" >&2
    exit 1
fi

HOOKS_DIR="$REPO_ROOT/.githooks"

# Check if hooks directory exists
if [ ! -d "$HOOKS_DIR" ]; then
    echo "Error: .githooks directory not found at $HOOKS_DIR" >&2
    exit 1
fi

# Make hook scripts executable
echo "Making hook scripts executable..."
chmod +x "$HOOKS_DIR/pre-push"
chmod +x "$HOOKS_DIR/pre-push.sh"
chmod +x "$HOOKS_DIR/commit-msg"

# Configure git to use the custom hooks directory
echo "Configuring git to use custom hooks directory..."
git config core.hooksPath .githooks

echo ""
echo "Git hooks configured successfully!"
echo ""
echo "The following hooks are now active:"
echo "  - pre-push: Validates branch naming conventions"
echo "  - commit-msg: Validates commit messages (conventional commits)"
echo ""
echo "Note: release-manifest.json lifecycle hooks (pre-merge-commit, post-merge,"
echo "the merge driver) are not set up yet — they're part of the deferred"
echo "release-management tooling. See CLAUDE.md's 'Deferred Tooling' section."
echo ""
echo "To disable hooks, run:"
echo "  git config --unset core.hooksPath"
echo ""
