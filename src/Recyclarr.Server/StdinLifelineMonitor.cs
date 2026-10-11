using System.Diagnostics;

namespace Recyclarr.Server;

/// <summary>
/// Hosted service that monitors stdin for EOF and polls whether the parent process is still alive.
/// Either condition triggers graceful application shutdown (belt-and-suspenders). Only registered
/// when the server is launched with <c>--parent-pid={pid}</c>, by any CLI command that owns the
/// server process (ephemeral launch and <c>recyclarr serve</c>).
/// </summary>
internal sealed class StdinLifelineMonitor(IHostApplicationLifetime lifetime, int parentPid)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Console reads block synchronously and ignore cancellation, so the stdin watch runs on
        // its own thread and is abandoned at shutdown. Awaiting it would hold shutdown until the
        // host timeout whenever the server stops (for example, on Ctrl+C) while stdin is open.
        var stdinTask = Task.Run(WatchStdin, CancellationToken.None);
        var parentTask = PollParentAsync(stoppingToken);

        // Whichever lifeline breaks first triggers shutdown
        await Task.WhenAny(stdinTask, parentTask);
        lifetime.StopApplication();
    }

    /// <summary>Reads stdin; returns when EOF is received.</summary>
    private static void WatchStdin()
    {
        var buffer = new char[1];

        // EOF (0): the parent closed its end of the stdin pipe
        while (Console.In.Read(buffer, 0, 1) > 0) { }
    }

    /// <summary>
    /// Polls the parent process every <see cref="PollInterval"/>; returns when the process no
    /// longer exists or the token is cancelled.
    /// </summary>
    private async Task PollParentAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollInterval, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!IsParentAlive())
            {
                return;
            }
        }
    }

    private bool IsParentAlive()
    {
        try
        {
            using var process = Process.GetProcessById(parentPid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            // Process does not exist
            return false;
        }
    }
}
