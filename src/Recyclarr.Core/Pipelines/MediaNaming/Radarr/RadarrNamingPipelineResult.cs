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
        PlannedRadarrNamingResource? settings
    )
        : this(DeriveStatus(completedFields, incompleteFields), null, outcomes, settings) { }

    private RadarrNamingPipelineResult(
        SyncResultStatus status,
        PipelineType? blockedBy,
        IReadOnlyList<RadarrNamingOutcome> outcomes,
        PlannedRadarrNamingResource? settings
    )
        : base(status, blockedBy)
    {
        Outcomes = outcomes;
        Settings = settings;
    }

    public IReadOnlyList<RadarrNamingOutcome> Outcomes { get; }

    /// <summary>Absent when no naming field is configured or the pipeline did not compute.</summary>
    public PlannedRadarrNamingResource? Settings { get; }

    internal override PipelineResult WithStatus(
        SyncResultStatus status,
        PipelineType? blockedBy = null
    ) => new RadarrNamingPipelineResult(status, blockedBy, Outcomes, Settings);

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

/// <summary>The naming settings the sync manages; a null field is not configured.</summary>
public sealed record PlannedRadarrNamingResource : PlannedResource
{
    public PlannedValue<bool?>? RenameMovies { get; init; }
    public PlannedValue<string?>? StandardMovieFormat { get; init; }
    public PlannedValue<string?>? MovieFolderFormat { get; init; }
}
