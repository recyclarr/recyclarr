[CmdletBinding()]
param (
    $runtime = "linux-musl-x64"
)

$artifactDir="$PSScriptRoot\artifacts"

Remove-Item $artifactDir -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish "$PSScriptRoot\..\src\Recyclarr" -o "$artifactDir\recyclarr-$runtime" -r $runtime
