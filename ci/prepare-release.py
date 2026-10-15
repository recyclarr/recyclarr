#!/usr/bin/env python3
"""Release helpers for the Prepare, Tag, and Recover Release workflows."""

import argparse
import json
import os
import re
import subprocess
import sys
from datetime import date
from pathlib import Path

REPO_ROOT = Path(__file__).parent.parent.resolve()
CHANGELOG_PATH = REPO_ROOT / "CHANGELOG.md"

# ANSI colors (no third-party deps)
RED = "\033[31m"
CYAN = "\033[36m"
BOLD = "\033[1m"
DIM = "\033[2m"
RESET = "\033[0m"

UNRELEASED_HEADING = "## [Unreleased]"
VERSION_HEADING_RE = re.compile(r"^## \[(\d+\.\d+\.\d+)]")
UNRELEASED_LINK_RE = re.compile(r"^\[Unreleased]:\s+(.+)/compare/v.+\.\.\.HEAD$")
VERSION_LINK_RE = re.compile(r"^\[(\d+\.\d+\.\d+)]:\s+")
SEMVER_RE = re.compile(r"^\d+\.\d+\.\d+$")


def error(msg: str) -> None:
    print(f"{RED}{BOLD}error:{RESET} {msg}", file=sys.stderr)


def info(label: str, value: str) -> None:
    print(f"  {CYAN}{label}{RESET} {value}")


def run_quiet(
    args: list[str], *, cwd: Path = REPO_ROOT
) -> subprocess.CompletedProcess[str]:
    """Run a subprocess, capturing all output."""
    return subprocess.run(args, capture_output=True, text=True, cwd=cwd)


def run_or_die(
    args: list[str], msg: str, *, cwd: Path = REPO_ROOT
) -> subprocess.CompletedProcess[str]:
    """Run a subprocess; exit with an error message if it fails."""
    result = run_quiet(args, cwd=cwd)
    if result.returncode != 0:
        error(msg)
        stderr = result.stderr.strip()
        if stderr:
            print(f"  {DIM}{stderr}{RESET}", file=sys.stderr)
        sys.exit(1)
    return result


def resolve_version() -> str:
    """Return the release version from gitversion."""
    result = run_or_die(
        ["dotnet", "gitversion", "/showvariable", "MajorMinorPatch"],
        "dotnet gitversion failed",
    )

    version = result.stdout.strip()
    if not version or not SEMVER_RE.match(version):
        error(f"gitversion returned unexpected value: {version!r}")
        sys.exit(1)

    return version


def parse_changelog(lines: list[str]) -> tuple[int, int, str | None]:
    """Find the Unreleased section boundaries and the previous version.

    Returns (unreleased_heading_index, next_version_heading_index, previous_version).
    next_version_heading_index points to the next ## [x.y.z] line, or len(lines) if none exists.
    """
    unreleased_idx: int | None = None
    next_heading_idx: int | None = None
    prev_version: str | None = None

    for i, line in enumerate(lines):
        if line.rstrip() == UNRELEASED_HEADING:
            unreleased_idx = i
            continue

        if unreleased_idx is not None and next_heading_idx is None:
            m = VERSION_HEADING_RE.match(line)
            if m:
                next_heading_idx = i
                prev_version = m.group(1)
                break

    if unreleased_idx is None:
        error("CHANGELOG.md has no ## [Unreleased] section")
        sys.exit(1)

    if next_heading_idx is None:
        next_heading_idx = len(lines)

    return unreleased_idx, next_heading_idx, prev_version


def parse_version_section(
    lines: list[str], version: str
) -> tuple[int, int, str | None] | None:
    """Find a specific version's section boundaries and its predecessor.

    Returns (heading_index, next_heading_index, previous_version) or None if not found.
    """
    version_idx: int | None = None

    for i, line in enumerate(lines):
        m = VERSION_HEADING_RE.match(line)
        if not m:
            continue

        if version_idx is not None:
            # This is the next version heading after the one we found
            return version_idx, i, m.group(1)

        if m.group(1) == version:
            version_idx = i

    if version_idx is not None:
        return version_idx, len(lines), None

    return None


def unreleased_has_content(lines: list[str], start: int, end: int) -> bool:
    """Check whether there are meaningful entries between the Unreleased heading and the next."""
    for line in lines[start + 1 : end]:
        stripped = line.strip()
        if stripped and not stripped.startswith("###"):
            return True
    return False


def extract_repo_url(lines: list[str]) -> str | None:
    """Derive the repository base URL from the existing [Unreleased] link reference."""
    for line in lines:
        m = UNRELEASED_LINK_RE.match(line.rstrip())
        if m:
            return m.group(1)
    return None


def transform_changelog(
    lines: list[str], version: str, prev_version: str | None, repo_url: str
) -> list[str]:
    """Produce the updated changelog lines."""
    unreleased_idx, next_heading_idx, _ = parse_changelog(lines)
    today = date.today().isoformat()

    # Build the replacement for the Unreleased heading area:
    # - Fresh empty Unreleased section
    # - New versioned heading
    new_heading_block = [
        UNRELEASED_HEADING,
        "",
        f"## [{version}] - {today}",
    ]

    result = lines[:unreleased_idx] + new_heading_block + lines[unreleased_idx + 1 :]

    # Update link references at the bottom of the file
    result = update_link_references(result, version, prev_version, repo_url)

    # Ensure single trailing newline
    while result and result[-1].strip() == "":
        result.pop()
    result.append("")

    return result


def update_link_references(
    lines: list[str], version: str, prev_version: str | None, repo_url: str
) -> list[str]:
    """Update the [Unreleased] link and insert the new version link."""
    new_unreleased_link = f"[Unreleased]: {repo_url}/compare/v{version}...HEAD"
    new_version_link = (
        f"[{version}]: {repo_url}/compare/v{prev_version}...v{version}"
        if prev_version
        else f"[{version}]: {repo_url}/releases/tag/v{version}"
    )

    result: list[str] = []
    inserted_version_link = False

    for line in lines:
        stripped = line.rstrip()

        if UNRELEASED_LINK_RE.match(stripped):
            # Replace existing Unreleased link
            result.append(new_unreleased_link)

            # Insert new version link immediately after
            if not inserted_version_link:
                result.append(new_version_link)
                inserted_version_link = True
            continue

        result.append(line)

    return result


def detect_mainline() -> str:
    """Detect the mainline branch name from the remote (main or master)."""
    for candidate in ("main", "master"):
        result = run_quiet(["git", "rev-parse", "--verify", f"origin/{candidate}"])
        if result.returncode == 0:
            return candidate
    error(
        "could not detect mainline branch (neither origin/main nor origin/master exists)"
    )
    sys.exit(1)


def require_mainline_branch(mainline: str) -> None:
    """Exit if the current branch is not the mainline."""
    result = run_quiet(["git", "branch", "--show-current"])
    current = result.stdout.strip()
    if current != mainline:
        error(f"must be on {mainline} branch to release (currently on {current})")
        sys.exit(1)


def require_workflows_healthy(mainline: str) -> None:
    """Exit if any workflow runs on the mainline branch are pending or failed."""
    # Check for in-progress or queued runs
    pending = []
    for status in ("in_progress", "queued"):
        result = run_or_die(
            [
                "gh",
                "run",
                "list",
                "--branch",
                mainline,
                "--status",
                status,
                "--json",
                "databaseId,name,status",
            ],
            "failed to query GitHub workflow runs",
        )
        pending += json.loads(result.stdout)

    current_run_id = os.environ.get("GITHUB_RUN_ID")
    if current_run_id:
        pending = [run for run in pending if str(run["databaseId"]) != current_run_id]

        for run in pending:
            run_or_die(
                ["gh", "run", "watch", str(run["databaseId"]), "--exit-status"],
                f"{run['name']} workflow failed",
            )
        pending = []

    if pending:
        error(f"there are pending workflow runs on {mainline}:")
        for run in pending:
            print(f"  {DIM}- {run['name']} ({run['status']}){RESET}", file=sys.stderr)
        sys.exit(1)

    # Check that the latest run for each workflow has not failed
    result = run_or_die(
        [
            "gh",
            "run",
            "list",
            "--branch",
            mainline,
            "--json",
            "databaseId,name,conclusion,workflowName",
            "--limit",
            "20",
        ],
        "failed to query GitHub workflow runs",
    )

    runs = json.loads(result.stdout)

    # Group by workflow name; the first occurrence is the latest
    latest_by_workflow: dict[str, dict] = {}
    for run in runs:
        wf = run["workflowName"]
        if wf not in latest_by_workflow:
            latest_by_workflow[wf] = run

    failed = [
        r
        for r in latest_by_workflow.values()
        if r["conclusion"] in ("failure", "timed_out", "cancelled")
    ]

    if failed:
        error(f"latest workflow runs on {mainline} have failures:")
        for run in failed:
            print(
                f"  {DIM}- {run['workflowName']} ({run['conclusion']}){RESET}",
                file=sys.stderr,
            )
        sys.exit(1)


def require_clean_worktree() -> None:
    """Exit if the working copy has uncommitted changes."""
    unstaged = run_quiet(["git", "diff", "--quiet"])
    staged = run_quiet(["git", "diff", "--cached", "--quiet"])
    if unstaged.returncode != 0 or staged.returncode != 0:
        error("working copy has uncommitted changes; commit or stash them first")
        sys.exit(1)


def recovery_info(mainline: str) -> dict[str, str]:
    """Return the only release tag eligible to move to the mainline HEAD."""
    require_mainline_branch(mainline)

    lines = CHANGELOG_PATH.read_text().splitlines()
    unreleased_idx, next_heading_idx, version = parse_changelog(lines)
    if unreleased_has_content(lines, unreleased_idx, next_heading_idx):
        error("## [Unreleased] must be empty to recover a release")
        sys.exit(1)

    if version is None:
        error("CHANGELOG.md has no released version to recover")
        sys.exit(1)

    section = parse_version_section(lines, version)
    if section is None or section[2] is None:
        error(f"could not determine the version before {version}")
        sys.exit(1)

    previous_version = section[2]
    tag = f"v{version}"
    tags = run_or_die(["git", "tag", "--list", "v*"], "failed to list tags")
    stable_tags = [
        candidate
        for candidate in tags.stdout.splitlines()
        if SEMVER_RE.match(candidate.removeprefix("v"))
    ]
    if tag not in stable_tags:
        error(f"recovery tag {tag} does not exist")
        sys.exit(1)

    newest_tag = max(
        stable_tags,
        key=lambda candidate: tuple(
            int(part) for part in candidate.removeprefix("v").split(".")
        ),
    )
    if tag != newest_tag:
        error(f"latest changelog version {tag} is not the newest tag ({newest_tag})")
        sys.exit(1)

    tag_sha = run_or_die(
        ["git", "rev-parse", f"{tag}^{{commit}}"],
        f"failed to resolve {tag}",
    ).stdout.strip()
    head_sha = run_or_die(["git", "rev-parse", "HEAD"], "failed to resolve HEAD")
    head_sha = head_sha.stdout.strip()

    if tag_sha == head_sha:
        error(f"{tag} already points to the current {mainline} commit")
        sys.exit(1)

    ancestor = run_quiet(["git", "merge-base", "--is-ancestor", tag_sha, head_sha])
    if ancestor.returncode != 0:
        error(f"{tag} does not point to an ancestor of {mainline}")
        sys.exit(1)

    return {
        "version": version,
        "previous_version": previous_version,
        "tag_sha": tag_sha,
        "head_sha": head_sha,
    }


def format_version_label(version: str, prev_version: str | None) -> str:
    """Format the version line, e.g. 'v8.2.1 -> v8.3.0'."""
    if prev_version:
        return f"v{prev_version} -> v{version}"
    return f"v{version}"


SEPARATOR = f"{DIM}{'─' * 60}{RESET}"


def print_changelog_preview(
    lines: list[str],
    version: str,
    prev_version: str | None,
    start: int | None = None,
    end: int | None = None,
) -> None:
    """Print the release content as it will appear in the changelog.

    When start/end are provided, use those section boundaries directly (retroactive mode).
    Otherwise, locate the Unreleased section.
    """
    if start is None or end is None:
        start, end, _ = parse_changelog(lines)
    today = date.today().isoformat()

    print(SEPARATOR)
    print(f"{CYAN}## [{version}] - {today}{RESET}")
    for line in lines[start + 1 : end]:
        print(line)
    print(f"{DIM}Links:{RESET}")
    print(f"  {DIM}[Unreleased]: ...compare/v{version}...HEAD{RESET}")
    if prev_version:
        print(f"  {DIM}[{version}]: ...compare/v{prev_version}...v{version}{RESET}")
    else:
        print(f"  {DIM}[{version}]: ...releases/tag/v{version}{RESET}")
    print(SEPARATOR)


def print_dry_run(
    lines: list[str],
    version: str,
    prev_version: str | None,
    start: int | None = None,
    end: int | None = None,
) -> None:
    """Show a preview of the release that would be created."""
    print(f"{BOLD}Dry run: {format_version_label(version, prev_version)}{RESET}")
    print()
    print_changelog_preview(lines, version, prev_version, start, end)
    print()
    print(f"{DIM}No files were modified.{RESET}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Prepare a Recyclarr release.")
    modes = parser.add_mutually_exclusive_group(required=True)
    modes.add_argument(
        "--dry-run",
        nargs="?",
        const=True,
        default=False,
        metavar="VERSION",
        help="preview without writing; optionally replay a previous release (e.g. --dry-run 8.6.0)",
    )
    modes.add_argument(
        "--update-changelog",
        action="store_true",
        help="update CHANGELOG.md without committing or tagging; print the version",
    )
    modes.add_argument(
        "--verify",
        metavar="VERSION",
        help="verify that CHANGELOG.md and GitVersion match a prepared release",
    )
    modes.add_argument(
        "--check-workflows",
        action="store_true",
        help="verify that mainline workflows are complete and successful",
    )
    modes.add_argument(
        "--recovery-info",
        action="store_true",
        help="print JSON describing the release eligible for recovery",
    )
    args = parser.parse_args()

    mainline = detect_mainline()

    if args.check_workflows:
        require_workflows_healthy(mainline)
        return

    if args.recovery_info:
        print(json.dumps(recovery_info(mainline)))
        return

    if args.verify is not None:
        version = args.verify
        if not SEMVER_RE.match(version):
            error(f"invalid version format: {version} (expected X.Y.Z)")
            sys.exit(1)

        resolved_version = resolve_version()
        if resolved_version != version:
            error(
                f"GitVersion resolved {resolved_version}, but the release expects {version}"
            )
            sys.exit(1)

        lines = CHANGELOG_PATH.read_text().splitlines()
        unreleased_idx, next_heading_idx, changelog_version = parse_changelog(lines)
        if unreleased_has_content(lines, unreleased_idx, next_heading_idx):
            error("## [Unreleased] must be empty after preparing a release")
            sys.exit(1)

        if changelog_version != version:
            error(
                f"latest changelog version is {changelog_version!r}, expected {version}"
            )
            sys.exit(1)

        return

    # --dry-run VERSION replays a previous release retroactively
    if args.dry_run and args.dry_run is not True:
        replay_version = args.dry_run
        if not SEMVER_RE.match(replay_version):
            error(f"invalid version format: {replay_version} (expected X.Y.Z)")
            sys.exit(1)

        text = CHANGELOG_PATH.read_text()
        lines = text.splitlines()

        existing = parse_version_section(lines, replay_version)
        if not existing:
            error(f"version {replay_version} not found in CHANGELOG.md")
            sys.exit(1)

        heading_idx, next_idx, prev_version = existing
        print_dry_run(lines, replay_version, prev_version, heading_idx, next_idx)
        require_workflows_healthy(mainline)
        return

    require_mainline_branch(mainline)

    version = resolve_version()

    text = CHANGELOG_PATH.read_text()
    lines = text.splitlines()

    # Validate
    unreleased_idx, next_heading_idx, prev_version = parse_changelog(lines)

    if not unreleased_has_content(lines, unreleased_idx, next_heading_idx):
        error("## [Unreleased] section has no entries")
        sys.exit(1)

    # Check for duplicate version
    for line in lines:
        if VERSION_HEADING_RE.match(line) and line.startswith(f"## [{version}]"):
            error(f"version {version} already exists in CHANGELOG.md")
            sys.exit(1)

    repo_url = extract_repo_url(lines)
    if not repo_url:
        error("could not determine repository URL from [Unreleased] link reference")
        sys.exit(1)

    # Transform
    new_lines = transform_changelog(lines, version, prev_version, repo_url)
    new_text = "\n".join(new_lines)

    if args.dry_run:
        print_dry_run(lines, version, prev_version)
        require_workflows_healthy(mainline)
        return

    require_clean_worktree()
    CHANGELOG_PATH.write_text(new_text)
    print(version)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print()
        sys.exit(130)
    except Exception as exc:
        error(str(exc))
        sys.exit(1)
