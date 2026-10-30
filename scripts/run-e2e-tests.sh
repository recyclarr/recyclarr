#!/usr/bin/env bash
# Runs E2E tests and prints the log file path for analysis.
log_file=/tmp/e2e-tests.log

dotnet test Recyclarr.slnx \
    --filter "Category=E2E" \
    --logger "console;verbosity=normal" \
    -v q >"$log_file" 2>&1
exit_code=$?

if ((exit_code == 0)); then
    echo "E2E tests PASSED"
else
    echo "E2E tests FAILED"
fi

echo "Log file: $log_file"
echo "The file is LARGE. Search it with rg; do not read the whole file!"
exit "$exit_code"
