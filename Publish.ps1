[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $runtime
)

dotnet publish Trash `
    --output publish `
    --runtime $runtime `
    --configuration Release `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true `
    -p:IncludeNativeLibrariesForSelfExtract=true
