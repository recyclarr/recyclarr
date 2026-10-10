#!/usr/bin/env bash
# Normalizes an upstream Sonarr/Radarr OpenAPI spec in place before it is committed:
# - Moves the /api/v3 prefix from every path into the server URL, so generated clients do not
#   repeat it at each call site. Paths outside /api/v3 (UI, login, feeds) are dropped because
#   they would resolve against the wrong base URL.
# - Keeps only application/json where Swashbuckle also lists text/json, text/plain, or
#   application/*+json for the same body.
# Input must be the raw upstream file; running it on an already normalized file fails.
set -euo pipefail

spec="${1:?usage: $0 <openapi.json>}"
prefix=/api/v3

if jq -e --arg p "$prefix" '.servers | any(.url | endswith($p))' "$spec" >/dev/null; then
  echo "error: $spec is already normalized" >&2
  exit 1
fi

tmp="$(mktemp)"
trap 'rm -f "$tmp"' EXIT

jq --arg p "$prefix" '
  def json_only:
    if has("application/json")
    then del(.["text/json", "text/plain", "application/*+json"])
    else .
    end;

  .servers[].url += $p
  | .paths |= with_entries(
      select(.key | startswith($p + "/"))
      | .key |= ltrimstr($p)
    )
  | .paths[][] |= (
      if type == "object" and has("responses") then
        (if .requestBody.content then .requestBody.content |= json_only else . end)
        | .responses[] |= (if .content then .content |= json_only else . end)
      else .
      end
    )
' "$spec" >"$tmp"

mv "$tmp" "$spec"
