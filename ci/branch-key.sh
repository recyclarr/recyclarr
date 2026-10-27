#!/usr/bin/env bash
# Prints one key per branch name argument. The key tags the dev image and versions the dev chart
# (0.0.0-<key>), so it must be valid as both a Docker tag and a SemVer prerelease identifier.
# The dev image and dev chart publish jobs both call this script so their tags match.
set -euo pipefail

for branch in "$@"; do
    key=$(printf '%s' "$branch" | tr -c '0-9A-Za-z-' '-')
    # Docker tags cannot start with "-" and SemVer rejects numeric identifiers with leading zeros;
    # a letter prefix avoids both.
    [[ $key =~ ^[A-Za-z] ]] || key="b-$key"
    # OCI tags allow 128 characters; the chart tag adds the "0.0.0-" prefix.
    echo "${key:0:120}"
done
