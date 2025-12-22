#!/usr/bin/env python3
"""Run dotnet test with code coverage and output coverage file paths."""

import shutil
import subprocess
import sys
from pathlib import Path

RESULTS_DIR = Path("/tmp/recyclarr-coverage")

# Clean previous results
if RESULTS_DIR.exists():
    shutil.rmtree(RESULTS_DIR)

# Run tests with coverage
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
    sys.exit(result.returncode)

# Output coverage file paths
for line in result.stdout.splitlines():
    if line.strip().endswith(".json"):
        print(line.strip())
