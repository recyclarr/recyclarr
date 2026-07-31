[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $PathToServer
)

$ErrorActionPreference = "Stop"

if (Get-Command chmod -errorAction SilentlyContinue) {
    "The chmod command was found. Setting read + execute permission."
    & chmod +rx $PathToServer
}

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $PathToServer
$startInfo.ArgumentList.Add("--urls=http://127.0.0.1:0")
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.UseShellExecute = $false

$process = [System.Diagnostics.Process]::Start($startInfo)
$deadline = [DateTime]::UtcNow.AddSeconds(30)

try {
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($process.HasExited) {
            $errorOutput = $process.StandardError.ReadToEnd()
            throw "Recyclarr server exited before startup: $errorOutput"
        }

        $readTask = $process.StandardOutput.ReadLineAsync()
        $timeout = $deadline - [DateTime]::UtcNow
        $line = $readTask.WaitAsync($timeout).GetAwaiter().GetResult()

        if ($line -like "READY:*") {
            "Recyclarr server started successfully: $line"
            exit 0
        }
    }

    throw "Recyclarr server did not start within 30 seconds"
}
finally {
    if (-not $process.HasExited) {
        $process.Kill($true)
        $process.WaitForExit()
    }
}
