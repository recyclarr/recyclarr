using System.Diagnostics;
using System.IO.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.Cli.Server;

/// <summary>
/// Spawns <c>recyclarr-server</c> as a child process for the duration of a single command.
/// </summary>
/// <remarks>
/// The child's stdout is a line protocol: <c>READY:{port}</c> announces the port the OS assigned,
/// and <c>LOG:{Level}:{message}</c> carries log events. Forwarding those events into this
/// process's logger is what keeps <c>--log</c> showing sync output now that the sync itself runs
/// server-side; draining the pipe also stops the child from blocking on a full stdout buffer.
/// The stdin pipe is held open as a lifeline: disposing this launcher closes stdin, which the
/// server's <c>StdinLifelineMonitor</c> reads as EOF and shuts down on (ADR-010).
/// </remarks>
internal sealed class EphemeralServerLauncher(
    ILogger log,
    IFileSystem fs,
    LoggingLevelSwitch levelSwitch
) : IAsyncDisposable
{
    private const string ReadyPrefix = "READY:";
    private const string LogPrefix = "LOG:";
    private const string LoopbackUrl = "http://127.0.0.1:0";

    private Process? _process;
    private StreamWriter? _stdinWriter;
    private Task? _outputForwarding;

    /// <summary>
    /// Starts the server process and returns its base address once it reports readiness.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the server binary is missing, or the process exits before sending READY.
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
                // Port 0 lets the OS pick a free port, so concurrent CLI invocations and a
                // separately running `recyclarr serve` never contend for one (ADR-010). The
                // address must be a literal loopback IP: Kestrel rejects dynamic ports on the
                // "localhost" alias because it cannot bind both IPv4 and IPv6 to the same
                // OS-assigned port.
                Arguments =
                    $"--urls={LoopbackUrl} "
                    + $"--parent-pid={Environment.ProcessId} "
                    + $"--log-level={levelSwitch.MinimumLevel}",
            },
        };

        _process.Start();
        _stdinWriter = _process.StandardInput;

        var address = await ReadUntilReadyAsync(_process.StandardOutput, ct);
        _outputForwarding = ForwardOutputAsync(_process.StandardOutput);
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

        if (_outputForwarding is not null)
        {
            await _outputForwarding;
        }
    }

    private async Task<Uri> ReadUntilReadyAsync(TextReader output, CancellationToken ct)
    {
        while (true)
        {
            var line =
                await output.ReadLineAsync(ct)
                ?? throw new InvalidOperationException(
                    "Server process exited before sending the READY handshake."
                );

            if (line.StartsWith(ReadyPrefix, StringComparison.Ordinal))
            {
                return new Uri($"http://127.0.0.1:{line[ReadyPrefix.Length..]}");
            }

            ForwardLine(line);
        }
    }

    private async Task ForwardOutputAsync(TextReader output)
    {
        while (await output.ReadLineAsync() is { } line)
        {
            ForwardLine(line);
        }
    }

    private void ForwardLine(string line)
    {
        if (!line.StartsWith(LogPrefix, StringComparison.Ordinal))
        {
            log.Debug("Unrecognized server output: {Line}", line);
            return;
        }

        var payload = line[LogPrefix.Length..];
        var separator = payload.IndexOf(':', StringComparison.Ordinal);
        if (separator < 0 || !Enum.TryParse<LogEventLevel>(payload[..separator], out var level))
        {
            log.Debug("Unrecognized server output: {Line}", line);
            return;
        }

        log.Write(level, "{ServerMessage}", payload[(separator + 1)..]);
    }
}
