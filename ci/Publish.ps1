[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $Runtime,
    [string] $OutputDir,
    [string] $Configuration = "Release",
    [string] $BuildPath = "src\Recyclarr.Cli",
    [switch] $NoSingleFile,
    [switch] $NoCompress,
    [switch] $ReadyToRun
)

$ErrorActionPreference = "Stop"

$extraArgs = @()

if ($ReadyToRun) {
    $extraArgs += @(
        "-p:PublishReadyToRunShowWarnings=true"
        "-p:PublishReadyToRunComposite=true"
        "-p:TieredCompilation=false"
    )
}

if (-not $NoSingleFile) {
    $extraArgs += @(
        "--self-contained=true"
        "-p:PublishSingleFile=true"
    )
}
else {
    $extraArgs += @(
        "--self-contained=false"
    )
}

if (-not $NoCompress) {
    $extraArgs += @(
        "-p:EnableCompressionInSingleFile=true"
    )
}

if (-not $OutputDir) {
    $OutputDir = "publish\$Runtime"
}

"Extra Args: $extraArgs"

dotnet publish $BuildPath `
    --output $OutputDir `
    --configuration $Configuration `
    --runtime $Runtime `
    @extraArgs

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed"
}
