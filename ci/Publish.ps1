[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $Runtime,

    [string] $OutputDir,

    [string] $Configuration = "Release",

    [string] $BuildPath = "src\Recyclarr",

    [switch] $NoSingleFile
)

$ErrorActionPreference = "Stop"

if (-not $NoSingleFile) {
    $selfContained = "true"
    $singleFileArgs = @(
        "-p:PublishSingleFile=true"
        "-p:IncludeNativeLibrariesForSelfExtract=true"
        "-p:PublishReadyToRunComposite=true"
        "-p:PublishReadyToRunShowWarnings=true"
        "-p:EnableCompressionInSingleFile=true"
    )
}
else {
    $selfContained = "false"
}

if (-not $OutputDir) {
    $OutputDir = "publish\$Runtime"
}

dotnet publish $BuildPath `
    --output $OutputDir `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained $selfContained `
    $singleFileArgs

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed"
}
