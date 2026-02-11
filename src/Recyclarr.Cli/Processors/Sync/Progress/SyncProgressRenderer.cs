using Recyclarr.Sync.Progress;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync.Progress;

internal class SyncProgressRenderer : IDisposable
{
    private const int RefreshIntervalMs = 80;

    private readonly IAnsiConsole _console;
    private readonly ProgressTableBuilder _tableBuilder = new();
    private readonly IDisposable _subscription;
    private ProgressSnapshot _snapshot = new([]);

    public SyncProgressRenderer(IAnsiConsole console, IProgressSource progressSource)
    {
        _console = console;
        _subscription = progressSource.Observable.Subscribe(s => _snapshot = s);
    }

    public async Task RenderProgressAsync(Func<Task> syncAction, CancellationToken ct)
    {
        _console.MarkupLine(
            "[grey]Legend:[/] [green]✓[/] ok [grey]·[/] [red]✗[/] failed [grey]·[/] [grey]--[/] skipped"
        );
        _console.WriteLine();

        await _console
            .Live(ProgressTableBuilder.BuildTable(_snapshot, _tableBuilder.GetNextSpinnerFrame()))
            .AutoClear(false)
            .StartAsync(RunSyncLoop);

        _console.WriteLine();
        return;

        async Task RunSyncLoop(LiveDisplayContext ctx)
        {
            var syncTask = syncAction();

            while (!syncTask.IsCompleted)
            {
                ctx.UpdateTarget(
                    ProgressTableBuilder.BuildTable(_snapshot, _tableBuilder.GetNextSpinnerFrame())
                );

                try
                {
                    await Task.Delay(RefreshIntervalMs, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            ctx.UpdateTarget(
                ProgressTableBuilder.BuildTable(_snapshot, _tableBuilder.GetNextSpinnerFrame())
            );

            await syncTask;
        }
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
