using Recyclarr.TrashGuide;

namespace Recyclarr.Sync.Results;

/// <summary>
/// The planning outcomes, terminal pipeline results, and optional operational failure for one
/// service instance.
/// </summary>
/// <remarks>
/// Collections are copied into stable snapshots. Status is derived from pipeline results, blocking
/// planning outcomes, and operational failures rather than from transient progress events.
/// </remarks>
public sealed record SyncInstanceResult
{
    public SyncInstanceResult(
        string instanceName,
        SupportedServices serviceType,
        IReadOnlyList<PipelineResult> pipelines,
        OperationalFailure? failure = null,
        IReadOnlyList<PlanningOutcome>? planningOutcomes = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceName);
        ArgumentNullException.ThrowIfNull(pipelines);

        InstanceName = instanceName;
        ServiceType = serviceType;
        Pipelines = pipelines.ToList().AsReadOnly();
        PlanningOutcomes = (planningOutcomes ?? []).ToList().AsReadOnly();
        Failure = failure;
        Status = SyncResultStatusAggregation.From(
            Pipelines.Select(x => x.Status),
            failure is not null || PlanningOutcomes.Any(x => x is BlockingPlanningOutcome)
        );
    }

    public string InstanceName { get; }
    public SupportedServices ServiceType { get; }
    public SyncResultStatus Status { get; }
    public IReadOnlyList<PlanningOutcome> PlanningOutcomes { get; }
    public IReadOnlyList<PipelineResult> Pipelines { get; }
    public OperationalFailure? Failure { get; }
}
