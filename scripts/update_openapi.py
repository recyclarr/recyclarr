#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = ["pre-commit"]
# ///
"""Refresh vendored Sonarr/Radarr OpenAPI specs from upstream and normalize their formatting."""

import argparse
import json
import subprocess
import sys
import urllib.request
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
UPSTREAM = "https://raw.githubusercontent.com/{repo}/develop/src/{project}/openapi.json"

TARGETS = {
    "sonarr-v3": (
        "Sonarr/Sonarr",
        "Sonarr.Api.V3",
        "src/Recyclarr.Api.Sonarr/V3/openapi.json",
    ),
    "sonarr-v5": (
        "Sonarr/Sonarr",
        "Sonarr.Api.V5",
        "src/Recyclarr.Api.Sonarr/V5/openapi.json",
    ),
    "radarr-v3": (
        "Radarr/Radarr",
        "Radarr.Api.V3",
        "src/Recyclarr.Api.Radarr/openapi.json",
    ),
}


def run_prettier(path: Path) -> None:
    cmd = [sys.executable, "-m", "pre_commit", "run", "prettier", "--files", str(path)]
    # The first run exits nonzero when it rewrites the file; the second must pass cleanly.
    subprocess.run(cmd, cwd=REPO_ROOT, capture_output=True, check=False)
    subprocess.run(cmd, cwd=REPO_ROOT, check=True)


# Upstream specs mark nearly every object schema `additionalProperties: false`. That describes the
# server version the spec came from, not a promise that fields stay fixed: Sonarr and Radarr add
# fields between releases. With `false`, Refitter generates DTOs without `[JsonExtensionData]`, so
# fields unknown to the vendored spec are dropped when a resource is read. Gateways update by
# sending the fetched resource back, so a dropped field is reset on the server (this silently reset
# Sonarr media management settings). Removing `false` makes Refitter add an extension data property
# to every DTO, so unknown fields round-trip unchanged. Schema-valued `additionalProperties`
# (dictionaries) are kept. Remove this once Refitter can force extension data on its own.
def allow_unknown_fields(node: object) -> None:
    if isinstance(node, dict):
        if node.get("additionalProperties") is False:
            del node["additionalProperties"]
        for value in node.values():
            allow_unknown_fields(value)
    elif isinstance(node, list):
        for value in node:
            allow_unknown_fields(value)


# Swashbuckle lists each body under text/json, text/plain, and application/*+json beside
# application/json. Keeping only application/json removes noise from every sync diff without
# losing information.
def keep_json_media_type_only(spec: dict) -> None:
    def json_only(content: dict) -> None:
        if "application/json" in content:
            for media_type in ("text/json", "text/plain", "application/*+json"):
                content.pop(media_type, None)

    for path_item in spec.get("paths", {}).values():
        for operation in path_item.values():
            if not isinstance(operation, dict) or "responses" not in operation:
                continue
            if content := operation.get("requestBody", {}).get("content"):
                json_only(content)
            for response in operation["responses"].values():
                if content := response.get("content"):
                    json_only(content)


def update(target: str) -> None:
    repo, project, local = TARGETS[target]
    url = UPSTREAM.format(repo=repo, project=project)
    path = REPO_ROOT / local

    print(f"{target}: {url} -> {local}")
    with urllib.request.urlopen(url, timeout=60) as response:
        spec = json.load(response)

    allow_unknown_fields(spec)
    keep_json_media_type_only(spec)
    path.write_text(json.dumps(spec, indent=2) + "\n", encoding="utf-8")
    run_prettier(path)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "targets",
        nargs="*",
        choices=sorted(TARGETS),
        metavar="TARGET",
        help=f"Specs to update (default: all). Choices: {', '.join(sorted(TARGETS))}",
    )
    args = parser.parse_args()

    for target in args.targets or sorted(TARGETS):
        update(target)


if __name__ == "__main__":
    main()
