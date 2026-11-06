namespace Recyclarr.Server.Features.Sync.GetResults;

internal sealed record SonarrNamingUpdateResponse
{
    public ValueChangeResponse<bool?>? RenameEpisodes { get; init; }
    public ValueChangeResponse<string?>? SeriesFolderFormat { get; init; }
    public ValueChangeResponse<string?>? SeasonFolderFormat { get; init; }
    public ValueChangeResponse<string?>? StandardEpisodeFormat { get; init; }
    public ValueChangeResponse<string?>? DailyEpisodeFormat { get; init; }
    public ValueChangeResponse<string?>? AnimeEpisodeFormat { get; init; }
}

internal sealed record RadarrNamingUpdateResponse
{
    public ValueChangeResponse<bool?>? RenameMovies { get; init; }
    public ValueChangeResponse<string?>? StandardMovieFormat { get; init; }
    public ValueChangeResponse<string?>? MovieFolderFormat { get; init; }
}

internal sealed record SonarrNamingPipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required IReadOnlyList<OutcomeResponse> Outcomes { get; init; }
    public required IReadOnlyList<SonarrNamingUpdateResponse> Updates { get; init; }

    /// <summary>Present only when the status is `blocked`.</summary>
    public BlockingPipeline? BlockedBy { get; init; }
}

internal sealed record RadarrNamingPipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required IReadOnlyList<OutcomeResponse> Outcomes { get; init; }
    public required IReadOnlyList<RadarrNamingUpdateResponse> Updates { get; init; }

    /// <summary>Present only when the status is `blocked`.</summary>
    public BlockingPipeline? BlockedBy { get; init; }
}
