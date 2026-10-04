#!/usr/bin/env bash
# Checks a package works on every Umbraco major it claims to support (see Directory.Packages.props).
#
#   1. Build and test against Umbraco 17 (what the package is compiled against and ships).
#   2. Build and test against Umbraco 18 (source compatibility).
#   3. Run the Umbraco 18 test build with the DLL compiled against 17 swapped in. This is
#      what an Umbraco 18 site actually runs, and catches members that moved between
#      majors (MissingMethodException), which step 2 can't see.
#   4. Check every Umbraco type/member the 17 build references exists in the 18 assemblies,
#      covering the code paths the tests in step 3 don't reach.
#
# Usage: tools/test-umbraco-compat.sh <package project dir>
#   e.g. tools/test-umbraco-compat.sh Packages/Mastodon/Umbraco.Community.Automate.Mastodon
# Runs from anywhere; needs the .NET 10 SDK and bash (Git Bash on Windows).
set -euo pipefail

if [ $# -ne 1 ] || [ ! -d "$1" ]; then
  echo "Usage: $0 <package project dir>" >&2
  exit 2
fi

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
package="$(cd "$1" && pwd)"
tests="$package.Tests"
dll="$(basename "$package").dll"
shipped="$(mktemp -d)"
trap 'rm -rf "$shipped"' EXIT

echo "::group::Umbraco 17"
dotnet test "$tests" -c Release -p:UmbracoMajor=17
cp "$package/bin/Release/net10.0/$dll" "$shipped/"
echo "::endgroup::"

echo "::group::Umbraco 18"
dotnet test "$tests" -c Release -p:UmbracoMajor=18
echo "::endgroup::"

echo "::group::Umbraco 17 build running on Umbraco 18"
cp "$shipped/$dll" "$tests/bin/Release/net10.0/"
dotnet test "$tests" -c Release -p:UmbracoMajor=18 --no-build
echo "::endgroup::"

echo "::group::Umbraco references in the 17 build resolve on Umbraco 18"
# ASP.NET Core's shared framework, which Umbraco's assemblies reference. Lines look like
# "Microsoft.AspNetCore.App 10.0.10 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]",
# and the path can contain spaces, so take everything between the brackets.
aspnet="$(dotnet --list-runtimes | tr -d '\r' | grep -E '^Microsoft\.AspNetCore\.App 10\.' | tail -1 | sed -E 's/^[^ ]+ ([^ ]+) \[(.*)\]$/\2\/\1/')"
dotnet run --project "$here/ReferenceCheck" -c Release -- \
  "$shipped/$dll" "$tests/bin/Release/net10.0" "$aspnet"
echo "::endgroup::"

# Leave the working tree built for the default (17) again.
dotnet restore "$tests" -p:UmbracoMajor=17 > /dev/null
