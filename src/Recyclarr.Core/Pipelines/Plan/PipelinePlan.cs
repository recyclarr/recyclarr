using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.Plan;

/// <summary>
/// The planning output for one instance: each pipeline's typed plan and outcomes, keyed by plan
/// type, plus the instance-level planning outcomes (ADR-015). An instance-blocking error from any
/// pipeline fails every pipeline in the instance.
/// </summary>
internal sealed class PipelinePlan
{
    private readonly Dictionary<Type, object> _pipelines = [];
    private readonly List<PlanningOutcome> _planningOutcomes = [];

    public bool HasInstanceBlockingErrors { get; private set; }
    public IReadOnlyList<PlanningOutcome> PlanningOutcomes => _planningOutcomes;

    public void Add<TPlan>(PlannedPipeline<TPlan> planned)
        where TPlan : class
    {
        _pipelines.Add(typeof(TPlan), planned);
        HasInstanceBlockingErrors |= planned.Outcomes.Any(x =>
            x is { Level: SyncDiagnosticLevel.Error, Scope: SyncOutcomeScope.InstanceBlocking }
        );
    }

    public void AddPlanningOutcome(PlanningOutcome outcome)
    {
        _planningOutcomes.Add(outcome);
    }

    public PlannedPipeline<TPlan> Get<TPlan>()
        where TPlan : class => (PlannedPipeline<TPlan>)_pipelines[typeof(TPlan)];
}

/// <summary>
/// One pipeline's plan and the sync outcomes its planner reported. A null value means the
/// instance configures nothing for the pipeline.
/// </summary>
internal sealed record PlannedPipeline<TPlan>(TPlan? Value, IReadOnlyList<SyncOutcome> Outcomes)
    where TPlan : class;
