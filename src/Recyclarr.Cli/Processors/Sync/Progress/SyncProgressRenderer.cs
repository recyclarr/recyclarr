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

        // Merge instance and pipeline events, fold into immutable snapshots via Scan.
        // Subscribe replaces the snapshot reference atomically for the render loop to poll.
        using var subscription = Observable
            .Merge(
                run.Instances.Select(e => (SyncRunEvent)e),
                run.Pipelines.Select(e => (SyncRunEvent)e)
            )
            .Scan(
                _snapshot,
                (snapshot, evt) =>
                    evt switch
                    {
                        InstanceEvent ie => ApplyInstanceEvent(snapshot, ie),
                        PipelineEvent pe => ApplyPipelineEvent(snapshot, pe),
                        _ => snapshot,
                    }
            )
            .Subscribe(s => _snapshot = s);

        console.MarkupLine(
            "[grey]Legend:[/] "
                + "[green]✓[/] ok [grey]·[/] "
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

    private static ProgressSnapshot ApplyInstanceEvent(ProgressSnapshot snapshot, InstanceEvent evt)
    {
        var index = snapshot.Instances.FindIndex(i =>
            i.Name.Equals(evt.Name, StringComparison.OrdinalIgnoreCase)
        );
        if (index < 0)
        {
            return snapshot;
        }

        var updated = snapshot.Instances[index] with { Status = evt.Status };
        return snapshot with { Instances = snapshot.Instances.SetItem(index, updated) };
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
        var pipelineSnapshot = new PipelineSnapshot(evt.Status, evt.Count);
        var updated = instance with
        {
            Pipelines = instance.Pipelines.SetItem(evt.Type, pipelineSnapshot),
        };
        return snapshot with { Instances = snapshot.Instances.SetItem(index, updated) };
    }
}
