$ErrorActionPreference = "Stop"

$recyclarrYaml = "$PSScriptRoot/../docker-compose.yml"

# Start the corresponding radarr/sonarr docker containers for testing/debugging
& $PSScriptRoot/Debug.ps1

docker compose -f $recyclarrYaml run --rm --build app @args
if ($LASTEXITCODE -ne 0) {
    throw "docker compose run failed"
}
