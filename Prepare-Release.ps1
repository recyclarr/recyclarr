[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string]
    $version
)

Update-Changelog -ReleaseVersion $version -LinkMode Automatic -LinkPattern @{
    FirstRelease = "https://github.com/rcdailey/trash-updater/releases/tag/v{CUR}"
    NormalRelease = "https://github.com/rcdailey/trash-updater/compare/v{PREV}...v{CUR}"
    Unreleased = "https://github.com/rcdailey/trash-updater/compare/v{CUR}...HEAD"
}

nbgv set-version $version
git commit -m "release: v$version" -- CHANGELOG.md version.json
git tag -m "release v$version" "v$version"
