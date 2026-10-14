#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = ["pre-commit"]
# ///
"""Refresh vendored Sonarr/Radarr OpenAPI specs from upstream and normalize their formatting."""

import argparse
import subprocess
import sys
import urllib.request
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
UPSTREAM = "https://raw.githubusercontent.com/{repo}/develop/src/{project}/openapi.json"

TARGETS = {
    "sonarr-v3": ("Sonarr/Sonarr", "Sonarr.Api.V3", "src/Recyclarr.Api.Sonarr/V3/openapi.json"),
    "sonarr-v5": ("Sonarr/Sonarr", "Sonarr.Api.V5", "src/Recyclarr.Api.Sonarr/V5/openapi.json"),
    "radarr-v3": ("Radarr/Radarr", "Radarr.Api.V3", "src/Recyclarr.Api.Radarr/openapi.json"),
}


def run_prettier(path: Path) -> None:
    cmd = [sys.executable, "-m", "pre_commit", "run", "prettier", "--files", str(path)]
    # The first run exits nonzero when it rewrites the file; the second must pass cleanly.
    subprocess.run(cmd, cwd=REPO_ROOT, capture_output=True, check=False)
    subprocess.run(cmd, cwd=REPO_ROOT, check=True)


def update(target: str) -> None:
    repo, project, local = TARGETS[target]
    url = UPSTREAM.format(repo=repo, project=project)
    path = REPO_ROOT / local

    print(f"{target}: {url} -> {local}")
    with urllib.request.urlopen(url, timeout=60) as response:
        path.write_bytes(response.read())

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
