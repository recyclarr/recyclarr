& $PSScriptRoot\Build-Artifacts.ps1

Push-Location "$PSScriptRoot\..\debugging"
docker compose pull
docker compose up -d
Pop-Location

docker compose build
docker compose run --rm recyclarr $args
