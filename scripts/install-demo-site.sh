#!/bin/bash
# Unified Demo Site Setup Script
# Creates a shared demo site referencing the published Umbraco.Automate package,
# plus project references to any provider packages that already exist in this repo.

set -e

# Determine repository root (parent of scripts folder)
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &>/dev/null && pwd )"
REPO_ROOT="$( cd "$SCRIPT_DIR/.." &>/dev/null && pwd )"

# Change to repository root to ensure consistent behavior
cd "$REPO_ROOT" || exit 1

# Parse arguments
SKIP_TEMPLATE_INSTALL=false
FORCE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-template-install|-s)
            SKIP_TEMPLATE_INSTALL=true
            shift
            ;;
        --force|-f)
            FORCE=true
            shift
            ;;
        --help|-h)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -s, --skip-template-install  Skip reinstalling Umbraco.Templates"
            echo "  -f, --force                  Recreate demo if it already exists"
            echo "  -h, --help                   Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo "========================================="
echo "Umbraco.Community.Automate Demo Site Setup"
echo "========================================="
echo "Working directory: $REPO_ROOT"
echo ""

# Check if demo already exists
if [ -d "demo" ] && [ "$FORCE" = false ]; then
    echo "Demo folder already exists. Use --force to recreate."
    echo "Or open the existing Umbraco.Community.Automate.local.slnx"
    exit 0
fi

# Clean up existing demo if Force
if [ "$FORCE" = true ] && [ -d "demo" ]; then
    echo "Removing existing demo folder..."
    rm -rf "demo"
fi

if [ "$FORCE" = true ] && [ -f "Umbraco.Community.Automate.local.slnx" ]; then
    rm -f "Umbraco.Community.Automate.local.slnx"
fi

# Step 1: Install Umbraco templates
if [ "$SKIP_TEMPLATE_INSTALL" = false ]; then
    echo "Installing Umbraco templates..."
    dotnet new install Umbraco.Templates --force
fi

# Step 2: Create demo folder with build overrides
echo "Creating demo folder..."
mkdir -p "demo"

# Disable package validation for demo folder
cp "$SCRIPT_DIR/templates/Directory.Build.props" "demo/Directory.Build.props"

# Disable central package management for demo folder
cp "$SCRIPT_DIR/templates/Directory.Packages.props" "demo/Directory.Packages.props"

# Step 3: Create the Umbraco demo site
echo "Creating Umbraco demo site..."
pushd "demo" > /dev/null
dotnet new umbraco --force -n "Umbraco.Automate.DemoSite" --friendly-name "Administrator" --email "admin@example.com" --password "password1234" --development-database-type SQLite
popd > /dev/null

# Step 3.1: Install Clean starter kit
echo "Installing Clean starter kit..."
pushd "demo/Umbraco.Automate.DemoSite" > /dev/null
dotnet add package Clean
popd > /dev/null

# Step 3.2: Set fixed port for consistent development
echo "Configuring fixed port (44380)..."
mkdir -p "demo/Umbraco.Automate.DemoSite/Properties"
cp "$SCRIPT_DIR/templates/launchSettings.json" "demo/Umbraco.Automate.DemoSite/Properties/launchSettings.json"

# Step 3.3: Add NamedPipeListenerComposer for HTTP over named pipes
echo "Adding NamedPipeListenerComposer for HTTP over named pipes..."
mkdir -p "demo/Umbraco.Automate.DemoSite/Composers"
cp "$SCRIPT_DIR/templates/NamedPipeListenerComposer.cs" "demo/Umbraco.Automate.DemoSite/Composers/NamedPipeListenerComposer.cs"

# Step 4: Create unified solution
echo "Creating unified solution..."
dotnet new sln -n "Umbraco.Community.Automate.local" --force

# Helper function to add all projects from a product's src and tests folders
add_product_projects() {
    local product_folder="$1"
    local solution_folder="$2"

    local count=0
    for sub in src tests; do
        local sub_path="$product_folder/$sub"
        if [ -d "$sub_path" ]; then
            while IFS= read -r -d '' proj; do
                local proj_name=$(basename "$proj")
                echo "  Adding $proj_name"
                dotnet sln "Umbraco.Community.Automate.local.slnx" add "$proj" --solution-folder "$solution_folder" 2>/dev/null || true
                ((count++))
            done < <(find "$sub_path" -name "*.csproj" -print0)
        fi
    done
    echo "  Added $count projects"
}

# Step 5: Discover and add each community provider package's projects.
# Unlike the main Umbraco.Automate repo (fixed list of internal products), this repo's
# package list grows as community packages are added — discover them dynamically by
# looking for top-level folders with a src/ subfolder, excluding non-package folders.
echo "Discovering provider packages..."
PROVIDER_PROJECT_REFS=()
for dir in */; do
    product_folder="${dir%/}"
    case "$product_folder" in
        demo|templates|scripts|.git|.claude|.github) continue ;;
    esac
    if [ -d "$product_folder/src" ]; then
        echo "Adding $product_folder projects..."
        add_product_projects "$product_folder" "$product_folder"

        # Track each package's main src csproj so the demo site can reference it directly.
        while IFS= read -r -d '' proj; do
            PROVIDER_PROJECT_REFS+=("$proj")
        done < <(find "$product_folder/src" -mindepth 2 -name "*.csproj" -print0)
    fi
done

# Step 6: Add demo site to solution
echo "Adding demo site to solution..."
dotnet sln "Umbraco.Community.Automate.local.slnx" add "demo/Umbraco.Automate.DemoSite/Umbraco.Automate.DemoSite.csproj" --solution-folder "Demo"

# Step 7: Reference the published Umbraco.Automate core package (no local project to reference)
DEMO_PROJECT="demo/Umbraco.Automate.DemoSite/Umbraco.Automate.DemoSite.csproj"
echo "Adding Umbraco.Automate package reference to demo site..."
dotnet add "$DEMO_PROJECT" package Umbraco.Automate --prerelease

# Step 8: Add a project reference to each discovered community provider package
for proj in "${PROVIDER_PROJECT_REFS[@]}"; do
    echo "  Referencing $(basename "$proj")"
    dotnet add "$DEMO_PROJECT" reference "$proj"
done

echo ""
echo "========================================="
echo "Setup Complete!"
echo "========================================="
echo ""
echo "Solution: Umbraco.Community.Automate.local.slnx"
echo "Demo site: demo/Umbraco.Automate.DemoSite"
echo ""
echo "Credentials:"
echo "  Email: admin@example.com"
echo "  Password: password1234"
echo ""
echo "Next steps:"
echo "  1. Open Umbraco.Community.Automate.local.slnx in your IDE"
echo "  2. Build the solution"
echo "  3. Run the Umbraco.Automate.DemoSite project"
echo ""
