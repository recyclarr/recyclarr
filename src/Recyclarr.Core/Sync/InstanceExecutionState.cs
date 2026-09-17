using Recyclarr.Config.Models;
using Recyclarr.Sync.Results;

namespace Recyclarr.Sync;

internal sealed class InstanceExecutionState(IServiceConfiguration config)
{
    private IReadOnlyList<PlanningOutcome> _planningOutcomes = [];

    public PipelineExecutionBuffer PipelineBuffer { get; } = new();
    public SyncInstanceResult? CompletedResult { get; private set; }

    public void RetainPlanningOutcomes(IReadOnlyList<PlanningOutcome> outcomes)
    {
        _planningOutcomes = outcomes.ToList().AsReadOnly();
    }

    public void RetainCompletedResult(SyncInstanceResult result)
    {
        CompletedResult = result;
    }

    public SyncInstanceResult BuildFaultedResult(SyncFault fault)
    {
        var result = CompletedResult;
        return new SyncInstanceResult(
            config.InstanceName,
            config.ServiceType,
            result?.Pipelines ?? PipelineBuffer.Results,
            result?.Failure,
            result?.PlanningOutcomes ?? _planningOutcomes,
            fault
        );
    }
}
