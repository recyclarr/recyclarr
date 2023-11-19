[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $SonarToken,

    [switch] $Details
)

$version = $(dotnet-gitversion /showvariable semver)
if ($LASTEXITCODE -ne 0) { throw "Failed: dotnet gitversion" }

$beginArgs = @(
    "-o:recyclarr"
    "-k:recyclarr_recyclarr"
    "-n:Recyclarr"
    "-v:$version"
    "-d:sonar.token=$SonarToken"
    "-d:sonar.host.url=https://sonarcloud.io"
    "-d:sonar.cs.opencover.reportsPaths=**/TestResults/*/coverage.opencover.xml"
)

if ($Details) {
    $beginArgs += "-d:sonar.verbose=true"
}

"Args: $beginArgs"
dotnet sonarscanner begin @beginArgs
if ($LASTEXITCODE -ne 0) { throw "Failed: sonarscanner begin" }

try {
    # Run a full build command because if we just do the tests, it will not build everything.
    # Building everything is important to ensure we analyze all code in the solution.
    dotnet build
    if ($LASTEXITCODE -ne 0) { throw "Failed: dotnet build" }

    dotnet test --no-build --collect:"XPLat Code Coverage;Format=opencover"
    if ($LASTEXITCODE -ne 0) { throw "Failed: dotnet test" }
}
finally {
    dotnet sonarscanner end "-d:sonar.token=$SonarToken"
    if ($LASTEXITCODE -ne 0) { throw "Failed: sonarscanner end" }
}
