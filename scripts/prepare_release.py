#!/usr/bin/env python3
"""Prepare a Recyclarr release: update CHANGELOG.md, commit, tag, and optionally push."""

import argparse
import re
import subprocess
import sys
from datetime import date
from pathlib import Path

REPO_ROOT = Path(__file__).parent.parent.resolve()
CHANGELOG_PATH = REPO_ROOT / "CHANGELOG.md"

# ANSI colors (no third-party deps)
RED = "\033[31m"
GREEN = "\033[32m"
YELLOW = "\033[33m"
CYAN = "\033[36m"
BOLD = "\033[1m"
DIM = "\033[2m"
RESET = "\033[0m"

UNRELEASED_HEADING = "## [Unreleased]"
VERSION_HEADING_RE = re.compile(r"^## \[(\d+\.\d+\.\d+)]")
UNRELEASED_LINK_RE = re.compile(r"^\[Unreleased]:\s+(.+)/compare/v.+\.\.\.HEAD$")
VERSION_LINK_RE = re.compile(r"^\[(\d+\.\d+\.\d+)]:\s+")
SEMVER_RE = re.compile(r"^\d+\.\d+\.\d+$")
RELEASE_COMMIT_RE = re.compile(r"^release: v(\d+\.\d+\.\d+)$")


def error(msg: str) -> None:
    print(f"{RED}{BOLD}error:{RESET} {msg}", file=sys.stderr)


def info(label: str, value: str) -> None:
    print(f"  {CYAN}{label}{RESET} {value}")


def success(msg: str) -> None:
    print(f"{GREEN}{BOLD}done:{RESET} {msg}")


def run_quiet(
    args: list[str], *, cwd: Path = REPO_ROOT
) -> subprocess.CompletedProcess[str]:
    """Run a subprocess, capturing all output."""
    return subprocess.run(args, capture_output=True, text=True, cwd=cwd)


def resolve_version(explicit: str | None) -> str:
    """Return the release version from the CLI arg or gitversion."""
    if explicit:
        if not SEMVER_RE.match(explicit):
            error(f"invalid version format: {explicit} (expected X.Y.Z)")
            sys.exit(1)
        return explicit

    result = run_quiet(["dotnet", "gitversion", "/showvariable", "MajorMinorPatch"])
    if result.returncode != 0:
        error("dotnet gitversion failed")
        stderr = result.stderr.strip()
        if stderr:
            print(f"  {DIM}{stderr}{RESET}", file=sys.stderr)
        sys.exit(1)

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


def unreleased_has_content(lines: list[str], start: int, end: int) -> bool:
    """Check whether there are meaningful entries between the Unreleased heading and the next."""
    for line in lines[start + 1 : end]:
        stripped = line.strip()
        if stripped and not stripped.startswith("###"):
            return True
    return False


def extract_sections(lines: list[str], start: int, end: int) -> list[str]:
    """Return the ### section names (Added, Fixed, etc.) under the Unreleased heading."""
    sections = []
    for line in lines[start + 1 : end]:
        stripped = line.strip()
        if stripped.startswith("### "):
            sections.append(stripped.removeprefix("### "))
    return sections


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


def git_commit(version: str) -> str:
    """Commit the changelog and return the abbreviated commit SHA."""
    result = run_quiet(
        [
            "git",
            "commit",
            "-m",
            f"release: v{version}",
            "--no-verify",
            "--",
            "CHANGELOG.md",
        ]
    )
    if result.returncode != 0:
        error("git commit failed")
        stderr = result.stderr.strip()
        if stderr:
            print(f"  {DIM}{stderr}{RESET}", file=sys.stderr)
        sys.exit(1)

    # Get abbreviated SHA of the new commit
    sha_result = run_quiet(["git", "rev-parse", "--short", "HEAD"])
    return sha_result.stdout.strip()


def git_tag(version: str) -> None:
    """Create an annotated tag for the release."""
    result = run_quiet(["git", "tag", "-fm", f"release v{version}", f"v{version}"])
    if result.returncode != 0:
        error("git tag failed")
        stderr = result.stderr.strip()
        if stderr:
            print(f"  {DIM}{stderr}{RESET}", file=sys.stderr)
        sys.exit(1)


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


def git_push(version: str, mainline: str) -> None:
    """Push the mainline branch and the release tag."""
    result = run_quiet(["git", "push", "origin", mainline, f"v{version}"])
    if result.returncode != 0:
        error("git push failed")
        stderr = result.stderr.strip()
        if stderr:
            print(f"  {DIM}{stderr}{RESET}", file=sys.stderr)
        sys.exit(1)


def require_clean_worktree() -> None:
    """Exit if the working copy has uncommitted changes."""
    unstaged = run_quiet(["git", "diff", "--quiet"])
    staged = run_quiet(["git", "diff", "--cached", "--quiet"])
    if unstaged.returncode != 0 or staged.returncode != 0:
        error("working copy has uncommitted changes; commit or stash them first")
        sys.exit(1)


def tag_exists_on_remote(tag: str) -> bool:
    """Check whether a tag has been pushed to origin."""
    result = run_quiet(["git", "ls-remote", "--tags", "origin", f"refs/tags/{tag}"])
    return bool(result.stdout.strip())


def undo_release() -> None:
    """Reverse a local release commit and tag."""
    # Verify HEAD is a release commit
    result = run_quiet(["git", "log", "-1", "--format=%s", "HEAD"])
    subject = result.stdout.strip()
    m = RELEASE_COMMIT_RE.match(subject)
    if not m:
        error(f"HEAD is not a release commit: {subject!r}")
        sys.exit(1)

    version = m.group(1)
    tag = f"v{version}"

    # Verify the tag exists and points to HEAD
    tag_result = run_quiet(["git", "rev-parse", "--verify", f"refs/tags/{tag}"])
    if tag_result.returncode != 0:
        error(f"tag {tag} does not exist")
        sys.exit(1)

    head_sha = run_quiet(["git", "rev-parse", "HEAD"]).stdout.strip()
    # Dereference annotated tag to get the commit it points to
    tag_sha = run_quiet(["git", "rev-parse", f"{tag}^{{commit}}"]).stdout.strip()
    if tag_sha != head_sha:
        error(f"tag {tag} does not point to HEAD")
        sys.exit(1)

    # Refuse if already pushed
    if tag_exists_on_remote(tag):
        error(f"tag {tag} has been pushed to origin; undo manually")
        sys.exit(1)

    # Delete tag, then undo the release commit without touching the working tree
    run_quiet(["git", "tag", "-d", tag])
    run_quiet(["git", "reset", "--soft", "HEAD^"])
    # Restore CHANGELOG.md to its pre-release state (updates both index and working tree)
    run_quiet(["git", "checkout", "HEAD", "--", "CHANGELOG.md"])

    success(f"undid release v{version} (tag {tag} deleted, commit removed)")


def format_version_label(version: str, prev_version: str | None) -> str:
    """Format the version line, e.g. 'v8.2.1 -> v8.3.0'."""
    if prev_version:
        return f"v{prev_version} -> v{version}"
    return f"v{version}"


def print_dry_run(
    old_lines: list[str], version: str, prev_version: str | None, sections: list[str]
) -> None:
    """Show a preview of the release that would be created."""
    unreleased_idx, next_heading_idx, _ = parse_changelog(old_lines)
    today = date.today().isoformat()

    print(f"{BOLD}Dry run: {format_version_label(version, prev_version)}{RESET}")
    print()

    # Show the release as it will appear in the changelog
    print(f"{CYAN}## [{version}] - {today}{RESET}")
    for line in old_lines[unreleased_idx + 1 : next_heading_idx]:
        print(line)

    # Show the link references that will be added/updated
    print(f"{DIM}Links:{RESET}")
    print(f"  {DIM}[Unreleased]: ...compare/v{version}...HEAD{RESET}")
    if prev_version:
        print(f"  {DIM}[{version}]: ...compare/v{prev_version}...v{version}{RESET}")
    else:
        print(f"  {DIM}[{version}]: ...releases/tag/v{version}{RESET}")

    print()
    print(f"{DIM}No files were modified.{RESET}")


def print_summary(
    version: str, prev_version: str | None, sections: list[str], sha: str
) -> None:
    """Print a release summary."""
    print()
    print(f"{BOLD}Release prepared{RESET}")
    info("version:", format_version_label(version, prev_version))
    info(" commit:", sha)
    if sections:
        info("  sections:", ", ".join(sections))
    print()


def prompt_push(version: str, mainline: str) -> None:
    """Ask the user whether to push and kick off the release."""
    print(f"{YELLOW}Push {mainline} and v{version} to origin?{RESET}")
    print(f"  {DIM}This will start the release pipeline.{RESET}")
    answer = input(f"  {YELLOW}[y/N]{RESET} ").strip().lower()
    print()

    if answer == "y":
        git_push(version, mainline)
        success(f"pushed {mainline} and v{version} to origin")
    else:
        print(f"  {DIM}Skipped. To push manually:{RESET}")
        print(f"  {DIM}  git push origin {mainline} v{version}{RESET}")
    print()


def main() -> None:
    parser = argparse.ArgumentParser(description="Prepare a Recyclarr release.")
    parser.add_argument(
        "--version", help="release version (X.Y.Z); defaults to gitversion"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="print transformed changelog without writing",
    )
    parser.add_argument(
        "--undo",
        action="store_true",
        help="reverse the most recent local release (delete tag, remove commit)",
    )
    args = parser.parse_args()

    mainline = detect_mainline()

    if args.undo:
        undo_release()
        return

    require_mainline_branch(mainline)

    version = resolve_version(args.version)

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

    sections = extract_sections(lines, unreleased_idx, next_heading_idx)

    # Transform
    new_lines = transform_changelog(lines, version, prev_version, repo_url)
    new_text = "\n".join(new_lines)

    if args.dry_run:
        print_dry_run(lines, version, prev_version, sections)
        return

    require_clean_worktree()

    # Write, commit, tag
    CHANGELOG_PATH.write_text(new_text)
    sha = git_commit(version)
    git_tag(version)

    # Summary and push prompt
    print_summary(version, prev_version, sections, sha)
    prompt_push(version, mainline)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print()
        sys.exit(130)
    except Exception as exc:
        error(str(exc))
        sys.exit(1)
