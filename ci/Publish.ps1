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

# Both executable projects are published to the same output directory so they
# ship together in a single archive. Shared library builds are reused via
# MSBuild's incremental compilation between the two publish calls.
$projects = @(
    "src\Recyclarr.Cli"
    "src\Recyclarr.Server"
)

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
