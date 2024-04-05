[CmdletBinding()]
param (
    [string] $Runtime = "linux-musl-x64",
    [string[]] $RunArgs
)

$ErrorActionPreference = "Stop"

$artifactsDir = "$PSScriptRoot\docker\artifacts"

# Delete old build artifacts
Remove-Item $artifactsDir -Recurse -Force -ErrorAction SilentlyContinue

# Publish new build artifacts
& .\ci\Publish.ps1 -NoSingleFile `
    -OutputDir "$artifactsDir\$Runtime" `
    -Runtime $Runtime

# Start the corresponding radarr/sonarr docker containers for testing/debugging
Push-Location "$PSScriptRoot\docker\debugging"
try {
    docker compose up -d --pull always
    if ($LASTEXITCODE -ne 0) {
        throw "failed to bring up services stack"
    }
}
finally {
    Pop-Location
}

docker compose run --rm --build app @RunArgs
if ($LASTEXITCODE -ne 0) {
    throw "docker compose run failed"
}
