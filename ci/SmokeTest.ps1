[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $PathToTrashExe
)

$ErrorActionPreference = "Stop"

if (Get-Command chmod -errorAction SilentlyContinue) {
    "The chmod command was found. Setting read + execute permission."
    & chmod +rx $PathToTrashExe
}

"Execute trash command to ensure basic functionality is working"
& $PathToTrashExe -h
if ($LASTEXITCODE -ne 0) {
    "Trash executable failed to run with exit code: $LASTEXITCODE"
    exit -1
}
