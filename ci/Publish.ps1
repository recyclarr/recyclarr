[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $runtime
)

$ErrorActionPreference = "Stop"

dotnet publish src\Recyclarr `
    --output publish\$runtime `
    --configuration Release `
    --runtime $runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishReadyToRunComposite=true `
    -p:PublishReadyToRunShowWarnings=true `
    -p:EnableCompressionInSingleFile=true
