[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $RootPath
)

$ErrorActionPreference = "Stop"

$ZipPath = "$RootPath-zip"
"Zip the published files to: $ZipPath"
New-Item -ItemType Directory -Force -Path $ZipPath
$dirs = Get-ChildItem -Path $RootPath -Directory -Name
foreach ($dir in $dirs) {
    "> Zipping: $RootPath\$dir"
    Compress-Archive $RootPath\$dir\* $ZipPath\$dir.zip -Force
}
