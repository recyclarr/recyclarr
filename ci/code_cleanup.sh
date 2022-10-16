#!/usr/bin/env bash

changed_files="$(git diff --relative --name-only origin/master... | egrep '\.cs$')"

echo '--------------------------------------------------'
echo 'Files to be checked for code cleanup:'
echo
echo "$changed_files"
echo '--------------------------------------------------'

jb cleanupcode Recyclarr.sln \
  --profile="Recyclarr Cleanup" \
  --include="$(echo "$changed_files" | tr '\n' ';')"
