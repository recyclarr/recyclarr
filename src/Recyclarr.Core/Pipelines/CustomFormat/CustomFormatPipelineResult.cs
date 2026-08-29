using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.CustomFormat;

/// <summary>
/// The managed identity of one Custom Format.
/// </summary>
public sealed record CustomFormatIdentity(string TrashId, string Name);

/// <summary>
/// The terminal semantic result of Custom Format synchronization.
/// </summary>
public sealed record CustomFormatPipelineResult : Recyclarr.Sync.Results.PipelineResult
{
    internal CustomFormatPipelineResult(
        int completedResources,
        int incompleteResources,
        IReadOnlyList<CustomFormatOutcome> outcomes,
        IReadOnlyList<CustomFormatDelta> deltas
    )
        : base(DeriveStatus(completedResources, incompleteResources))
    {
        Outcomes = outcomes.ToList().AsReadOnly();
        Deltas = deltas.ToList().AsReadOnly();
    }

    public IReadOnlyList<CustomFormatOutcome> Outcomes { get; }
    public IReadOnlyList<CustomFormatDelta> Deltas { get; }

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

public sealed record CustomFormatAmbiguousMatchOutcome : CustomFormatOutcome
{
    public CustomFormatAmbiguousMatchOutcome(
        CustomFormatIdentity identity,
        IReadOnlyList<CustomFormatServiceMatch> serviceMatches
    )
    {
        Identity = identity;
        ServiceMatches = serviceMatches.ToList().AsReadOnly();
    }

    public CustomFormatIdentity Identity { get; }
    public IReadOnlyList<CustomFormatServiceMatch> ServiceMatches { get; }
}

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

public abstract record CustomFormatDelta(CustomFormatIdentity Identity) : ResourceDelta;

public sealed record CustomFormatCreateDelta(
    CustomFormatIdentity Identity,
    CustomFormatSourceInfo SelectionProvenance
) : CustomFormatDelta(Identity);

public sealed record CustomFormatUpdateDelta : CustomFormatDelta
{
    public CustomFormatUpdateDelta(
        CustomFormatIdentity identity,
        CustomFormatSourceInfo selectionProvenance,
        IReadOnlyList<CustomFormatUpdateComponent> components
    )
        : base(identity)
    {
        SelectionProvenance = selectionProvenance;
        Components = components.ToList().AsReadOnly();
    }

    public CustomFormatSourceInfo SelectionProvenance { get; }
    public IReadOnlyList<CustomFormatUpdateComponent> Components { get; }
}

public sealed record CustomFormatDeleteDelta(CustomFormatIdentity Identity)
    : CustomFormatDelta(Identity);

public abstract record CustomFormatUpdateComponent;

public sealed record CustomFormatNameChanged(ValueDelta<string> Value)
    : CustomFormatUpdateComponent;

public sealed record CustomFormatIncludeWhenRenamingChanged(ValueDelta<bool> Value)
    : CustomFormatUpdateComponent;

public sealed record CustomFormatSpecificationAdded(string Name) : CustomFormatUpdateComponent;

public sealed record CustomFormatSpecificationChanged(string Name) : CustomFormatUpdateComponent;

public sealed record CustomFormatSpecificationRemoved(string Name) : CustomFormatUpdateComponent;
