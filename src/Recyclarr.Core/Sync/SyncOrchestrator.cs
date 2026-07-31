using System.Diagnostics.CodeAnalysis;
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
        Justification = "The run boundary converts unexpected failures into opaque fault results."
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
            var buffer = new PipelineExecutionBuffer();
            var instanceCompleted = false;
            try
            {
                using var instanceScope = instanceScopeFactory.Start<InstanceSyncProcessor>(config);
                instances.Add(await instanceScope.Entry.Process(settings, buffer, ct));
                instanceCompleted = true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (!instanceCompleted && buffer.Results.Count > 0)
                {
                    instances.Add(
                        new SemanticInstanceResult(
                            config.InstanceName,
                            config.ServiceType,
                            buffer.Results
                        )
                    );
                }

                var reference = Guid.NewGuid().ToString("N");
                try
                {
                    faultReporter?.Report(reference, e);
                }
                catch
                {
                    // Fault reporting must not erase the terminal result.
                }

                return new SyncRunResult(instances, new SyncFault(reference));
            }
        }

        return new SyncRunResult(instances);
    }
}
