#!/usr/bin/env bash
#
# Lefthook pre-push hook: scans the commit range that is actually about to be
# pushed for leaked secrets, using gitleaks.
#
# Why this exists as a script (not an inline `gitleaks protect` command):
# `gitleaks protect` only ever looks at the *uncommitted working-tree diff*
# (like `git diff`). It never inspects commits, so a secret committed with
# `git commit --no-verify` on an otherwise-clean working tree would sail
# through undetected - defeating the point of a pre-push safety net. Instead
# this script uses `gitleaks detect --log-opts=<range>`, which asks gitleaks
# to run `git log <range>` and scan the diff of every commit it returns -
# i.e. the actual commits being pushed.
#
# Git invokes pre-push hooks with the remote name and URL as $1/$2, and
# feeds one line per ref being pushed on stdin, in the form:
#   <local ref> <local sha1> <remote ref> <remote sha1>
# (see githooks(5), "pre-push"). Lefthook only forwards stdin to this script
# because `use_stdin: true` is set for it in lefthook.yml.

set -euo pipefail

remote_name="$1"
# remote_url="$2" # unused, kept for documentation of the pre-push protocol

zero_sha="0000000000000000000000000000000000000000"
overall_status=0

while read -r local_ref local_sha remote_ref remote_sha; do
  # Deleting a remote ref locally has nothing to scan.
  if [[ "$local_sha" == "$zero_sha" ]]; then
    continue
  fi

  if [[ "$remote_sha" != "$zero_sha" ]]; then
    # The remote already has this ref: scan exactly the commits being added
    # on top of what it already has.
    range="${remote_sha}..${local_sha}"
  else
    # Brand new ref on the remote - there is no remote commit to diff
    # against. Fall back to the merge-base with the remote's default branch
    # (determined from local remote-tracking refs only, no network calls),
    # and scan from there. If no default branch/merge-base can be
    # established (e.g. this is the very first push to an empty remote, or
    # an unrelated history), fall back to scanning this ref's entire
    # history so nothing goes unchecked.
    default_branch=""
    if git symbolic-ref -q "refs/remotes/${remote_name}/HEAD" >/dev/null 2>&1; then
      default_branch="$(git symbolic-ref -q --short "refs/remotes/${remote_name}/HEAD" | sed "s#^${remote_name}/##")"
    fi
    if [[ -z "$default_branch" ]]; then
      for candidate in main master; do
        if git rev-parse --verify -q "refs/remotes/${remote_name}/${candidate}" >/dev/null 2>&1; then
          default_branch="$candidate"
          break
        fi
      done
    fi

    range=""
    if [[ -n "$default_branch" ]]; then
      merge_base="$(git merge-base "refs/remotes/${remote_name}/${default_branch}" "$local_sha" 2>/dev/null || true)"
      if [[ -n "$merge_base" ]]; then
        range="${merge_base}..${local_sha}"
      fi
    fi
    if [[ -z "$range" ]]; then
      range="$local_sha"
    fi
  fi

  echo "gitleaks: scanning ${local_ref} (${range}) before push to ${remote_name}"
  if ! gitleaks detect --source . --config .gitleaks.toml --redact --log-opts="${range}" --verbose; then
    overall_status=1
  fi
done

exit "$overall_status"
