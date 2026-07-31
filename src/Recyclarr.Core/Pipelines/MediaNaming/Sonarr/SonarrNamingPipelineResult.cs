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
        SonarrNamingDelta? delta
    )
        : this(DeriveStatus(completedFields, incompleteFields), null, outcomes, delta) { }

    private SonarrNamingPipelineResult(
        SyncResultStatus status,
        PipelineType? blockedBy,
        IReadOnlyList<SonarrNamingOutcome> outcomes,
        SonarrNamingDelta? delta
    )
        : base(status, blockedBy)
    {
        Outcomes = outcomes.ToList().AsReadOnly();
        Delta = delta;
    }

    public IReadOnlyList<SonarrNamingOutcome> Outcomes { get; }
    public SonarrNamingDelta? Delta { get; }

    internal override PipelineResult WithStatus(
        SyncResultStatus status,
        PipelineType? blockedBy = null
    ) => new SonarrNamingPipelineResult(status, blockedBy, Outcomes, Delta);

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

public sealed record SonarrNamingDelta : ResourceDelta
{
    public ValueDelta<bool?>? RenameEpisodes { get; init; }
    public ValueDelta<string?>? SeriesFolderFormat { get; init; }
    public ValueDelta<string?>? SeasonFolderFormat { get; init; }
    public ValueDelta<string?>? StandardEpisodeFormat { get; init; }
    public ValueDelta<string?>? DailyEpisodeFormat { get; init; }
    public ValueDelta<string?>? AnimeEpisodeFormat { get; init; }
}
