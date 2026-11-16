using Recyclarr.Client.V1;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync.Progress;

internal class SyncProgressRenderer(IAnsiConsole console)
{
    private const int RefreshIntervalMs = 80;

    private readonly ProgressTableBuilder _tableBuilder = new();
    private IReadOnlyList<InstanceSnapshotResponse> _snapshot = [];

    /// <summary>
    /// Runs <paramref name="poll"/>, rendering a live table from each snapshot it reports, and
    /// returns the final snapshot <paramref name="poll"/> returns. The display refreshes faster
    /// than snapshots arrive so the spinner keeps animating between polls. The live table clears
    /// when the job ends; the caller replaces it with the results table. A non-interactive console
    /// only waits for the final snapshot, because live-display cursor operations can fail on a
    /// redirected stream.
    /// </summary>
    public async Task<GetSyncJobResponse> RenderProgressAsync(
        Func<Action<GetSyncJobResponse>, Task<GetSyncJobResponse>> poll,
        CancellationToken ct
    )
    {
        if (!console.Profile.Capabilities.Interactive)
        {
            return await poll(_ => { });
        }

        console.MarkupLine(
            "[grey]Legend:[/] "
                + $"{StatusMarkup.Ok} ok [grey]·[/] "
                + $"{StatusMarkup.Partial} partial [grey]·[/] "
                + $"{StatusMarkup.Failed} failed [grey]·[/] "
                + $"{StatusMarkup.NotRun} not run"
                + "\n"
        );

        return await console
            .Live(ProgressTableBuilder.BuildTable(_snapshot, _tableBuilder.GetNextSpinnerFrame()))
            .AutoClear(true)
            .StartAsync(RunPollLoop);

        async Task<GetSyncJobResponse> RunPollLoop(LiveDisplayContext ctx)
        {
            var polling = poll(job => _snapshot = [.. job.Progress]);

            while (!polling.IsCompleted)
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
            return await polling;

            void UpdateTable()
            {
                ctx.UpdateTarget(
                    ProgressTableBuilder.BuildTable(_snapshot, _tableBuilder.GetNextSpinnerFrame())
                );
            }
        }
    }
}
