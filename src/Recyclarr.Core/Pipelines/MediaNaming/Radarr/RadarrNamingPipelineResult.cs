using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaNaming.Radarr;

/// <summary>
/// The terminal status, outcomes, and calculated field changes for Radarr naming synchronization.
/// </summary>
public sealed record RadarrNamingPipelineResult : PipelineResult
{
    internal RadarrNamingPipelineResult(
        int completedFields,
        int incompleteFields,
        IReadOnlyList<RadarrNamingOutcome> outcomes,
        RadarrNamingDelta? delta
    )
        : this(DeriveStatus(completedFields, incompleteFields), null, outcomes, delta) { }

    private RadarrNamingPipelineResult(
        SyncResultStatus status,
        PipelineType? blockedBy,
        IReadOnlyList<RadarrNamingOutcome> outcomes,
        RadarrNamingDelta? delta
    )
        : base(status, blockedBy)
    {
        Outcomes = outcomes.ToList().AsReadOnly();
        Delta = delta;
    }

    public IReadOnlyList<RadarrNamingOutcome> Outcomes { get; }
    public RadarrNamingDelta? Delta { get; }

    internal override PipelineResult WithStatus(
        SyncResultStatus status,
        PipelineType? blockedBy = null
    ) => new RadarrNamingPipelineResult(status, blockedBy, Outcomes, Delta);

    private static SyncResultStatus DeriveStatus(int completedFields, int incompleteFields)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(completedFields);
        ArgumentOutOfRangeException.ThrowIfNegative(incompleteFields);

        if (incompleteFields == 0)
        {
            return SyncResultStatus.Succeeded;
        }

        return completedFields > 0 ? SyncResultStatus.Partial : SyncResultStatus.Failed;
    }
}

public abstract record RadarrNamingOutcome : PipelineOutcome;

public sealed record RadarrNamingReferenceMismatchOutcome(
    RadarrNamingFormatField Field,
    string ConfiguredKey
) : RadarrNamingOutcome;

public enum RadarrNamingFormatField
{
    StandardMovieFormat,
    MovieFolderFormat,
}

public sealed record RadarrNamingDelta : ResourceDelta
{
    public ValueDelta<bool?>? RenameMovies { get; init; }
    public ValueDelta<string?>? StandardMovieFormat { get; init; }
    public ValueDelta<string?>? MovieFolderFormat { get; init; }
}
