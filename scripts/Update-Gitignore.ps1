#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

$gitIgnorePath = "$PSScriptRoot/../.gitignore"

Function gig {
  param(
    [Parameter(Mandatory=$true)]
    [string[]]$list
  )
  $params = ($list | ForEach-Object { [uri]::EscapeDataString($_) }) -join ","
  Invoke-WebRequest -Uri "https://www.toptal.com/developers/gitignore/api/$params" | `
    Select-Object -ExpandProperty content | `
    Set-Content -Path $gitIgnorePath -Encoding utf8 -NoNewline
}

gig archives,windows,macos,csharp,rider

# Replace specific ignore patterns
$(Get-Content $gitIgnorePath) `
    -replace '^\.idea', '**/.idea' `
| Set-Content -Path $gitIgnorePath
