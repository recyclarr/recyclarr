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
        IReadOnlyList<PlannedQualitySizeResource> resources
    )
        : this(DeriveStatus(completedResources, incompleteResources), null, outcomes, resources) { }

    private QualitySizePipelineResult(
        SyncResultStatus status,
        PipelineType? blockedBy,
        IReadOnlyList<QualitySizeOutcome> outcomes,
        IReadOnlyList<PlannedQualitySizeResource> resources
    )
        : base(status, blockedBy)
    {
        Outcomes = outcomes;
        Resources = resources;
    }

    public IReadOnlyList<QualitySizeOutcome> Outcomes { get; }

    /// <summary>Every planned quality the service has.</summary>
    public IReadOnlyList<PlannedQualitySizeResource> Resources { get; }

    internal override PipelineResult WithStatus(
        SyncResultStatus status,
        PipelineType? blockedBy = null
    ) => new QualitySizePipelineResult(status, blockedBy, Outcomes, Resources);

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

/// <summary>
/// A guide quality profile links a quality size file (by trash_id) that the guide does not
/// contain. Only reported for services that store sizes per quality profile.
/// </summary>
public sealed record QualitySizeGuideReferenceMismatchOutcome(string Label, string TrashId)
    : QualitySizeOutcome;

/// <summary>
/// A guide quality profile links more than one quality size file, so no size source can be chosen.
/// Only reported for services that store sizes per quality profile.
/// </summary>
public sealed record QualitySizeAmbiguousGuideReferenceOutcome(IReadOnlyList<string> Labels)
    : QualitySizeOutcome;

public sealed record QualitySizePreferredRatioClampedOutcome(decimal Configured, decimal Used)
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

/// <summary>The size limits the sync manages on one quality, in MB per minute.</summary>
public sealed record PlannedQualitySizeResource : PlannedResource
{
    public required string Quality { get; init; }
    public required PlannedValue<QualitySizeValue> Minimum { get; init; }
    public required PlannedValue<QualitySizeValue> Preferred { get; init; }
    public required PlannedValue<QualitySizeValue> Maximum { get; init; }

    public static PlannedQualitySizeResource Created(
        string quality,
        QualitySizeValue minimum,
        QualitySizeValue preferred,
        QualitySizeValue maximum
    ) =>
        new()
        {
            Action = PlannedAction.Create,
            Quality = quality,
            Minimum = PlannedValue.Created(minimum),
            Preferred = PlannedValue.Created(preferred),
            Maximum = PlannedValue.Created(maximum),
        };

    public static PlannedQualitySizeResource Compared(
        string quality,
        (QualitySizeValue Current, QualitySizeValue Desired) minimum,
        (QualitySizeValue Current, QualitySizeValue Desired) preferred,
        (QualitySizeValue Current, QualitySizeValue Desired) maximum
    )
    {
        var min = PlannedValue.Compared(minimum.Current, minimum.Desired);
        var pref = PlannedValue.Compared(preferred.Current, preferred.Desired);
        var max = PlannedValue.Compared(maximum.Current, maximum.Desired);
        return new PlannedQualitySizeResource
        {
            Action =
                min.Changed || pref.Changed || max.Changed
                    ? PlannedAction.Update
                    : PlannedAction.Unchanged,
            Quality = quality,
            Minimum = min,
            Preferred = pref,
            Maximum = max,
        };
    }
}
