using System.Diagnostics;

namespace Recyclarr.Server;

/// <summary>
/// Hosted service that monitors stdin for EOF and polls whether the parent process is still alive.
/// Either condition triggers graceful application shutdown (belt-and-suspenders). Only registered
/// when the server is launched with <c>--parent-pid={pid}</c> (ephemeral launch mode).
/// </summary>
internal sealed class StdinLifelineMonitor(IHostApplicationLifetime lifetime, int parentPid)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stdinTask = WatchStdinAsync(stoppingToken);
        var parentTask = PollParentAsync(stoppingToken);

        // Whichever lifeline breaks first triggers shutdown
        await Task.WhenAny(stdinTask, parentTask);
        lifetime.StopApplication();
    }

    /// <summary>Reads stdin; returns when EOF is received or the token is cancelled.</summary>
    private static async Task WatchStdinAsync(CancellationToken ct)
    {
        var buffer = new char[1];

        while (!ct.IsCancellationRequested)
        {
            int read;
            try
            {
                read = await Console.In.ReadAsync(buffer, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (read == 0)
            {
                // EOF: parent closed its end of the stdin pipe
                return;
            }
        }
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
