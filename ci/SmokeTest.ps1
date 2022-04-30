[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $PathToExe
)

$ErrorActionPreference = "Stop"

if (Get-Command chmod -errorAction SilentlyContinue) {
    "The chmod command was found. Setting read + execute permission."
    & chmod +rx $PathToExe
}

"Execute recyclarr command to ensure basic functionality is working"
& $PathToExe -h
if ($LASTEXITCODE -ne 0) {
    "Recyclarr executable failed to run with exit code: $LASTEXITCODE"
    exit -1
}
