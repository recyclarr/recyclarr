[CmdletBinding()]
param (
    $Runtime = "linux-musl-x64"
)

$artifactsDir = "$PSScriptRoot\artifacts"

Remove-Item $artifactsDir -Recurse -Force -ErrorAction SilentlyContinue

Push-Location $PSScriptRoot\..
& ci\Publish.ps1 -NoSingleFile `
    -OutputDir "$artifactsDir\recyclarr-$Runtime" `
    -Runtime $Runtime
Pop-Location
