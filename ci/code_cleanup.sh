#!/usr/bin/env bash
ref="$1"
changed_files="$(git diff --relative --name-only --diff-filter=d $ref... | egrep '\.cs$')"

if [[ -z "$changed_files" ]]; then
  echo "No changed files detected; skipping code cleanup"
  exit 0
fi

echo '--------------------------------------------------'
echo 'Files to be checked for code cleanup:'
echo
echo "$changed_files"
echo '--------------------------------------------------'

jb cleanupcode Recyclarr.sln \
  --profile="Recyclarr Cleanup" \
  --include="$(echo "$changed_files" | tr '\n' ';')"
