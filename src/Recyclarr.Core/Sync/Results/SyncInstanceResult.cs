using Recyclarr.TrashGuide;

namespace Recyclarr.Sync.Results;

/// <summary>
/// The ordered terminal pipeline results and optional operational failure for one service instance.
/// </summary>
public sealed record SyncInstanceResult
{
    public SyncInstanceResult(
        string instanceName,
        SupportedServices serviceType,
        IReadOnlyList<PipelineResult> pipelines,
        OperationalFailure? failure = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceName);
        ArgumentNullException.ThrowIfNull(pipelines);

        InstanceName = instanceName;
        ServiceType = serviceType;
        Pipelines = pipelines.ToList().AsReadOnly();
        Failure = failure;
        Status = SyncResultStatusAggregation.From(
            Pipelines.Select(x => x.Status),
            failure is not null
        );
    }

    public string InstanceName { get; }
    public SupportedServices ServiceType { get; }
    public SyncResultStatus Status { get; }
    public IReadOnlyList<PipelineResult> Pipelines { get; }
    public OperationalFailure? Failure { get; }
}
