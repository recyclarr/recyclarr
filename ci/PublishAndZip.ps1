[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $runtime
)

$ErrorActionPreference = "Stop"

& "$PSScriptRoot\Publish.ps1" $runtime

if (Get-Command chmod -errorAction SilentlyContinue) {
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
