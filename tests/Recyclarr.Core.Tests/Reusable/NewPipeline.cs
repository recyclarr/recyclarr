using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Core.Tests.Reusable;

/// <summary>
/// Runs one real sync operation through orchestration without planning: the plan is supplied
/// directly, so the planner is never called.
/// </summary>
internal static class NewPipeline
{
    public static CompositeSyncPipeline Executor<TPlan>(
        PipelineType type,
        ISyncOperation<TPlan> operation
    )
        where TPlan : class =>
        new(
            Substitute.For<ILogger>(),
            new PipelineGraph([
                new Pipeline<TPlan>(type, [], Substitute.For<IPipelinePlanner<TPlan>>(), operation),
            ])
        );

    public static PipelinePlan Plan<TPlan>(
        TPlan? value,
        IReadOnlyList<SyncOutcome>? outcomes = null
    )
        where TPlan : class
    {
        var plan = new PipelinePlan();
        plan.Add(new PlannedPipeline<TPlan>(value, outcomes ?? []));
        return plan;
    }
}
