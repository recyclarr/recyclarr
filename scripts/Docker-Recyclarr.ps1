#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

$recyclarrYaml = "$PSScriptRoot/../compose.yaml"
$configDir = $env:RECYCLARR_CONFIG_DIR
if (-not $configDir) {
    throw "RECYCLARR_CONFIG_DIR is not set (set it with mise)"
}
if (-not $env:RECYCLARR_API_KEY) {
    throw "RECYCLARR_API_KEY is not set"
}

# The image packages prebuilt binaries for the Docker host's architecture.
$arch = switch (docker info --format '{{.Architecture}}') {
    { $_ -in "x86_64", "amd64" } { "x64" }
    { $_ -in "aarch64", "arm64" } { "arm64" }
    default { throw "Unsupported Docker architecture: $_" }
}
& "$PSScriptRoot/../ci/Publish.ps1" -Runtime "linux-musl-$arch" -NoSingleFile `
    -OutputDir "$PSScriptRoot/../publish/linux-musl-$arch"

$env:DOCKER_UID = $(id -u)
$env:DOCKER_GID = $(id -g)

# Ensure the bind mount directory exists with correct ownership before the container starts.
# If Docker creates it first, it ends up owned by root and the container (running as
# the current user) can't write to it.
if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir -Force | Out-Null
}

# BSD stat (macOS) and GNU stat (Linux) take different format flags.
$stat = if ($IsMacOS) { stat -f '%u:%g' $configDir } else { stat -c '%u:%g' $configDir }
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
