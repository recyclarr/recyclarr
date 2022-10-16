#!/usr/bin/env bash
files="$(git diff --name-only)"

for file in $files; do
  echo "File: $file"
  diffoutput="$(git diff $file | sed -z 's/\n/%0A/g')"
  echo "::error file=$file,title=Code Cleanup Needed In File::$diffoutput"
done
