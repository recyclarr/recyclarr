using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using CliWrap;
using Recyclarr.Platform;

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

    private readonly TaskCompletionSource _lifeline = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    private Task<CommandResult>? _execution;

    /// <summary>
    /// Starts the server process and returns its base address once it reports readiness.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the server binary is missing.
    /// </exception>
    /// <exception cref="ServerStartException">
    /// Thrown when the process exits before sending READY.
    /// </exception>
    [SuppressMessage("Style", "REC001", Justification = "Explicit None marks ct as ignored")]
    public async Task<Uri> StartAsync(string apiKey, CancellationToken ct)
    {
        var serverBinary = ServerBinaryLocator.GetServerBinary(fs);
        if (!serverBinary.Exists)
        {
            throw new InvalidOperationException(
                $"Server binary not found: {serverBinary.FullName}"
            );
        }

        var ready = new TaskCompletionSource<Uri>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        List<string> errors = [];

        _execution = CliWrap
            .Cli.Wrap(serverBinary.FullName)
            // Port 0 lets the OS pick a free port, so concurrent CLI invocations and a separately
            // running `recyclarr serve` never contend for one (ADR-010). The address must be a
            // literal loopback IP: Kestrel rejects dynamic ports on the "localhost" alias because
            // it cannot bind both IPv4 and IPv6 to the same OS-assigned port.
            .WithArguments([
                $"--urls={LoopbackUrl}",
                $"--parent-pid={Environment.ProcessId}",
                "--ephemeral=true",
            ])
            // Replaces any inherited key, so the server accepts only this command's key. An
            // inherited key file would conflict with it and stop the server (ADR-027).
            .WithEnvironmentVariables(env =>
                env.Set(ServerApiKeyLocator.Variable, apiKey)
                    .Set(ServerApiKeyLocator.FileVariable, null)
            )
            // stdin stays open until disposal completes the lifeline; CliWrap then closes it.
            .WithStandardInputPipe(PipeSource.Create((_, _) => _lifeline.Task))
            .WithStandardOutputPipe(
                PipeTarget.ToDelegate(line =>
                {
                    if (line.StartsWith(ReadyPrefix, StringComparison.Ordinal))
                    {
                        ready.TrySetResult(
                            new Uri($"http://127.0.0.1:{line[ReadyPrefix.Length..]}")
                        );
                    }
                })
            )
            .WithStandardErrorPipe(PipeTarget.ToDelegate(errors.Add))
            .WithValidation(CommandResultValidation.None)
            // The server's lifetime ends at disposal, not when the caller stops waiting for READY
            .ExecuteAsync(CancellationToken.None)
            .Task;

        await Task.WhenAny(ready.Task, _execution).WaitAsync(ct);
        if (ready.Task.IsCompleted)
        {
            return await ready.Task;
        }

        // The server exited before it was ready (usually invalid configuration, ADR-019). The
        // execution completes only after stderr is drained, so the error list is complete.
        await _execution;
        throw new ServerStartException(errors);
    }

    public async ValueTask DisposeAsync()
    {
        // Closing stdin signals EOF, which the server treats as "my parent is gone".
        _lifeline.TrySetResult();

        if (_execution is not null)
        {
            await _execution;
        }
    }
}
