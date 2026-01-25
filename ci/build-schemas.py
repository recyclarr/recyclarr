#!/usr/bin/env python3
"""Build versioned schema directory for deployment to Cloudflare Pages."""

import argparse
import shutil
import subprocess
import sys
from collections import defaultdict
from pathlib import Path

MAJOR_VERSIONS_TO_DEPLOY = 3
SCHEMAS_SOURCE_DIR = "schemas"
OUTPUT_DIR = Path("schemas-dist")


def run(cmd: list[str], **kwargs) -> subprocess.CompletedProcess[str]:
    """Run a command and return the result."""
    return subprocess.run(cmd, capture_output=True, text=True, check=True, **kwargs)


def get_tags() -> list[str]:
    """Get all version tags from git."""
    result = run(["git", "tag", "-l", "v*"])
    return [t for t in result.stdout.strip().split("\n") if t]


def parse_version(tag: str) -> tuple[int, int, int] | None:
    """Parse vX.Y.Z tag into (major, minor, patch) tuple."""
    if not tag.startswith("v"):
        return None
    try:
        parts = tag[1:].split(".")
        if len(parts) != 3:
            return None
        return int(parts[0]), int(parts[1]), int(parts[2])
    except ValueError:
        return None


def group_by_major_minor(
    tags: list[str],
) -> dict[int, dict[tuple[int, int], str]]:
    """
    Group tags by major version, then find latest patch for each minor.
    Returns: {major: {(major, minor): latest_tag}}
    """
    minor_versions: dict[tuple[int, int], list[tuple[int, str]]] = defaultdict(list)

    for tag in tags:
        version = parse_version(tag)
        if version is None:
            continue
        major, minor, patch = version
        minor_versions[(major, minor)].append((patch, tag))

    result: dict[int, dict[tuple[int, int], str]] = defaultdict(dict)
    for (major, minor), patches in minor_versions.items():
        latest_patch_tag = max(patches, key=lambda x: x[0])[1]
        result[major][(major, minor)] = latest_patch_tag

    return result


def get_files_at_tag(tag: str, path: str) -> list[str]:
    """List files in a directory at a specific tag."""
    try:
        result = run(["git", "ls-tree", "-r", "--name-only", tag, path])
        return [f for f in result.stdout.strip().split("\n") if f]
    except subprocess.CalledProcessError:
        return []


def copy_schemas_from_tag(tag: str, dest_dir: Path, dry_run: bool) -> int:
    """Extract schemas from a tag to destination directory. Returns file count."""
    files = get_files_at_tag(tag, SCHEMAS_SOURCE_DIR)
    if not files:
        print(f"  Warning: {SCHEMAS_SOURCE_DIR}/ not found at {tag}, skipping")
        return 0

    if dry_run:
        print(f"  Would copy {len(files)} files to {dest_dir}")
        return len(files)

    dest_dir.mkdir(parents=True, exist_ok=True)

    for file_path in files:
        relative_path = Path(file_path).relative_to(SCHEMAS_SOURCE_DIR)
        dest_file = dest_dir / relative_path

        result = run(["git", "show", f"{tag}:{file_path}"])

        dest_file.parent.mkdir(parents=True, exist_ok=True)
        dest_file.write_text(result.stdout)

    print(f"  Copied {len(files)} files to {dest_dir}")
    return len(files)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Build versioned schema directory for Cloudflare Pages deployment."
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print what would be done without writing any files",
    )
    args = parser.parse_args()

    if args.dry_run:
        print("=== DRY RUN MODE (no files will be written) ===\n")

    tags = get_tags()
    if not tags:
        print("No version tags found")
        return 1

    grouped = group_by_major_minor(tags)
    if not grouped:
        print("No valid version tags found")
        return 1

    top_majors = sorted(grouped.keys(), reverse=True)[:MAJOR_VERSIONS_TO_DEPLOY]
    print(f"Major versions to deploy: {top_majors}")

    if not args.dry_run:
        if OUTPUT_DIR.exists():
            shutil.rmtree(OUTPUT_DIR)
        OUTPUT_DIR.mkdir()

    all_versions: list[tuple[tuple[int, int, int], str]] = []
    for major in top_majors:
        for (maj, minor), tag in grouped[major].items():
            version = parse_version(tag)
            if version:
                all_versions.append((version, tag))

    latest_tag = max(all_versions, key=lambda x: x[0])[1]
    latest_version = parse_version(latest_tag)
    assert latest_version is not None

    print(f"Latest version: {latest_tag}")
    print(f"Output directory: {OUTPUT_DIR}/")

    total_dirs = 0
    total_files = 0

    for major in top_majors:
        minors = grouped[major]
        latest_minor_tag = max(
            minors.items(), key=lambda x: (x[0][1], parse_version(x[1]))
        )[1]

        print(f"\nv{major} (latest: {latest_minor_tag})")

        print(f"  v{major}/ <- {latest_minor_tag}")
        total_files += copy_schemas_from_tag(
            latest_minor_tag, OUTPUT_DIR / f"v{major}", args.dry_run
        )
        total_dirs += 1

        for (maj, minor), tag in sorted(minors.items()):
            print(f"  v{maj}.{minor}/ <- {tag}")
            total_files += copy_schemas_from_tag(
                tag, OUTPUT_DIR / f"v{maj}.{minor}", args.dry_run
            )
            total_dirs += 1

    print(f"\nlatest/ <- {latest_tag}")
    total_files += copy_schemas_from_tag(
        latest_tag, OUTPUT_DIR / "latest", args.dry_run
    )
    total_dirs += 1

    print(f"\nSummary: {total_dirs} directories, {total_files} total files")

    if args.dry_run:
        print("\n=== DRY RUN COMPLETE (no files written) ===")
    else:
        print(f"\nSchema build complete. Output in {OUTPUT_DIR}/")

    return 0


if __name__ == "__main__":
    sys.exit(main())
