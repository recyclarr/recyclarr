#!/usr/bin/env pwsh
# Runs E2E tests and outputs the log file path for analysis.
# Usage: ./scripts/Run-E2ETests.ps1

$logFile = "/tmp/e2e-tests.log"

dotnet test Recyclarr.slnx `
    --filter "Category=E2E" `
    --logger "console;verbosity=normal" `
    -v q 2>&1 > $logFile

Write-Output $logFile
