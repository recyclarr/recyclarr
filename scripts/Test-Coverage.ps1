#!/usr/bin/env pwsh
# Runs dotnet test with code coverage and outputs only coverage file paths.
# Usage: ./scripts/Test-Coverage.ps1

$ErrorActionPreference = 'Stop'
$ResultsDir = '/tmp/recyclarr-coverage'

# Clean previous results
if (Test-Path $ResultsDir) {
    Remove-Item -Recurse -Force $ResultsDir
}

# Run tests with coverage (suppress all output)
$output = & dotnet test `
    --collect:"XPlat Code Coverage;Format=json" `
    --results-directory $ResultsDir `
    -v q 2>&1

if ($LASTEXITCODE -ne 0) {
    $output | Write-Host
    exit $LASTEXITCODE
}

# Output only coverage file paths
$output | Where-Object { $_ -match '\.json$' } | ForEach-Object { $_.Trim() }
