#!/usr/bin/env bash
# Regenerates the code blocks in a skill reference doc from real, compiled source files, so
# the doc can never silently drift from code that's actually built and tested.
#
# A code block is "owned" by this script when the line directly above its opening fence is
# exactly one backtick-quoted path, on its own line, e.g.:
#
#   `Actions/SendMessageAction.cs`
#   ```csharp
#   ...
#   ```
#
# The path is relative to <source dir>. Everything else in the markdown file (headings,
# prose, and any fenced block with no such marker above it, like a hand-written directory
# tree) is left untouched.
#
# Usage: tools/generate-skill-reference.sh <source dir> <markdown file> [--check]
#   e.g. tools/generate-skill-reference.sh Packages/WeatherApi/Umbraco.Community.Automate.WeatherApi \
#          .claude/skills/umbraco-community-automate/references/simple.md
#
# Without --check, rewrites <markdown file> in place. With --check, exits 1 and prints a
# diff if regenerating would change it, without writing anything; exits 0 if it's already
# up to date. Exits 2 (in both modes) on a usage error, a marker whose file doesn't exist, or
# an orphaned marker -- one not immediately followed by its fence (prose, another marker, or
# end of file in between) -- rather than silently leaving stale content in place.
set -euo pipefail

if [ $# -lt 2 ] || [ $# -gt 3 ]; then
  echo "Usage: $0 <source dir> <markdown file> [--check]" >&2
  exit 2
fi

source_dir="$1"
markdown_file="$2"
mode="${3:-}"

if [ -n "$mode" ] && [ "$mode" != "--check" ]; then
  echo "Unknown argument: $mode" >&2
  exit 2
fi

if [ ! -d "$source_dir" ]; then
  echo "Source directory not found: $source_dir" >&2
  exit 2
fi
if [ ! -f "$markdown_file" ]; then
  echo "Markdown file not found: $markdown_file" >&2
  exit 2
fi

# Fail fast, naming every missing file at once, rather than stopping at the first one.
missing=0
while IFS= read -r path; do
  [ -z "$path" ] && continue
  if [ ! -f "$source_dir/$path" ]; then
    echo "Marker references a file that doesn't exist: $source_dir/$path" >&2
    missing=1
  fi
done < <(grep -oE '^`[^`]+`$' "$markdown_file" | sed -E 's/^`(.*)`$/\1/')

if [ "$missing" -eq 1 ]; then
  exit 2
fi

generated="$(mktemp)"
trap 'rm -f "$generated"' EXIT

# A marker must be immediately followed by its fence: a marker followed by prose, by another
# marker, or by end of file is an error, not a silent no-op, so a future hand-edit that orphans
# a marker fails loudly here (and in --check) instead of quietly freezing stale content forever.
awk -v srcdir="$source_dir" '
  BEGIN { pending = ""; pending_line = 0; owned = 0 }
  {
    if (owned == 1) {
      if ($0 ~ /^```/) { print; owned = 0 }
      next
    }
    if ($0 ~ /^`[^`]+`$/) {
      if (pending != "") {
        print "Marker on line " pending_line " (`" pending "`) is immediately followed by another marker on line " NR ", not a fenced code block" > "/dev/stderr"
        pending = ""
        exit 2
      }
      pending = substr($0, 2, length($0) - 2)
      pending_line = NR
      print
      next
    }
    if (pending != "") {
      if ($0 ~ /^```/) {
        print
        path = srcdir "/" pending
        while ((getline content < path) > 0) print content
        close(path)
        pending = ""
        owned = 1
        next
      }
      print "Marker on line " pending_line " (`" pending "`) is not immediately followed by a fenced code block (line " NR " isn'\''t one)" > "/dev/stderr"
      pending = ""
      exit 2
    }
    print
  }
  END {
    if (pending != "") {
      print "Marker on line " pending_line " (`" pending "`) is not followed by anything -- end of file reached before a fenced code block" > "/dev/stderr"
      exit 2
    }
  }
' "$markdown_file" > "$generated"

if [ "$mode" = "--check" ]; then
  if diff -q "$markdown_file" "$generated" > /dev/null; then
    exit 0
  fi
  echo "Regenerating would change $markdown_file:" >&2
  diff -u "$markdown_file" "$generated" || true
  exit 1
fi

cp "$generated" "$markdown_file"
echo "Regenerated $markdown_file from $source_dir"
