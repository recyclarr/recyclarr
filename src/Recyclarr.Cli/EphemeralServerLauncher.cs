using System.Diagnostics;
using System.IO.Abstractions;

namespace Recyclarr.Cli;

/// <summary>
/// Spawns <c>recyclarr-server</c> as a child process for programmatic use. Captures stdout to read
/// the <c>READY:{port}</c> handshake, then exposes <see cref="BaseAddress"/>. The stdin pipe
/// is kept open as a lifeline: disposing this launcher closes stdin, triggering the server's
/// <c>StdinLifelineMonitor</c> to shut down the process cleanly.
/// </summary>
internal sealed class EphemeralServerLauncher(IFileSystem fs) : IAsyncDisposable
{
    private Process? _process;
    private StreamWriter? _stdinWriter;

    /// <summary>
    /// Base address of the running server (e.g. <c>http://localhost:7982</c>). Only valid after a
    /// successful call to <see cref="StartAsync"/>.
    /// </summary>
    public string? BaseAddress { get; private set; }

    /// <summary>
    /// Starts the server process, waits for the READY handshake, and populates
    /// <see cref="BaseAddress"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the server exits before sending the READY handshake.
    /// </exception>
    public async Task StartAsync(CancellationToken ct = default)
    {
        var serverBinary = GetServerBinary();

        _process = new Process
        {
            StartInfo = new ProcessStartInfo(serverBinary.FullName)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                Arguments = $"--parent-pid={Environment.ProcessId}",
            },
        };

        _process.Start();
        _stdinWriter = _process.StandardInput;

        // Read stdout lines until the READY handshake arrives
        while (true)
        {
            var line =
                await _process.StandardOutput.ReadLineAsync(ct)
                ?? throw new InvalidOperationException(
                    "Server process exited before sending the READY handshake."
                );

            if (!line.StartsWith("READY:", StringComparison.Ordinal))
            {
                continue;
            }

            var portStr = line["READY:".Length..];
            BaseAddress = $"http://localhost:{portStr}";
            break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_stdinWriter is not null)
        {
            // Close stdin to signal EOF → server's StdinLifelineMonitor calls StopApplication()
            await _stdinWriter.DisposeAsync();
        }

        if (_process is not null)
        {
            await _process.WaitForExitAsync();
            _process.Dispose();
        }
    }

    private IFileInfo GetServerBinary()
    {
        // non-null: ProcessPath is only null in bundled single-file hosts without apphost
        var processDir = fs.FileInfo.New(Environment.ProcessPath!).Directory!;
        var name = OperatingSystem.IsWindows() ? "recyclarr-server.exe" : "recyclarr-server";
        return processDir.File(name);
    }
}
