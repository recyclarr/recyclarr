#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Installs pre-commit hooks for the Recyclarr repository
.DESCRIPTION
    This script installs pre-commit hooks and ensures all required tools are available.
    It integrates with the existing development workflow.
.EXAMPLE
    ./scripts/Install-PreCommit.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# Helper function to run commands and check exit codes
function Invoke-CommandWithCheck {
    param(
        [string]$Command,
        [string]$SuccessMessage,
        [string]$ErrorMessage,
        [switch]$Optional,
        [switch]$SuppressOutput
    )

    if ($SuppressOutput) {
        $null = Invoke-Expression $Command
    } else {
        $result = Invoke-Expression $Command
    }

    if ($LASTEXITCODE -ne 0) {
        if ($Optional) {
            Write-Warning $ErrorMessage
            return $false
        } else {
            Write-Error $ErrorMessage
            exit 1
        }
    }

    if ($SuccessMessage) {
        if ($SuppressOutput) {
            Write-Host "✓ $SuccessMessage" -ForegroundColor Green
        } else {
            Write-Host "✓ $SuccessMessage" -ForegroundColor Green
            if ($result) {
                Write-Host "  $result" -ForegroundColor Gray
            }
        }
    }
    return $true
}

# Helper function to check if a tool is available and install if needed
function Test-AndInstallTool {
    param(
        [string]$CheckCommand,
        [string]$InstallCommand,
        [string]$ToolName,
        [switch]$Optional
    )

    $success = Invoke-CommandWithCheck `
        -Command $CheckCommand `
        -SuccessMessage "$ToolName is available" `
        -ErrorMessage "$ToolName not found" `
        -Optional:$Optional `
        -SuppressOutput

    if (-not $success -and $InstallCommand) {
        Write-Warning "$ToolName not found. Installing..."
        Invoke-CommandWithCheck `
            -Command $InstallCommand `
            -ErrorMessage "Failed to install $ToolName" `
            -Optional:$Optional `
            -SuppressOutput
    }
}

Write-Host "Setting up pre-commit hooks for Recyclarr..." -ForegroundColor Green

# Check if pre-commit is installed
$preCommitVersion = pre-commit --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error `
        "Pre-commit is not installed. Please install it with: pip install pre-commit"
    exit 1
}
Write-Host "✓ Pre-commit is installed: $preCommitVersion" -ForegroundColor Green

# Check if .NET tools are installed and restore if needed
$null = Test-AndInstallTool `
    -CheckCommand "dotnet tool list" `
    -InstallCommand "dotnet tool restore" `
    -ToolName ".NET tools"

# Check if CSharpier is available and install if needed
$null = Test-AndInstallTool `
    -CheckCommand "dotnet csharpier --version" `
    -InstallCommand "dotnet tool install csharpier" `
    -ToolName "CSharpier"

# Install pre-commit hooks
Write-Host "Installing pre-commit hooks..." -ForegroundColor Blue
$null = Invoke-CommandWithCheck `
    -Command "pre-commit install" `
    -SuccessMessage "Pre-commit hooks installed successfully" `
    -ErrorMessage "Failed to install pre-commit hooks" `
    -SuppressOutput

# Install commit-msg hook for conventional commits (optional)
Write-Host "Installing commit-msg hook for conventional commits..." -ForegroundColor Blue
$success = Invoke-CommandWithCheck `
    -Command "pre-commit install --hook-type commit-msg" `
    -SuccessMessage "Commit-msg hook installed" `
    -ErrorMessage "Failed to install commit-msg hook (this is optional)" `
    -Optional `
    -SuppressOutput

# Run a test to ensure everything works
Write-Host "Testing pre-commit setup..." -ForegroundColor Blue
$testSuccess = Invoke-CommandWithCheck `
    -Command "pre-commit run --all-files --show-diff-on-failure" `
    -SuccessMessage "Pre-commit setup test completed" `
    -ErrorMessage "Pre-commit test had issues. This may be normal for first-time setup." `
    -Optional `
    -SuppressOutput

if (-not $testSuccess) {
    Write-Host "You can run 'pre-commit run --all-files' later to fix any issues." `
        -ForegroundColor Yellow
}

Write-Host "`n🎉 Pre-commit setup complete!" -ForegroundColor Green
Write-Host "The following hooks are now active:" -ForegroundColor Cyan
Write-Host "  • Code formatting (CSharpier)" -ForegroundColor White
Write-Host "  • Code cleanup (ReSharper)" -ForegroundColor White
Write-Host "  • Markdown linting" -ForegroundColor White
Write-Host "  • Basic file checks (trailing whitespace, etc.)" -ForegroundColor White
Write-Host "  • .NET build verification" -ForegroundColor White
Write-Host "`nHooks will run automatically on git commit." -ForegroundColor Blue
Write-Host "To run manually: pre-commit run --all-files" -ForegroundColor Blue
