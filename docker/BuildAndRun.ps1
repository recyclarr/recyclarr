[CmdletBinding()]
param (
    [string] $Runtime = "linux-musl-x64",
    [string[]] $RunArgs
)

$artifactsDir = "$PSScriptRoot\artifacts"

# Delete old build artifacts
Remove-Item $artifactsDir -Recurse -Force -ErrorAction SilentlyContinue

# Publish new build artifacts
Push-Location $PSScriptRoot\..
try {
    & ci\Publish.ps1 -NoSingleFile `
        -OutputDir "$artifactsDir\recyclarr-$Runtime" `
        -Runtime $Runtime
}
finally {
    Pop-Location
}

# Start the corresponding radarr/sonarr docker containers for testing/debugging
Push-Location "$PSScriptRoot\..\debugging"
try {
    docker compose up -d --pull always
}
finally {
    Pop-Location
}

# Finally, build the docker image and run it
docker compose build
# TODO: Use `--build` when it releases:
# https://github.com/docker/compose/issues/10003
docker compose run --rm recyclarr @RunArgs
