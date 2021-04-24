[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string]
    $runtime
)

$ErrorActionPreference = "Stop"

dotnet publish src\Trash `
    --output publish\$runtime `
    --configuration Release `
    --runtime $runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishReadyToRun=true

New-Item -ItemType Directory -Force -Path publish\zip
Compress-Archive publish\$runtime\trash* publish\zip\trash-$runtime.zip -Force
