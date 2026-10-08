[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $Runtime,
    [string] $OutputDir,
    [string] $Configuration = "Release",
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
        "-p:IncludeNativeLibrariesForSelfExtract=true"
    )
}
else {
    $extraArgs += @(
        "--self-contained=false"
    )
}

# Workaround for dotnet/runtime#112167: EnableCompressionInSingleFile causes intermittent
# AccessViolationException on macOS ARM64. Disable until the fix is backported to .NET 10.
if (-not $NoCompress -and $Runtime -ne "osx-arm64") {
    $extraArgs += @(
        "-p:EnableCompressionInSingleFile=true"
    )
}

if (-not $OutputDir) {
    $OutputDir = "publish\$Runtime"
}

"Extra Args: $extraArgs"

# Every executable shipped in a release. All publish into the same directory, which downstream steps
# (smoke test, signing, archiving) treat as the release unit for this runtime.
$projects = @(
    "src\Recyclarr.Cli"
    "src\Recyclarr.Server"
)

foreach ($project in $projects) {
    "> Publishing: $project"
    dotnet publish $project `
        --output $OutputDir `
        --configuration $Configuration `
        --runtime $Runtime `
        @extraArgs

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $project"
    }
}
