using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.CustomFormat;

/// <summary>
/// The managed identity of one Custom Format.
/// </summary>
public sealed record CustomFormatIdentity(string TrashId, string Name);

/// <summary>
/// The terminal semantic result of Custom Format synchronization.
/// </summary>
public sealed record CustomFormatPipelineResult : PipelineResult
{
    internal CustomFormatPipelineResult(
        int completedResources,
        int incompleteResources,
        IReadOnlyList<CustomFormatOutcome> outcomes,
        IReadOnlyList<PlannedCustomFormatResource> resources,
        IReadOnlyList<CustomFormatIdentity> deletes
    )
        : this(
            DeriveStatus(completedResources, incompleteResources),
            null,
            completedResources,
            incompleteResources,
            outcomes,
            resources,
            deletes
        ) { }

    private CustomFormatPipelineResult(
        SyncResultStatus status,
        PipelineType? blockedBy,
        int completedResources,
        int incompleteResources,
        IReadOnlyList<CustomFormatOutcome> outcomes,
        IReadOnlyList<PlannedCustomFormatResource> resources,
        IReadOnlyList<CustomFormatIdentity> deletes
    )
        : base(status, blockedBy)
    {
        CompletedResources = completedResources;
        IncompleteResources = incompleteResources;
        Outcomes = outcomes;
        Resources = resources;
        Deletes = deletes;
    }

    public IReadOnlyList<CustomFormatOutcome> Outcomes { get; }
    public IReadOnlyList<PlannedCustomFormatResource> Resources { get; }

    /// <summary>
    /// Managed custom formats removed from the configuration, present only when
    /// <c>delete_old_custom_formats</c> is enabled.
    /// </summary>
    public IReadOnlyList<CustomFormatIdentity> Deletes { get; }
    internal int CompletedResources { get; }
    internal int IncompleteResources { get; }

    internal override PipelineResult WithStatus(
        SyncResultStatus status,
        PipelineType? blockedBy = null
    ) =>
        new CustomFormatPipelineResult(
            status,
            blockedBy,
            CompletedResources,
            IncompleteResources,
            Outcomes,
            Resources,
            Deletes
        );

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

public abstract record CustomFormatOutcome : PipelineOutcome;

public sealed record IncompatibleCustomFormatGroupOutcome(string Name, string TrashId)
    : CustomFormatOutcome;

public sealed record EmptyCustomFormatGroupOutcome(string Name, string TrashId)
    : CustomFormatOutcome;

public sealed record CustomFormatDuplicateNameOutcome(string Name, IReadOnlyList<string> TrashIds)
    : CustomFormatOutcome;

public sealed record CustomFormatAdoptedOutcome(CustomFormatIdentity Identity, int ServiceId)
    : CustomFormatOutcome;

public sealed record CustomFormatServiceMatch(string Name, int ServiceId);

public sealed record CustomFormatAmbiguousMatchOutcome(
    CustomFormatIdentity Identity,
    IReadOnlyList<CustomFormatServiceMatch> ServiceMatches
) : CustomFormatOutcome;

public sealed record CustomFormatStateConflictOutcome(
    CustomFormatIdentity Identity,
    CustomFormatIdentity ManagedIdentity,
    int ServiceId
) : CustomFormatOutcome;

public sealed record CustomFormatCreateRejectedOutcome(CustomFormatIdentity Identity)
    : CustomFormatOutcome;

public sealed record CustomFormatUpdateRejectedOutcome(CustomFormatIdentity Identity)
    : CustomFormatOutcome;

public sealed record CustomFormatDeleteRejectedOutcome(CustomFormatIdentity Identity)
    : CustomFormatOutcome;

/// <summary>
/// One configured custom format and the values the sync manages on it. A format skipped by an
/// outcome (duplicate name, ambiguous match) has no entry.
/// </summary>
public sealed record PlannedCustomFormatResource : PlannedResource
{
    public required CustomFormatIdentity Identity { get; init; }
    public required CustomFormatSourceInfo SelectionProvenance { get; init; }
    public required PlannedValue<string> Name { get; init; }
    public required PlannedValue<bool> IncludeWhenRenaming { get; init; }

    /// <summary>
    /// Every condition of the guide definition, plus conditions the sync removes from the service.
    /// </summary>
    public required IReadOnlyList<CustomFormatSpecificationResult> Specifications { get; init; }
}

public enum CustomFormatSpecificationChange
{
    Added,
    Changed,
    Removed,
    Unchanged,
}

public sealed record CustomFormatSpecificationResult(
    string Name,
    CustomFormatSpecificationChange Change
);
