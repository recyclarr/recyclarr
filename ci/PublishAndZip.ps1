[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $runtime
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
    -p:PublishReadyToRun=true `
    -p:PublishReadyToRunShowWarnings=true

if (Get-Command chmod) {
    "The chmod command was found. Setting read + execute permission."
    & chmod +rx ./publish/$runtime/trash
}

"Execute trash command to ensure basic functionality is working"
& .\publish\$runtime\trash -h
if ($LASTEXITCODE -ne 0) {
    "Trash executable failed to run with exit code: $LASTEXITCODE"
    exit -1
}

"Zip the published files"
New-Item -ItemType Directory -Force -Path publish\zip
Compress-Archive publish\$runtime\* publish\zip\trash-$runtime.zip -Force
