using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Sync.Results;
using SemanticInstanceResult = Recyclarr.Sync.Results.SyncInstanceResult;

namespace Recyclarr.Sync;

internal class SyncOrchestrator(
    InstanceScopeFactory instanceScopeFactory,
    ISyncFaultReporter faultReporter
) : ISyncOrchestrator
{
    public async Task<SyncRunResult> RunAsync(
        IReadOnlyList<IServiceConfiguration> configs,
        ISyncSettings settings,
        IInstanceSyncProgress progress,
        CancellationToken ct
    )
    {
        var instances = new List<SemanticInstanceResult>();

        foreach (var config in configs)
        {
            ct.ThrowIfCancellationRequested();

            ReportProgressSafely(() => progress.InstanceStarted(config.InstanceName));
            var completedResult = await ExecuteInstanceAsync(config, settings, ct);

            instances.Add(completedResult);
            ReportProgressSafely(() => progress.InstanceCompleted(completedResult));
        }

        return new SyncRunResult(instances);
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The instance boundary converts unexpected failures into opaque faults."
    )]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Disposal is explicit so its fault is captured alongside processing faults."
    )]
    private async Task<SemanticInstanceResult> ExecuteInstanceAsync(
        IServiceConfiguration config,
        ISyncSettings settings,
        CancellationToken ct
    )
    {
        var state = new InstanceExecutionState(config);
        LifetimeScopeWrapper<InstanceSyncProcessor>? instanceScope = null;
        Exception? attemptException = null;

        try
        {
            instanceScope = instanceScopeFactory.Start<InstanceSyncProcessor>(config);
            state.RetainCompletedResult(await instanceScope.Entry.Process(settings, state, ct));
        }
        catch (Exception e)
        {
            attemptException = e;
        }

        var cleanupException = DisposeCapturingFault(instanceScope);
        Exception[] exceptions =
        [
            .. new[] { attemptException, cleanupException }.OfType<Exception>(),
        ];

        if (exceptions.Length == 0)
        {
            return state.CompletedResult
                ?? throw new InvalidOperationException(
                    "Instance attempt produced no terminal result"
                );
        }

        // Only a signaled run token stops the run; any other cancellation is an instance fault.
        var cancellation = ct.IsCancellationRequested
            ? exceptions.OfType<OperationCanceledException>().FirstOrDefault()
            : null;
        if (cancellation is not null)
        {
            var fault = exceptions.FirstOrDefault(e => e is not OperationCanceledException);
            if (fault is not null)
            {
                ReportFault(fault);
            }

            ExceptionDispatchInfo.Throw(cancellation);
        }

        var faultException =
            exceptions.Length == 1 ? exceptions[0] : new AggregateException(exceptions);
        return state.BuildFaultedResult(ReportFault(faultException));
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Disposal faults are retained on the instance result."
    )]
    private static Exception? DisposeCapturingFault(
        LifetimeScopeWrapper<InstanceSyncProcessor>? scope
    )
    {
        try
        {
            scope?.Dispose();
            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Fault reporting cannot alter execution or terminal results."
    )]
    private SyncFault ReportFault(Exception exception)
    {
        var reference = Guid.NewGuid().ToString("N");
        try
        {
            faultReporter.Report(reference, exception);
        }
        catch
        {
            // Fault reporting must not erase the terminal result.
        }

        return new SyncFault(reference);
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Progress reporting cannot alter execution or terminal results."
    )]
    private static void ReportProgressSafely(Action report)
    {
        try
        {
            report();
        }
        catch
        {
            // Progress is best effort; terminal results remain authoritative.
        }
    }
}
