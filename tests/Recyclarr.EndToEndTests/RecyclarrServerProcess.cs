using System.Globalization;
using CliWrap;
using NUnit.Framework;

namespace Recyclarr.EndToEndTests;

/// <summary>
/// A running published <c>recyclarr-server</c> process. The server loads configuration once at
/// startup (ADR-019), so a test that needs different configuration stops this process and starts
/// a new one. Server output is streamed to the test progress log as it arrives.
/// </summary>
internal sealed class RecyclarrServerProcess : IAsyncDisposable
{
    private const string ReadyPrefix = "READY:";

    private readonly CancellationTokenSource _forcefulStop;
    private readonly CancellationTokenSource _gracefulStop;
    private readonly Task<CommandResult> _execution;

    public Uri BaseAddress { get; }

    private RecyclarrServerProcess(
        CancellationTokenSource forcefulStop,
        CancellationTokenSource gracefulStop,
        Task<CommandResult> execution,
        Uri baseAddress
    )
    {
        _forcefulStop = forcefulStop;
        _gracefulStop = gracefulStop;
        _execution = execution;
        BaseAddress = baseAddress;
    }

    public static async Task<RecyclarrServerProcess> StartAsync(
        string binaryPath,
        IReadOnlyDictionary<string, string?> environment,
        CancellationToken ct
    )
    {
        var forcefulStop = new CancellationTokenSource();
        var gracefulStop = new CancellationTokenSource();
        var readyPort = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        var execution = Cli.Wrap(binaryPath)
            // Port 0 lets the OS pick a free port; the READY line reports the one bound.
            .WithArguments(["--urls=http://127.0.0.1:0"])
            .WithEnvironmentVariables(environment)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => OnOutput(line, readyPort)))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(OnErrorOutput))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(forcefulStop.Token, gracefulStop.Token)
            .Task;

        await using var cancelReady = ct.Register(() => readyPort.TrySetCanceled(ct));
        var first = await Task.WhenAny(readyPort.Task, execution);
        if (first != readyPort.Task)
        {
            var result = await execution;
            forcefulStop.Dispose();
            gracefulStop.Dispose();
            throw new InvalidOperationException(
                $"recyclarr-server exited with code {result.ExitCode} before reporting READY; "
                    + "see the server output above."
            );
        }

        var port = await readyPort.Task;
        return new RecyclarrServerProcess(
            forcefulStop,
            gracefulStop,
            execution,
            new Uri($"http://127.0.0.1:{port}")
        );
    }

    private static void OnOutput(string line, TaskCompletionSource<int> readyPort)
    {
        TestContext.Progress.WriteLine($"[server] {line}");
        if (line.StartsWith(ReadyPrefix, StringComparison.Ordinal))
        {
            readyPort.TrySetResult(
                int.Parse(line[ReadyPrefix.Length..], CultureInfo.InvariantCulture)
            );
        }
    }

    private static void OnErrorOutput(string line)
    {
        TestContext.Progress.WriteLine($"[server:stderr] {line}");
    }

    /// <summary>
    /// Sends SIGINT so the server shuts down cleanly, and kills it if it has not exited within a
    /// few seconds.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _forcefulStop.CancelAfter(TimeSpan.FromSeconds(10));
        await _gracefulStop.CancelAsync();

        try
        {
            await _execution;
        }
        catch (OperationCanceledException)
        {
            // Expected: CliWrap reports a requested stop as cancellation.
        }
        finally
        {
            _forcefulStop.Dispose();
            _gracefulStop.Dispose();
        }
    }
}
