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
        IReadOnlyList<CustomFormatDelta> deltas
    )
        : this(
            DeriveStatus(completedResources, incompleteResources),
            null,
            completedResources,
            incompleteResources,
            outcomes,
            deltas
        ) { }

    private CustomFormatPipelineResult(
        SyncResultStatus status,
        PipelineType? blockedBy,
        int completedResources,
        int incompleteResources,
        IReadOnlyList<CustomFormatOutcome> outcomes,
        IReadOnlyList<CustomFormatDelta> deltas
    )
        : base(status, blockedBy)
    {
        CompletedResources = completedResources;
        IncompleteResources = incompleteResources;
        Outcomes = outcomes;
        Deltas = deltas;
    }

    public IReadOnlyList<CustomFormatOutcome> Outcomes { get; }
    public IReadOnlyList<CustomFormatDelta> Deltas { get; }
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
            Deltas
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

public sealed record CustomFormatReferenceMismatchOutcome(string TrashId) : CustomFormatOutcome;

public sealed record CustomFormatGroupReferenceMismatchOutcome(string TrashId)
    : CustomFormatOutcome;

public sealed record IncompatibleCustomFormatGroupOutcome(string Name, string TrashId)
    : CustomFormatOutcome;

public sealed record EmptyCustomFormatGroupOutcome(string Name, string TrashId)
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

public abstract record CustomFormatDelta : ResourceDelta
{
    public required CustomFormatIdentity Identity { get; init; }
}

public sealed record CustomFormatCreateDelta : CustomFormatDelta
{
    public required CustomFormatSourceInfo SelectionProvenance { get; init; }
}

public sealed record CustomFormatUpdateDelta : CustomFormatDelta
{
    public required CustomFormatSourceInfo SelectionProvenance { get; init; }
    public required IReadOnlyList<CustomFormatUpdateComponent> Components { get; init; }
}

public sealed record CustomFormatDeleteDelta : CustomFormatDelta;

public abstract record CustomFormatUpdateComponent;

public sealed record CustomFormatNameChanged(ValueDelta<string> Value)
    : CustomFormatUpdateComponent;

public sealed record CustomFormatIncludeWhenRenamingChanged(ValueDelta<bool> Value)
    : CustomFormatUpdateComponent;

public sealed record CustomFormatSpecificationAdded(string Name) : CustomFormatUpdateComponent;

public sealed record CustomFormatSpecificationChanged(string Name) : CustomFormatUpdateComponent;

public sealed record CustomFormatSpecificationRemoved(string Name) : CustomFormatUpdateComponent;
