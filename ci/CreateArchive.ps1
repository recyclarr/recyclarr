[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $PublishDir,
    [Parameter(Mandatory = $true)]
    [string] $OutputDir,
    [string] $ArchiveDirName
)

$ErrorActionPreference = "Stop"

$archiveTargets = @()
if ($ArchiveDirName) {
    $archiveTargets += "$ArchiveDirName"
}
else {
    $archiveTargets += Get-ChildItem -Path $PublishDir -Directory -Name
}

New-Item -ItemType Directory -Force -Path $OutputDir
$OutputDir = Resolve-Path $OutputDir

foreach ($dir in $archiveTargets) {
    $archiveName = "recyclarr-$dir"
    if ($dir.StartsWith("win-")) {
        "> Zipping: $dir"
        Compress-Archive "$PublishDir/$dir/*" "$OutputDir/$archiveName.zip" -Force
    }
    else {
        "> Tarballing: $dir"
        Push-Location "$PublishDir/$dir"
        tar -cJv --owner=0 --group=0 -f "$archiveName.tar.xz" *
        Move-Item "$archiveName.tar.xz" $OutputDir
        Pop-Location
    }
}
