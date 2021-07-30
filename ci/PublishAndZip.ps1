[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string]
    $runtime
)

$ErrorActionPreference = "Stop"

# Note for `IncludeSymbolsInSingleFile`:
#
# This is only required because LibGit2Sharp bundles PDB files in its nuget package.
# See the following github issues for more info:
#
# - https://github.com/dotnet/runtime/issues/3807
# - https://github.com/libgit2/libgit2sharp.nativebinaries/issues/111

dotnet publish src\Trash `
    --output publish\$runtime `
    --configuration Release `
    --runtime $runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:IncludeSymbolsInSingleFile=true `
    -p:PublishReadyToRun=true

New-Item -ItemType Directory -Force -Path publish\zip
Compress-Archive publish\$runtime\trash* publish\zip\trash-$runtime.zip -Force
