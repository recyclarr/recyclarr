#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

$debuggingYaml = "$PSScriptRoot/../docker-compose.yml"

# Start the corresponding radarr/sonarr docker containers for testing/debugging
docker compose -f $debuggingYaml up -d --pull always
if ($LASTEXITCODE -ne 0) {
    throw "docker compose up failed (debug)"
}

Write-Host "ntfy: http://localhost:8090/recyclarr"
