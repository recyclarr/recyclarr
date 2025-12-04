#!/usr/bin/env pwsh
# Query code coverage results from Test-Coverage.ps1 output.
# AI-optimized output format: path:pct:covered/total[:uncovered_lines]

param(
    [Parameter(Position = 0)]
    [ValidateSet('files', 'uncovered', 'lowest')]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$Pattern,

    [int]$First,
    [int]$Last
)

$ErrorActionPreference = 'Stop'
$ResultsDir = '/tmp/recyclarr-coverage'
$RepoRoot = (Get-Item "$PSScriptRoot/..").FullName

function Show-Help {
    Write-Output @"
Usage: Query-Coverage.ps1 <command> [args] [-First N] [-Last N]

Commands:
  files <substring>     Coverage % for files matching substring
  uncovered <substring> Same as 'files' but includes uncovered line numbers
  lowest [N]            Show N files with lowest coverage (default: 10)

Options:
  -First N              Return only first N results
  -Last N               Return only last N results

Output format: path:pct:covered/total[:uncovered_lines]
Example: src/Foo/Bar.cs:75:15/20:5,8,12-14
"@
}

function Get-CoverageData {
    $jsonFiles = Get-ChildItem -Path $ResultsDir -Filter 'coverage.json' -Recurse -ErrorAction SilentlyContinue
    if (-not $jsonFiles) {
        Write-Error "No coverage files found. Run Test-Coverage.ps1 first."
        exit 1
    }

    $merged = @{}
    foreach ($file in $jsonFiles) {
        $data = Get-Content $file.FullName | ConvertFrom-Json -AsHashtable
        foreach ($assembly in $data.Keys) {
            foreach ($filePath in $data[$assembly].Keys) {
                $relativePath = $filePath.Replace($RepoRoot + '/', '')
                if (-not $merged.ContainsKey($relativePath)) {
                    $merged[$relativePath] = @{ Lines = @{} }
                }
                foreach ($class in $data[$assembly][$filePath].Keys) {
                    foreach ($method in $data[$assembly][$filePath][$class].Keys) {
                        $lines = $data[$assembly][$filePath][$class][$method].Lines
                        foreach ($lineNum in $lines.Keys) {
                            $merged[$relativePath].Lines[$lineNum] = $lines[$lineNum]
                        }
                    }
                }
            }
        }
    }
    return $merged
}

function Format-LineRanges {
    param([int[]]$Lines)
    if (-not $Lines -or $Lines.Count -eq 0) { return '' }

    $sorted = $Lines | Sort-Object
    $ranges = @()
    $start = $sorted[0]
    $end = $sorted[0]

    for ($i = 1; $i -lt $sorted.Count; $i++) {
        if ($sorted[$i] -eq $end + 1) {
            $end = $sorted[$i]
        } else {
            $ranges += if ($start -eq $end) { "$start" } else { "$start-$end" }
            $start = $sorted[$i]
            $end = $sorted[$i]
        }
    }
    $ranges += if ($start -eq $end) { "$start" } else { "$start-$end" }
    return $ranges -join ','
}

function Get-FileCoverage {
    param($Data, [string]$Filter, [bool]$IncludeLines)

    $results = @()
    foreach ($path in $Data.Keys) {
        if ($Filter -and $path -notlike "*$Filter*") { continue }

        $lines = $Data[$path].Lines
        $total = $lines.Count
        if ($total -eq 0) { continue }

        $covered = ($lines.Values | Where-Object { $_ -gt 0 }).Count
        $pct = [math]::Floor($covered * 100 / $total)
        $uncoveredLines = @()
        if ($IncludeLines) {
            $uncoveredLines = $lines.Keys | Where-Object { $lines[$_] -eq 0 } | ForEach-Object { [int]$_ }
        }

        $results += [PSCustomObject]@{
            Path = $path
            Pct = $pct
            Covered = $covered
            Total = $total
            UncoveredLines = $uncoveredLines
        }
    }
    return $results | Sort-Object Pct
}

function Write-CoverageOutput {
    param($Results, [bool]$IncludeLines)

    foreach ($r in $Results) {
        $output = "$($r.Path):$($r.Pct):$($r.Covered)/$($r.Total)"
        if ($IncludeLines -and $r.UncoveredLines.Count -gt 0) {
            $output += ":$(Format-LineRanges $r.UncoveredLines)"
        }
        Write-Output $output
    }
}

# Main
if (-not $Command) {
    Show-Help
    exit 1
}

$coverage = Get-CoverageData

function Limit-Results {
    param($Results)
    if ($First) { $Results = $Results | Select-Object -First $First }
    if ($Last) { $Results = $Results | Select-Object -Last $Last }
    return $Results
}

switch ($Command) {
    'files' {
        if (-not $Pattern) {
            Show-Help
            exit 1
        }
        $results = Get-FileCoverage -Data $coverage -Filter $Pattern -IncludeLines $false
        Write-CoverageOutput -Results (Limit-Results $results) -IncludeLines $false
    }
    'uncovered' {
        if (-not $Pattern) {
            Show-Help
            exit 1
        }
        $results = Get-FileCoverage -Data $coverage -Filter $Pattern -IncludeLines $true
        Write-CoverageOutput -Results (Limit-Results $results) -IncludeLines $true
    }
    'lowest' {
        $n = if ($Pattern) { [int]$Pattern } else { 10 }
        $results = Get-FileCoverage -Data $coverage -Filter $null -IncludeLines $false | Select-Object -First $n
        Write-CoverageOutput -Results (Limit-Results $results) -IncludeLines $false
    }
    default {
        Show-Help
        exit 1
    }
}
