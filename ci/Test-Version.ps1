[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$Version
)

if ($Version -match 'v\d+\.\d+\.\d+') {
    "match=true" >> $env:GITHUB_OUTPUT
}
else {
    "match=false" >> $env:GITHUB_OUTPUT
}
