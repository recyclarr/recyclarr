[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $PublishDir
)

$ErrorActionPreference = "Stop"

# Windows binaries carry an .exe suffix; Unix binaries have none.
$suffix = if ($IsWindows) { ".exe" } else { "" }
$pathToCli = Join-Path $PublishDir "recyclarr$suffix"
$pathToServer = Join-Path $PublishDir "recyclarr-server$suffix"

if (Get-Command chmod -errorAction SilentlyContinue) {
    "The chmod command was found. Setting read + execute permission."
    & chmod +rx $pathToCli $pathToServer
}

$version = & $pathToCli --version
if ($LASTEXITCODE -ne 0) {
    throw "Recyclarr CLI failed to report its version"
}
"Recyclarr CLI started successfully: $version"

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $pathToServer
$startInfo.ArgumentList.Add("--urls=http://127.0.0.1:0")
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.UseShellExecute = $false
# The server refuses to start without an API key of at least 32 characters.
$startInfo.Environment["RECYCLARR_API_KEY"] = "smoke-test-api-key-0123456789abcdef"

$process = [System.Diagnostics.Process]::Start($startInfo)
$deadline = [DateTime]::UtcNow.AddSeconds(30)
# The server logs to stdout, so keep consumed lines to report why it exited.
$output = [System.Collections.Generic.List[string]]::new()

try {
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($process.HasExited) {
            $output.Add($process.StandardOutput.ReadToEnd())
            $output.Add($process.StandardError.ReadToEnd())
            throw "Recyclarr server exited before startup:`n$($output -join "`n")"
        }

        $readTask = $process.StandardOutput.ReadLineAsync()
        $timeout = [TimeSpan]::FromTicks([Math]::Max(0, ($deadline - [DateTime]::UtcNow).Ticks))
        try {
            $line = $readTask.WaitAsync($timeout).GetAwaiter().GetResult()
        }
        catch [System.TimeoutException] {
            break
        }

        if ($null -ne $line) {
            $output.Add($line)
        }

        if ($line -like "READY:*") {
            "Recyclarr server started successfully: $line"
            exit 0
        }
    }

    # Stop the server first so reading stderr to the end cannot block.
    $process.Kill($true)
    $process.WaitForExit()
    $output.Add($process.StandardError.ReadToEnd())
    throw "Recyclarr server did not start within 30 seconds:`n$($output -join "`n")"
}
finally {
    if (-not $process.HasExited) {
        $process.Kill($true)
        $process.WaitForExit()
    }
}
