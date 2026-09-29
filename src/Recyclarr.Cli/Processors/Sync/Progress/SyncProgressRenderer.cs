using Recyclarr.Client.V1;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync.Progress;

internal class SyncProgressRenderer(IAnsiConsole console)
{
    private const int RefreshIntervalMs = 80;

    private readonly ProgressTableBuilder _tableBuilder = new();
    private IReadOnlyList<InstanceSnapshotResponse> _snapshot = [];

    /// <summary>
    /// Renders a live table from job snapshots as they arrive, and returns the final one. The
    /// display refreshes faster than snapshots arrive so the spinner keeps animating between polls.
    /// The live table clears when the job ends; the caller replaces it with the results table.
    /// </summary>
    public async Task<GetSyncJobResponse> RenderProgressAsync(
        IAsyncEnumerable<GetSyncJobResponse> updates,
        CancellationToken ct
    )
    {
        GetSyncJobResponse? lastJob = null;

        console.MarkupLine(
            "[grey]Legend:[/] "
                + $"{StatusMarkup.Ok} ok [grey]·[/] "
                + $"{StatusMarkup.Partial} partial [grey]·[/] "
                + $"{StatusMarkup.Failed} failed [grey]·[/] "
                + $"{StatusMarkup.NotRun} not run"
                + "\n"
        );

        await console
            .Live(ProgressTableBuilder.BuildTable(_snapshot, _tableBuilder.GetNextSpinnerFrame()))
            .AutoClear(true)
            .StartAsync(RunPollLoop);

        return lastJob
            ?? throw new InvalidOperationException("The sync job reported no status at all");

        async Task RunPollLoop(LiveDisplayContext ctx)
        {
            var consuming = ConsumeUpdatesAsync();

            while (!consuming.IsCompleted)
            {
                UpdateTable();

                try
                {
                    await Task.Delay(RefreshIntervalMs, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            UpdateTable();
            await consuming;
            return;

            void UpdateTable()
            {
                ctx.UpdateTarget(
                    ProgressTableBuilder.BuildTable(_snapshot, _tableBuilder.GetNextSpinnerFrame())
                );
            }
        }

        async Task ConsumeUpdatesAsync()
        {
            await foreach (var job in updates.WithCancellation(ct))
            {
                lastJob = job;
                _snapshot = [.. job.Progress];
            }
        }
    }
}
