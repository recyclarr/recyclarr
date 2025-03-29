$ErrorActionPreference = "Stop"

$recyclarrYaml = "$PSScriptRoot/../docker-compose.yml"

# Start the corresponding radarr/sonarr docker containers for testing/debugging
& $PSScriptRoot/Docker-Debug.ps1

docker compose -f $recyclarrYaml --profile recyclarr run --rm --build recyclarr @args
if ($LASTEXITCODE -ne 0) {
    throw "docker compose run failed (recyclarr)"
}
