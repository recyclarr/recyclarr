using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Sync.Results;
using SemanticInstanceResult = Recyclarr.Sync.Results.SyncInstanceResult;

namespace Recyclarr.Sync;

internal class SyncOrchestrator(
    InstanceScopeFactory instanceScopeFactory,
    ISyncFaultReporter? faultReporter = null
) : ISyncOrchestrator
{
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The instance boundary converts unexpected failures into opaque faults."
    )]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The explicit finally block captures disposal faults without losing context."
    )]
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

            var state = new InstanceExecutionState(config);
            ReportProgress(() => progress.InstanceStarted(config.InstanceName));
            LifetimeScopeWrapper<InstanceSyncProcessor>? instanceScope = null;
            Exception? attemptException = null;
            Exception? cleanupException = null;

            try
            {
                try
                {
                    instanceScope = instanceScopeFactory.Start<InstanceSyncProcessor>(config);
                    var result = await instanceScope.Entry.Process(settings, state, ct);
                    state.RetainCompletedResult(result);
                }
                catch (Exception e)
                {
                    attemptException = e;
                }
            }
            finally
            {
                try
                {
                    instanceScope?.Dispose();
                }
                catch (Exception e)
                {
                    cleanupException = e;
                }
            }

            if (attemptException is OperationCanceledException cancellation)
            {
                if (cleanupException is not null)
                {
                    ReportFault(cleanupException);
                }

                ExceptionDispatchInfo.Capture(cancellation).Throw();
            }

            var faultException = (attemptException, cleanupException) switch
            {
                ({ } primary, { } cleanup) => new AggregateException(primary, cleanup),
                ({ } primary, null) => primary,
                (null, { } cleanup) => cleanup,
                _ => null,
            };

            SemanticInstanceResult completedResult;
            if (faultException is not null)
            {
                completedResult = state.BuildFaultedResult(ReportFault(faultException));
            }
            else if (state.CompletedResult is not { } retainedResult)
            {
                throw new InvalidOperationException("Instance attempt produced no terminal result");
            }
            else
            {
                completedResult = retainedResult;
            }

            instances.Add(completedResult);
            ReportProgress(() => progress.InstanceCompleted(completedResult));
        }

        return new SyncRunResult(instances);
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
            faultReporter?.Report(reference, exception);
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
    private static void ReportProgress(Action report)
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
