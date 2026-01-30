#!/usr/bin/env pwsh
# Runs E2E tests and outputs the log file path for analysis.
# Usage: ./scripts/Run-E2ETests.ps1

$logFile = "/tmp/e2e-tests.log"

# E2E tests are excluded from discovery by default; enable via MSBuild property
dotnet test --project tests/Recyclarr.EndToEndTests `
    -p:IsTestingPlatformApplication=true `
    -- --disable-logo `
    2>&1 > $logFile

$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "E2E tests PASSED" -ForegroundColor Green
} else {
    Write-Host "E2E tests FAILED" -ForegroundColor Red
}

Write-Output "Log file: $logFile"
Write-Output "The file is LARGE. Search it with rg; do not read the whole file!"
exit $exitCode
