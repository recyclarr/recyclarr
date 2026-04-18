#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

$recyclarrYaml = "$PSScriptRoot/../docker-compose.yml"
$configDir = $env:RECYCLARR_CONFIG_DIR
if (-not $configDir) {
    throw "RECYCLARR_CONFIG_DIR is not set (source: mise.toml)"
}
$env:DOCKER_UID = $(id -u)
$env:DOCKER_GID = $(id -g)

# Ensure the bind mount directory exists with correct ownership before the container starts.
# If Docker creates it first, it ends up owned by root and the container (running as
# the current user) can't write to it.
if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir -Force | Out-Null
}

$stat = stat -c '%u:%g' $configDir
if ($stat -ne "$env:DOCKER_UID`:$env:DOCKER_GID") {
    Write-Host "Fixing $configDir ownership ($stat -> $env:DOCKER_UID`:$env:DOCKER_GID)"
    sudo chown -R "$env:DOCKER_UID`:$env:DOCKER_GID" $configDir
}

# Start the corresponding radarr/sonarr docker containers for testing/debugging
docker compose -f $recyclarrYaml up -d
if ($LASTEXITCODE -ne 0) {
    throw "docker compose up failed (debug)"
}

Write-Host "ntfy: http://localhost:8090/recyclarr"

docker compose -f $recyclarrYaml --profile recyclarr run --rm --build recyclarr @args
if ($LASTEXITCODE -ne 0) {
    throw "docker compose run failed (recyclarr)"
}
