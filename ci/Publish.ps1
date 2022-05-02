[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $runtime
)

$ErrorActionPreference = "Stop"

dotnet publish src\Recyclarr `
    --output publish\$runtime `
    --configuration Release `
    --runtime $runtime `
    --self-contained true
