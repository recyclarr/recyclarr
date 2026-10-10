#!/usr/bin/env bash
# Normalizes an upstream Sonarr/Radarr OpenAPI spec in place before it is committed. Swashbuckle
# lists each body under text/json, text/plain, and application/*+json beside application/json;
# only application/json is kept. Running it again on a normalized file changes nothing.
set -euo pipefail

spec="${1:?usage: $0 <openapi.json>}"

tmp="$(mktemp)"
trap 'rm -f "$tmp"' EXIT

jq '
  def json_only:
    if has("application/json")
    then del(.["text/json", "text/plain", "application/*+json"])
    else .
    end;

  .paths[][] |= (
      if type == "object" and has("responses") then
        (if .requestBody.content then .requestBody.content |= json_only else . end)
        | .responses[] |= (if .content then .content |= json_only else . end)
      else .
      end
    )
' "$spec" >"$tmp"

mv "$tmp" "$spec"
