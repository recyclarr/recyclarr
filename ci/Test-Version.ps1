[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$Version
)

if ($Version -match 'v\d+\.\d+\.\d+') {
    '::set-output name=match::true'
}
else {
    '::set-output name=match::false'
}
