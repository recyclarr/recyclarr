using System.Diagnostics;
using System.IO.Abstractions;

namespace Recyclarr.Cli.Server;

/// <summary>
/// Spawns <c>recyclarr-server</c> as a child process for the duration of a single command.
/// </summary>
/// <remarks>
/// The child's stdout carries only <c>READY:{port}</c>, which announces the port the OS assigned.
/// Its stderr carries only error log events, which explain a server that exits before READY; the
/// server's own log files hold everything else. Both pipes are drained for the child's lifetime so
/// it never blocks on a full buffer. The stdin pipe is held open as a lifeline: disposing this
/// launcher closes stdin, which the server's <c>StdinLifelineMonitor</c> reads as EOF and shuts
/// down on (ADR-010).
/// </remarks>
internal sealed class EphemeralServerLauncher(IFileSystem fs) : IAsyncDisposable
{
    private const string ReadyPrefix = "READY:";
    private const string LoopbackUrl = "http://127.0.0.1:0";

    private Process? _process;
    private StreamWriter? _stdinWriter;
    private Task? _outputDrain;
    private Task<List<string>>? _errorCapture;

    /// <summary>
    /// Starts the server process and returns its base address once it reports readiness.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the server binary is missing.
    /// </exception>
    /// <exception cref="ServerStartException">
    /// Thrown when the process exits before sending READY.
    /// </exception>
    public async Task<Uri> StartAsync(CancellationToken ct)
    {
        var serverBinary = ServerBinaryLocator.GetServerBinary(fs);
        if (!serverBinary.Exists)
        {
            throw new InvalidOperationException(
                $"Server binary not found: {serverBinary.FullName}"
            );
        }

        _process = new Process
        {
            StartInfo = new ProcessStartInfo(serverBinary.FullName)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // Port 0 lets the OS pick a free port, so concurrent CLI invocations and a
                // separately running `recyclarr serve` never contend for one (ADR-010). The
                // address must be a literal loopback IP: Kestrel rejects dynamic ports on the
                // "localhost" alias because it cannot bind both IPv4 and IPv6 to the same
                // OS-assigned port.
                Arguments =
                    $"--urls={LoopbackUrl} "
                    + $"--parent-pid={Environment.ProcessId} "
                    + "--ephemeral=true",
            },
        };

        _process.Start();
        _stdinWriter = _process.StandardInput;
        _errorCapture = CaptureLinesAsync(_process.StandardError);

        // stdout closing without READY means the server exited, so stderr is complete too
        var address =
            await ReadUntilReadyAsync(_process.StandardOutput, ct)
            ?? throw new ServerStartException(await _errorCapture);

        _outputDrain = CaptureLinesAsync(_process.StandardOutput);
        return address;
    }

    public async ValueTask DisposeAsync()
    {
        if (_stdinWriter is not null)
        {
            // Closing stdin signals EOF, which the server treats as "my parent is gone".
            await _stdinWriter.DisposeAsync();
        }

        if (_process is not null)
        {
            await _process.WaitForExitAsync();
            _process.Dispose();
        }

        if (_outputDrain is not null)
        {
            await _outputDrain;
        }

        if (_errorCapture is not null)
        {
            await _errorCapture;
        }
    }

    /// <summary>
    /// Returns the server's address, or null when stdout closes first because the server exited
    /// before it was ready (usually invalid configuration, ADR-019).
    /// </summary>
    private static async Task<Uri?> ReadUntilReadyAsync(TextReader output, CancellationToken ct)
    {
        while (await output.ReadLineAsync(ct) is { } line)
        {
            if (line.StartsWith(ReadyPrefix, StringComparison.Ordinal))
            {
                return new Uri($"http://127.0.0.1:{line[ReadyPrefix.Length..]}");
            }
        }

        return null;
    }

    private static async Task<List<string>> CaptureLinesAsync(TextReader output)
    {
        List<string> lines = [];
        while (await output.ReadLineAsync() is { } line)
        {
            lines.Add(line);
        }

        return lines;
    }
}
