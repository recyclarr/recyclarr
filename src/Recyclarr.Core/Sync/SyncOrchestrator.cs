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
        CancellationToken ct
    )
    {
        var instances = new List<SemanticInstanceResult>();

        foreach (var config in configs)
        {
            ct.ThrowIfCancellationRequested();

            var state = new InstanceExecutionState(config);
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

            if (faultException is not null)
            {
                instances.Add(state.BuildFaultedResult(ReportFault(faultException)));
                continue;
            }

            if (state.CompletedResult is not { } completedResult)
            {
                throw new InvalidOperationException("Instance attempt produced no terminal result");
            }

            instances.Add(completedResult);
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
}
