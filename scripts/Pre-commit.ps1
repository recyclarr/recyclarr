#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Utility script for pre-commit operations in Recyclarr
.DESCRIPTION
    Provides common pre-commit related tasks for developers
.PARAMETER Task
    The task to execute: check, fix, install, uninstall, update
.EXAMPLE
    ./scripts/Pre-commit.ps1 check
    ./scripts/Pre-commit.ps1 fix
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('check', 'fix', 'install', 'uninstall', 'update', 'help')]
    [string]$Task
)

$ErrorActionPreference = "Stop"

function Write-Usage {
    Write-Host "Pre-commit utility for Recyclarr" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage: ./scripts/Pre-commit.ps1 <task>" -ForegroundColor Blue
    Write-Host ""
    Write-Host "Available tasks:" -ForegroundColor Yellow
    Write-Host "  check     - Run all pre-commit hooks on all files" -ForegroundColor White
    Write-Host "  fix       - Run auto-fixable hooks (formatting, cleanup)" -ForegroundColor White
    Write-Host "  install   - Install pre-commit hooks" -ForegroundColor White
    Write-Host "  uninstall - Remove pre-commit hooks" -ForegroundColor White
    Write-Host "  update    - Update pre-commit hook versions" -ForegroundColor White
    Write-Host "  help      - Show this help message" -ForegroundColor White
}

switch ($Task) {
    'check' {
        Write-Host "Running all pre-commit checks..." -ForegroundColor Blue
        pre-commit run --all-files
    }

    'fix' {
        Write-Host "Running auto-fixable hooks..." -ForegroundColor Blue
        # Run only the hooks that can automatically fix issues
        pre-commit run --all-files csharpier
        pre-commit run --all-files resharper-cleanup
    }

    'install' {
        Write-Host "Installing pre-commit hooks..." -ForegroundColor Blue
        & "$PSScriptRoot/Install-PreCommit.ps1"
    }

    'uninstall' {
        Write-Host "Uninstalling pre-commit hooks..." -ForegroundColor Blue
        pre-commit uninstall
        pre-commit uninstall --hook-type commit-msg
        Write-Host "✓ Pre-commit hooks removed" -ForegroundColor Green
    }

    'update' {
        Write-Host "Updating pre-commit hooks..." -ForegroundColor Blue
        pre-commit autoupdate
        Write-Host "✓ Pre-commit hooks updated" -ForegroundColor Green
        Write-Host "Consider running 'pre-commit run --all-files' to test updated hooks" -ForegroundColor Yellow
    }

    'help' {
        Write-Usage
    }
}
