#!/usr/bin/env python3
"""Run tests with code coverage and query results.

AI-optimized output format: path:pct:covered/total[:uncovered_lines]
"""

import argparse
import json
import shutil
import subprocess
import sys
from pathlib import Path

RESULTS_DIR = Path("/tmp/recyclarr-coverage")
REPO_ROOT = Path(__file__).parent.parent.resolve()


def run_coverage() -> int:
    """Run dotnet test with code coverage."""
    if RESULTS_DIR.exists():
        shutil.rmtree(RESULTS_DIR)

    result = subprocess.run(
        [
            "dotnet",
            "test",
            "--collect:XPlat Code Coverage;Format=json",
            "--results-directory",
            str(RESULTS_DIR),
            "-v",
            "q",
        ],
        capture_output=True,
        text=True,
    )

    if result.returncode != 0:
        print(result.stdout)
        print(result.stderr, file=sys.stderr)
        return result.returncode

    return 0


def get_coverage_data() -> dict[str, dict[int, int]]:
    """Load and merge coverage from all JSON files, taking max hit count per line."""
    json_files = list(RESULTS_DIR.rglob("coverage.json"))
    if not json_files:
        print("No coverage files found. Run with --run first.", file=sys.stderr)
        sys.exit(1)

    merged: dict[str, dict[int, int]] = {}

    for json_file in json_files:
        data = json.loads(json_file.read_text())
        for assembly in data.values():
            for file_path, classes in assembly.items():
                rel_path = str(Path(file_path).relative_to(REPO_ROOT))
                if rel_path not in merged:
                    merged[rel_path] = {}

                for class_data in classes.values():
                    for method_data in class_data.values():
                        for line_num, hits in method_data.get("Lines", {}).items():
                            line = int(line_num)
                            merged[rel_path][line] = max(
                                merged[rel_path].get(line, 0), hits
                            )

    return merged


def format_line_ranges(lines: list[int]) -> str:
    """Format line numbers as compact ranges: 1,3-5,8"""
    if not lines:
        return ""

    sorted_lines = sorted(lines)
    ranges = []
    start = end = sorted_lines[0]

    for line in sorted_lines[1:]:
        if line == end + 1:
            end = line
        else:
            ranges.append(f"{start}" if start == end else f"{start}-{end}")
            start = end = line

    ranges.append(f"{start}" if start == end else f"{start}-{end}")
    return ",".join(ranges)


def get_file_coverage(
    data: dict[str, dict[int, int]], patterns: list[str] | None, include_lines: bool
) -> list[dict]:
    """Calculate coverage stats for files matching any of the patterns."""
    results = []

    for path, lines in data.items():
        if patterns and not any(p.lower() in path.lower() for p in patterns):
            continue
        if not lines:
            continue

        total = len(lines)
        covered = sum(1 for hits in lines.values() if hits > 0)
        pct = covered * 100 // total

        result = {"path": path, "pct": pct, "covered": covered, "total": total}
        if include_lines:
            result["uncovered"] = [ln for ln, hits in lines.items() if hits == 0]

        results.append(result)

    return sorted(results, key=lambda x: x["pct"])


def print_results(results: list[dict], include_lines: bool) -> None:
    """Output results in AI-friendly format."""
    for r in results:
        output = f"{r['path']}:{r['pct']}:{r['covered']}/{r['total']}"
        if include_lines and r.get("uncovered"):
            output += f":{format_line_ranges(r['uncovered'])}"
        print(output)


def main():
    parser = argparse.ArgumentParser(
        description="Run tests with coverage and query results"
    )
    parser.add_argument(
        "-r", "--run", action="store_true", help="Run tests before querying"
    )
    subparsers = parser.add_subparsers(dest="command")

    files_parser = subparsers.add_parser("files", help="Coverage %% for matching files")
    files_parser.add_argument(
        "patterns", nargs="+", help="Substrings to match (case-insensitive, not globs)"
    )
    files_parser.add_argument("-f", "--first", type=int, help="Return first N results")
    files_parser.add_argument("-l", "--last", type=int, help="Return last N results")

    uncovered_parser = subparsers.add_parser(
        "uncovered", help="Same as files + line numbers"
    )
    uncovered_parser.add_argument(
        "patterns", nargs="+", help="Substrings to match (case-insensitive, not globs)"
    )
    uncovered_parser.add_argument(
        "-f", "--first", type=int, help="Return first N results"
    )
    uncovered_parser.add_argument(
        "-l", "--last", type=int, help="Return last N results"
    )

    lowest_parser = subparsers.add_parser("lowest", help="N files with lowest coverage")
    lowest_parser.add_argument(
        "n", nargs="?", type=int, default=10, help="Number of files (default: 10)"
    )

    args = parser.parse_args()

    if args.run:
        returncode = run_coverage()
        if returncode != 0:
            sys.exit(returncode)

    if not args.command:
        if not args.run:
            parser.print_help()
            sys.exit(1)
        sys.exit(0)

    coverage = get_coverage_data()

    if args.command == "files":
        results = get_file_coverage(coverage, args.patterns, include_lines=False)
        if args.first:
            results = results[: args.first]
        if args.last:
            results = results[-args.last :]
        print_results(results, include_lines=False)

    elif args.command == "uncovered":
        results = get_file_coverage(coverage, args.patterns, include_lines=True)
        if args.first:
            results = results[: args.first]
        if args.last:
            results = results[-args.last :]
        print_results(results, include_lines=True)

    elif args.command == "lowest":
        results = get_file_coverage(coverage, None, include_lines=False)[: args.n]
        print_results(results, include_lines=False)


if __name__ == "__main__":
    main()
