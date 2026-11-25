#!/usr/bin/env bash
set -euo pipefail

# Inputs (environment):
#   NEEDS          JSON object of needed jobs, each with a "result" field
#   ALLOWED_SKIPS  whitespace-separated job IDs that may be skipped

jq -r 'to_entries[] | "\(.key): \(.value.result)"' <<<"$NEEDS"

failed=$(jq -r --arg skips "${ALLOWED_SKIPS:-}" '
  ($skips | [splits("\\s+")] | map(select(. != ""))) as $allowed
  | to_entries[]
  | select(.value.result != "success")
  | select(.value.result != "skipped" or (.key | IN($allowed[]) | not))
  | .key' <<<"$NEEDS")

if [[ -n $failed ]]; then
  echo "::error::Jobs did not succeed: ${failed//$'\n'/, }"
  exit 1
fi
