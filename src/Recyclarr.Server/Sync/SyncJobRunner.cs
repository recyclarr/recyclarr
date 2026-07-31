using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync;

// Entry point resolved inside a run's lifetime scope (see SyncRunScopeFactory). Subscribes to the
// scope's ISyncRunScope observables to accumulate progress/diagnostics into the job store as the
// run progresses, then records the terminal status once the orchestrator completes.
internal sealed class SyncJobRunner(
    ILogger log,
    ISyncOrchestrator orchestrator,
    ISyncRunScope run,
    ISyncJobStore store,
    INotificationService notify,
    SyncDiagnosticsLogger diagnosticsLogger
)
{
    public async Task RunAsync(
        JobId jobId,
        IReadOnlyList<IServiceConfiguration> configs,
        ISyncSettings settings,
        CancellationToken ct
    )
    {
        // Injected to activate the server's diagnostic log subscription.
        _ = diagnosticsLogger;

        var diagnostics = new List<SyncDiagnosticEvent>();
        var snapshot = BuildInitialSnapshot(configs);

        store.Update(
            jobId,
            j =>
            {
                j.Status = SyncJobStatus.Running;
                j.Progress = snapshot;
            }
        );

        using var diagnosticsSubscription = run.Diagnostics.Subscribe(evt =>
        {
            diagnostics.Add(evt);
            store.Update(jobId, j => j.Diagnostics = diagnostics.ToList());
        });

        using var pipelineSubscription = run.Pipelines.Subscribe(evt =>
        {
            snapshot = ApplyPipelineEvent(snapshot, evt);
            store.Update(jobId, j => j.Progress = snapshot);
        });

        SyncJobStatus terminalStatus;
        SyncRunResult? result;

        try
        {
            result = await orchestrator.RunAsync(configs, settings, ct);
            terminalStatus = result.Status.ToJobStatus();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            diagnostics.Add(new SyncDiagnosticEvent(null, SyncDiagnosticLevel.Error, e.Message));
            var reference = Guid.NewGuid().ToString("N");
            log.Error(e, "Unexpected sync runner fault {Reference}", reference);
            result = new SyncRunResult([], new SyncFault(reference));
            terminalStatus = result.Status.ToJobStatus();
        }

        // Sent from here rather than by the API caller: the notification body is built from the
        // ISyncRunScope observables, which only exist inside this lifetime scope. It also runs
        // before the terminal status is recorded, so a client that sees the job finish sees every
        // diagnostic the run produced, including a failed notification.
        await SendNotificationAsync(diagnostics);

        store.Update(
            jobId,
            j =>
            {
                j.Result = result;
                j.Diagnostics = diagnostics.ToList();
                j.Status = terminalStatus;
            }
        );
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task SendNotificationAsync(List<SyncDiagnosticEvent> diagnostics)
    {
        try
        {
            await notify.SendNotification();
        }
        catch (Exception e)
        {
            log.Warning(e, "Failed to send notification");
            diagnostics.Add(
                new SyncDiagnosticEvent(
                    null,
                    SyncDiagnosticLevel.Warning,
                    $"Failed to send notification: {e.Message}"
                )
            );
        }
    }

    private static ProgressSnapshot BuildInitialSnapshot(
        IReadOnlyList<IServiceConfiguration> configs
    )
    {
        var instances = configs
            .Select(c => new InstanceSnapshot(c.InstanceName, InstanceProgressStatus.Pending, []))
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
            new PipelineSnapshot(evt.Status, evt.Count, evt.Changes)
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
