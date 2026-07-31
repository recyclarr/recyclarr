using Recyclarr.Config.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.QualitySize;

/// <summary>
/// The terminal status, outcomes, and calculated changes for quality-size synchronization.
/// </summary>
public sealed record QualitySizePipelineResult : PipelineResult
{
    internal QualitySizePipelineResult(
        int completedResources,
        int incompleteResources,
        IReadOnlyList<QualitySizeOutcome> outcomes,
        IReadOnlyList<QualitySizeDelta> deltas
    )
        : this(DeriveStatus(completedResources, incompleteResources), null, outcomes, deltas) { }

    private QualitySizePipelineResult(
        SyncResultStatus status,
        PipelineType? blockedBy,
        IReadOnlyList<QualitySizeOutcome> outcomes,
        IReadOnlyList<QualitySizeDelta> deltas
    )
        : base(status, blockedBy)
    {
        Outcomes = outcomes.ToList().AsReadOnly();
        Deltas = deltas.ToList().AsReadOnly();
    }

    public IReadOnlyList<QualitySizeOutcome> Outcomes { get; }
    public IReadOnlyList<QualitySizeDelta> Deltas { get; }

    internal override PipelineResult WithStatus(
        SyncResultStatus status,
        PipelineType? blockedBy = null
    ) => new QualitySizePipelineResult(status, blockedBy, Outcomes, Deltas);

    private static SyncResultStatus DeriveStatus(int completedResources, int incompleteResources)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(completedResources);
        ArgumentOutOfRangeException.ThrowIfNegative(incompleteResources);

        if (incompleteResources == 0)
        {
            return SyncResultStatus.Succeeded;
        }

        return completedResources > 0 ? SyncResultStatus.Partial : SyncResultStatus.Failed;
    }
}

public abstract record QualitySizeOutcome : PipelineOutcome;

public sealed record QualitySizeDefinitionReferenceMismatchOutcome(string Type)
    : QualitySizeOutcome;

public sealed record QualitySizeReferenceMismatchOutcome(string Quality, string Type)
    : QualitySizeOutcome;

public sealed record QualitySizeServiceQualityNotFoundOutcome(string Quality) : QualitySizeOutcome;

public sealed record QualitySizePreferredRatioClampedOutcome(ValueDelta<decimal> Value)
    : QualitySizeOutcome;

public sealed record QualitySizeMinimumGreaterThanPreferredOutcome(
    string Quality,
    QualitySizeValue Minimum,
    QualitySizeValue Preferred
) : QualitySizeOutcome;

public sealed record QualitySizeUnlimitedPreferredGreaterThanMaximumOutcome(
    string Quality,
    QualitySizeValue Preferred,
    QualitySizeValue Maximum
) : QualitySizeOutcome;

public sealed record QualitySizePreferredGreaterThanMaximumOutcome(
    string Quality,
    QualitySizeValue Preferred,
    QualitySizeValue Maximum
) : QualitySizeOutcome;

public sealed record QualitySizeDelta : ResourceDelta
{
    public QualitySizeDelta(string quality, IReadOnlyList<QualitySizeUpdateComponent> components)
    {
        Quality = quality;
        Components = components.ToList().AsReadOnly();
    }

    public string Quality { get; }
    public IReadOnlyList<QualitySizeUpdateComponent> Components { get; }
}

public abstract record QualitySizeUpdateComponent;

public sealed record QualitySizeMinimumChanged(ValueDelta<QualitySizeValue> Value)
    : QualitySizeUpdateComponent;

public sealed record QualitySizePreferredChanged(ValueDelta<QualitySizeValue> Value)
    : QualitySizeUpdateComponent;

public sealed record QualitySizeMaximumChanged(ValueDelta<QualitySizeValue> Value)
    : QualitySizeUpdateComponent;
