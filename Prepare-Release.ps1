[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string]
    $version
)

$ErrorActionPreference = "Stop"

# Requires: Install-Module -Name ChangelogManagement
Update-Changelog -ReleaseVersion $version -LinkMode Automatic -LinkPattern @{
    FirstRelease = "https://github.com/rcdailey/trash-updater/releases/tag/v{CUR}"
    NormalRelease = "https://github.com/rcdailey/trash-updater/compare/v{PREV}...v{CUR}"
    Unreleased = "https://github.com/rcdailey/trash-updater/compare/v{CUR}...HEAD"
}

# Read & Write the file after updating the changelog to force a newline at the end of the file. The
# Update-Changelog method removes the newline at the end.
$content = Get-Content -Path .\CHANGELOG.md
Set-Content -Path .\CHANGELOG.md -Value $content

# Requires: dotnet tool install -g nbgv
nbgv set-version $version

git commit -m "release: v$version" -- CHANGELOG.md version.json
git tag -m "release v$version" "v$version"
