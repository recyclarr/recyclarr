using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaNaming.Sonarr;

/// <summary>
/// The terminal status, outcomes, and calculated field changes for Sonarr naming synchronization.
/// </summary>
public sealed record SonarrNamingPipelineResult : PipelineResult
{
    internal SonarrNamingPipelineResult(
        int completedFields,
        int incompleteFields,
        IReadOnlyList<SonarrNamingOutcome> outcomes,
        PlannedSonarrNamingResource? settings
    )
        : this(DeriveStatus(completedFields, incompleteFields), null, outcomes, settings) { }

    private SonarrNamingPipelineResult(
        SyncResultStatus status,
        PipelineType? blockedBy,
        IReadOnlyList<SonarrNamingOutcome> outcomes,
        PlannedSonarrNamingResource? settings
    )
        : base(status, blockedBy)
    {
        Outcomes = outcomes;
        Settings = settings;
    }

    public IReadOnlyList<SonarrNamingOutcome> Outcomes { get; }

    /// <summary>Absent when no naming field is configured or the pipeline did not compute.</summary>
    public PlannedSonarrNamingResource? Settings { get; }

    internal override PipelineResult WithStatus(
        SyncResultStatus status,
        PipelineType? blockedBy = null
    ) => new SonarrNamingPipelineResult(status, blockedBy, Outcomes, Settings);

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

public abstract record SonarrNamingOutcome : PipelineOutcome;

public sealed record SonarrNamingReferenceMismatchOutcome(
    SonarrNamingFormatField Field,
    string ConfiguredKey
) : SonarrNamingOutcome;

public enum SonarrNamingFormatField
{
    SeriesFolderFormat,
    SeasonFolderFormat,
    StandardEpisodeFormat,
    DailyEpisodeFormat,
    AnimeEpisodeFormat,
}

/// <summary>The naming settings the sync manages; a null field is not configured.</summary>
public sealed record PlannedSonarrNamingResource : PlannedResource
{
    public PlannedValue<bool?>? RenameEpisodes { get; init; }
    public PlannedValue<string?>? SeriesFolderFormat { get; init; }
    public PlannedValue<string?>? SeasonFolderFormat { get; init; }
    public PlannedValue<string?>? StandardEpisodeFormat { get; init; }
    public PlannedValue<string?>? DailyEpisodeFormat { get; init; }
    public PlannedValue<string?>? AnimeEpisodeFormat { get; init; }
}
