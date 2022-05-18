[CmdletBinding()]
param ()

$ErrorActionPreference = "Stop"

dotnet tool update --global GitVersion.Tool
$version = dotnet-gitversion /showvariable SemVer

Install-Module -Name ChangelogManagement
Update-Changelog -ReleaseVersion $version -LinkMode Automatic -LinkPattern @{
    FirstRelease = "https://github.com/recyclarr/recyclarr/releases/tag/v{CUR}"
    NormalRelease = "https://github.com/recyclarr/recyclarr/compare/v{PREV}...v{CUR}"
    Unreleased = "https://github.com/recyclarr/recyclarr/compare/v{CUR}...HEAD"
}

# Read & Write the file after updating the changelog to force a newline at the end of the file. The
# Update-Changelog method removes the newline at the end.
$content = Get-Content -Path .\CHANGELOG.md
Set-Content -Path .\CHANGELOG.md -Value $content

git commit -m "release: v$version" -- CHANGELOG.md
git tag -m "release v$version" "v$version"
