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
    --self-contained true
