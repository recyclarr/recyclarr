[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $runtime,
    [Parameter()]
    [switch] $noSingleFile
)

$ErrorActionPreference = "Stop"

if (-not $noSingleFile) {
    $singleFileArgs = @(
        "--self-contained=true"
        "-p:PublishSingleFile=true"
        "-p:IncludeNativeLibrariesForSelfExtract=true"
        "-p:PublishReadyToRunComposite=true"
        "-p:PublishReadyToRunShowWarnings=true"
        "-p:EnableCompressionInSingleFile=true"
    )
}

dotnet publish src\Recyclarr `
    --output publish\$runtime `
    --configuration Release `
    --runtime $runtime `
    $singleFileArgs
