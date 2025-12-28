#!/usr/bin/env python3
"""Query GitHub code scanning alerts. AI-optimized output: path:line:severity:rule:message"""

import argparse
import json
import re
import subprocess
import sys

# Severity levels from lowest to highest
SEVERITY_LEVELS = ["note", "warning", "error"]


def get_current_branch() -> str:
    """Get the current git branch name."""
    result = subprocess.run(
        ["git", "branch", "--show-current"],
        capture_output=True,
        text=True,
        check=True,
    )
    return result.stdout.strip()


def get_repo_info() -> tuple[str, str]:
    """Get owner and repo from git remote."""
    result = subprocess.run(
        ["gh", "repo", "view", "--json", "owner,name"],
        capture_output=True,
        text=True,
        check=True,
    )
    data = json.loads(result.stdout)
    return data["owner"]["login"], data["name"]


def fetch_alerts(owner: str, repo: str, ref: str, state: str = "open") -> list[dict]:
    """Fetch all code scanning alerts for a branch."""
    result = subprocess.run(
        [
            "gh",
            "api",
            f"repos/{owner}/{repo}/code-scanning/alerts",
            "-X",
            "GET",
            "-f",
            f"ref=refs/heads/{ref}",
            "-f",
            f"state={state}",
            "-f",
            "per_page=100",
            "--paginate",
        ],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        print(f"Error fetching alerts: {result.stderr}", file=sys.stderr)
        sys.exit(1)

    # gh --paginate outputs multiple JSON arrays, need to merge them
    alerts = []
    for line in result.stdout.strip().split("\n"):
        if line:
            alerts.extend(json.loads(line))
    return alerts


def filter_alerts(
    alerts: list[dict],
    path_pattern: str | None = None,
    rule_pattern: str | None = None,
    min_severity: str = "warning",
) -> list[dict]:
    """Filter alerts by path, rule, or minimum severity threshold."""
    filtered = alerts

    if path_pattern:
        regex = re.compile(path_pattern, re.IGNORECASE)
        filtered = [
            a
            for a in filtered
            if regex.search(a["most_recent_instance"]["location"]["path"])
        ]

    if rule_pattern:
        regex = re.compile(rule_pattern, re.IGNORECASE)
        filtered = [a for a in filtered if regex.search(a["rule"]["id"])]

    # Filter by minimum severity threshold
    min_index = SEVERITY_LEVELS.index(min_severity)
    filtered = [
        a
        for a in filtered
        if SEVERITY_LEVELS.index(a["rule"]["severity"]) >= min_index
    ]

    return filtered


def format_alert(alert: dict) -> str:
    """Format a single alert as path:line:severity:rule:message"""
    loc = alert["most_recent_instance"]["location"]
    path = loc["path"]
    line = loc["start_line"]
    severity = alert["rule"]["severity"]
    rule = alert["rule"]["id"]
    message = alert["most_recent_instance"]["message"]["text"]
    # Remove newlines from message to keep one-line format
    message = message.replace("\n", " ").replace("\r", "")
    return f"{path}:{line}:{severity}:{rule}:{message}"


def main():
    parser = argparse.ArgumentParser(
        description="Query GitHub code scanning alerts for current branch"
    )
    parser.add_argument(
        "-p",
        "--path",
        help="Filter by path pattern (regex)",
    )
    parser.add_argument(
        "-r",
        "--rule",
        help="Filter by rule ID pattern (regex)",
    )
    parser.add_argument(
        "-s",
        "--severity",
        choices=["note", "warning", "error"],
        default="warning",
        help="Minimum severity threshold (default: warning)",
    )
    parser.add_argument(
        "-b",
        "--branch",
        help="Branch to query (default: current branch)",
    )
    parser.add_argument(
        "--state",
        default="open",
        choices=["open", "dismissed", "fixed"],
        help="Alert state (default: open)",
    )
    parser.add_argument(
        "--no-summary",
        action="store_true",
        help="Skip summary header",
    )

    args = parser.parse_args()

    branch = args.branch or get_current_branch()
    owner, repo = get_repo_info()

    alerts = fetch_alerts(owner, repo, branch, args.state)
    filtered = filter_alerts(alerts, args.path, args.rule, min_severity=args.severity)

    # Print summary header
    if not args.no_summary:
        severity_counts = {}
        for a in filtered:
            sev = a["rule"]["severity"]
            severity_counts[sev] = severity_counts.get(sev, 0) + 1
        counts_str = ", ".join(f"{v} {k}" for k, v in sorted(severity_counts.items()))
        print(f"# {len(filtered)} alerts ({counts_str}) on {branch}")

    # Print alerts
    for alert in filtered:
        print(format_alert(alert))


if __name__ == "__main__":
    main()
