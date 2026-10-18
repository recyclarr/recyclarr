using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.Plan;

/// <summary>
/// What one planner may read and report while planning its pipeline. Prerequisite plans come only
/// from declared dependencies, which <see cref="PipelineGraph"/> has already planned. Sync outcomes
/// belong to the current pipeline and reach only its operation; planning outcomes belong to the
/// instance (ADR-015).
/// </summary>
internal sealed class PlanningContext(IPipeline pipeline, PipelineGraph graph, PipelinePlan plan)
{
    private readonly List<SyncOutcome> _outcomes = [];

    public IReadOnlyList<SyncOutcome> Outcomes => _outcomes;

    public void Add(SyncOutcome outcome) => _outcomes.Add(outcome);

    public void AddPlanningOutcome(PlanningOutcome outcome) => plan.AddPlanningOutcome(outcome);

    public TPlan Require<TPlan>()
        where TPlan : class
    {
        var dependency =
            pipeline.Dependencies.Select(graph.Get).FirstOrDefault(x => x.PlanType == typeof(TPlan))
            ?? throw new InvalidOperationException(
                $"{pipeline.Type} planning requires {typeof(TPlan).Name}, "
                    + "which no declared dependency produces"
            );

        return plan.Get<TPlan>().Value
            ?? throw new InvalidOperationException(
                $"{dependency.Type} produced no plan for {pipeline.Type} to read"
            );
    }
}
