using System.Collections.Immutable;
using System.Reactive.Linq;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync.Progress;

internal class SyncProgressRenderer(IAnsiConsole console, ISyncRunScope run)
{
    private const int RefreshIntervalMs = 80;

    private readonly ProgressTableBuilder _tableBuilder = new();
    private ProgressSnapshot _snapshot = new([]);

    public async Task RenderProgressAsync(
        IReadOnlyList<string> instanceNames,
        Func<Task> syncAction,
        CancellationToken ct
    )
    {
        _snapshot = BuildInitialSnapshot(instanceNames);

        // Fold pipeline events into immutable snapshots via Scan.
        // Instance status is derived from pipeline statuses (worst-status-wins).
        // Subscribe replaces the snapshot reference atomically for the render loop to poll.
        using var subscription = run
            .Pipelines.Scan(_snapshot, ApplyPipelineEvent)
            .Subscribe(s => _snapshot = s);

        console.MarkupLine(
            "[grey]Legend:[/] "
                + "[green]✓[/] ok [grey]·[/] "
                + "[yellow]~[/] partial [grey]·[/] "
                + "[red]✗[/] failed [grey]·[/] "
                + "[grey]--[/] skipped"
                + "\n"
        );

        await console
            .Live(ProgressTableBuilder.BuildTable(_snapshot, _tableBuilder.GetNextSpinnerFrame()))
            .AutoClear(false)
            .StartAsync(RunSyncLoop);

        console.WriteLine();
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

    private static ProgressSnapshot BuildInitialSnapshot(IReadOnlyList<string> instanceNames)
    {
        var instances = instanceNames
            .Select(n => new InstanceSnapshot(n, InstanceProgressStatus.Pending, []))
            .ToImmutableList();

        return new ProgressSnapshot(instances);
    }

    private static ProgressSnapshot ApplyPipelineEvent(ProgressSnapshot snapshot, PipelineEvent evt)
    {
        var index = snapshot.Instances.FindIndex(i =>
            i.Name.Equals(evt.Instance, StringComparison.OrdinalIgnoreCase)
        );
        if (index < 0)
        {
            return snapshot;
        }

        var instance = snapshot.Instances[index];

        // Interrupted only affects pipelines that haven't reached a terminal state yet;
        // pipelines that already succeeded/failed/etc. keep their status.
        if (
            evt.Status is PipelineProgressStatus.Interrupted
            && instance.Pipelines.TryGetValue(evt.Type, out var existing)
            && IsTerminal(existing.Status)
        )
        {
            return snapshot;
        }

        var pipelines = instance.Pipelines.SetItem(
            evt.Type,
            new PipelineSnapshot(evt.Status, evt.Count)
        );
        var updated = instance with
        {
            Pipelines = pipelines,
            Status = InstanceSnapshot.DeriveStatus(pipelines),
        };
        return snapshot with { Instances = snapshot.Instances.SetItem(index, updated) };
    }

    private static bool IsTerminal(PipelineProgressStatus status)
    {
        return status
            is PipelineProgressStatus.Succeeded
                or PipelineProgressStatus.Partial
                or PipelineProgressStatus.Failed
                or PipelineProgressStatus.Skipped
                or PipelineProgressStatus.Interrupted;
    }
}
