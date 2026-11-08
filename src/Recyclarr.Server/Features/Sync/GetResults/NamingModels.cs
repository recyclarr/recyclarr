namespace Recyclarr.Server.Features.Sync.GetResults;

/// <summary>The naming settings Recyclarr manages. A field that is not configured is absent.</summary>
internal sealed record SonarrNamingSettingsResponse
{
    public required ResourceAction Action { get; init; }
    public PlannedValueResponse<bool?>? RenameEpisodes { get; init; }
    public PlannedValueResponse<string?>? SeriesFolderFormat { get; init; }
    public PlannedValueResponse<string?>? SeasonFolderFormat { get; init; }
    public PlannedValueResponse<string?>? StandardEpisodeFormat { get; init; }
    public PlannedValueResponse<string?>? DailyEpisodeFormat { get; init; }
    public PlannedValueResponse<string?>? AnimeEpisodeFormat { get; init; }
}

/// <summary>The naming settings Recyclarr manages. A field that is not configured is absent.</summary>
internal sealed record RadarrNamingSettingsResponse
{
    public required ResourceAction Action { get; init; }
    public PlannedValueResponse<bool?>? RenameMovies { get; init; }
    public PlannedValueResponse<string?>? StandardMovieFormat { get; init; }
    public PlannedValueResponse<string?>? MovieFolderFormat { get; init; }
}

internal sealed record SonarrNamingPipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required IReadOnlyList<OutcomeResponse> Outcomes { get; init; }

    /// <summary>Absent when no naming field is usable or the pipeline did not run.</summary>
    public SonarrNamingSettingsResponse? Settings { get; init; }

    /// <summary>Present only when the status is `blocked`.</summary>
    public BlockingPipeline? BlockedBy { get; init; }
}

internal sealed record RadarrNamingPipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required IReadOnlyList<OutcomeResponse> Outcomes { get; init; }

    /// <summary>Absent when no naming field is usable or the pipeline did not run.</summary>
    public RadarrNamingSettingsResponse? Settings { get; init; }

    /// <summary>Present only when the status is `blocked`.</summary>
    public BlockingPipeline? BlockedBy { get; init; }
}
