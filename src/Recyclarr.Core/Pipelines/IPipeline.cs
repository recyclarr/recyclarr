using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines;

/// <summary>
/// One registered sync pipeline: its identity, the pipelines it depends on, its planner, and its
/// sync operation. One dependency list orders both stages: a dependency's plan is available to
/// the dependent planner, and a dependency that does not succeed blocks the dependent operation
/// (ADR-030).
/// </summary>
internal interface IPipeline
{
    PipelineType Type { get; }
    IReadOnlyList<PipelineType> Dependencies { get; }

    /// <summary>
    /// The plan type this pipeline produces. Each plan type belongs to exactly one pipeline, so
    /// planners request prerequisite plans by type.
    /// </summary>
    Type PlanType { get; }

    void Plan(PipelinePlan plan, PipelineGraph graph);

    /// <summary>
    /// False when the planner produced no plan because the instance configures nothing for this
    /// pipeline; the orchestrator then skips it and reports no result.
    /// </summary>
    bool HasPlan(PipelinePlan plan);

    Task<PipelineResult> Execute(
        bool preview,
        PipelinePlan plan,
        Action<PipelineResult> capture,
        CancellationToken ct
    );

    PipelineResult CreateBlockedResult(PipelineType dependency);
    PipelineResult CreateFailedResult(PipelineResult? current);
}

internal sealed class Pipeline<TPlan>(
    PipelineType type,
    IReadOnlyList<PipelineType> dependencies,
    IPipelinePlanner<TPlan> planner,
    ISyncOperation<TPlan> operation
) : IPipeline
    where TPlan : class
{
    public PipelineType Type => type;
    public IReadOnlyList<PipelineType> Dependencies => dependencies;
    public Type PlanType => typeof(TPlan);

    public void Plan(PipelinePlan plan, PipelineGraph graph)
    {
        var context = new PlanningContext(this, graph, plan);
        plan.Add(new PlannedPipeline<TPlan>(planner.Plan(context), context.Outcomes));
    }

    public bool HasPlan(PipelinePlan plan) => plan.Get<TPlan>().Value is not null;

    public Task<PipelineResult> Execute(
        bool preview,
        PipelinePlan plan,
        Action<PipelineResult> capture,
        CancellationToken ct
    )
    {
        var planned = plan.Get<TPlan>();
        var value =
            planned.Value ?? throw new InvalidOperationException($"{type} has no plan to execute");
        return operation.Execute(preview, value, planned.Outcomes, capture, ct);
    }

    public PipelineResult CreateBlockedResult(PipelineType dependency) =>
        operation.CreateBlockedResult(dependency);

    public PipelineResult CreateFailedResult(PipelineResult? current) =>
        operation.CreateFailedResult(current);
}
