using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Server.Sync;

// Entry point resolved inside a run's lifetime scope (see SyncRunScopeFactory). Subscribes to the
// scope's ISyncRunScope observables to accumulate progress/diagnostics into the job store as the
// run progresses, then records the terminal status once the orchestrator completes.
internal sealed class SyncJobRunner(
    ILogger log,
    ISyncOrchestrator orchestrator,
    ISyncRunScope run,
    ISyncRunResults results,
    ISyncJobStore store,
    INotificationService notify,
    SyncDiagnosticsLogger diagnosticsLogger
)
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
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

        var terminalStatus = SyncJobStatus.Failed;
        IReadOnlyList<SyncJobInstanceResult> instanceResults = [];

        try
        {
            var status = await orchestrator.RunAsync(configs, settings, ct);
            terminalStatus = DeriveJobStatus(status, snapshot);

            // Capture results before returning: this method runs inside the run's lifetime
            // scope, and ISyncRunResults goes away with it, taking the ability to look results
            // up (ADR-014). Mapping to the wire happens at the endpoint, not here.
            instanceResults = configs
                .Select(c => new SyncJobInstanceResult(
                    c.InstanceName,
                    results.GetInstanceResult(c.InstanceName)
                ))
                .ToList();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            diagnostics.Add(new SyncDiagnosticEvent(null, SyncDiagnosticLevel.Error, e.Message));
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
                j.Results = instanceResults;
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

    private static SyncJobStatus DeriveJobStatus(ExitStatus exitStatus, ProgressSnapshot snapshot)
    {
        if (exitStatus == ExitStatus.Failed)
        {
            return SyncJobStatus.Failed;
        }

        var instanceStatuses = snapshot.Instances.Select(i => i.Status).ToList();

        if (instanceStatuses.Any(s => s == InstanceProgressStatus.Failed))
        {
            return SyncJobStatus.Failed;
        }

        if (instanceStatuses.Any(s => s == InstanceProgressStatus.Partial))
        {
            return SyncJobStatus.Partial;
        }

        return SyncJobStatus.Succeeded;
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
