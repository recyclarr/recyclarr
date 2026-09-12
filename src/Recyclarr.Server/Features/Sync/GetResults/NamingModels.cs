namespace Recyclarr.Server.Features.Sync.GetResults;

internal enum SonarrNamingField
{
    SeriesFolderFormat,
    SeasonFolderFormat,
    StandardEpisodeFormat,
    DailyEpisodeFormat,
    AnimeEpisodeFormat,
}

internal enum RadarrNamingField
{
    StandardMovieFormat,
    MovieFolderFormat,
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record NamingReferenceMismatchResponse<TField>(TField Field, string ConfiguredKey);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SonarrNamingUpdateResponse
{
    public ValueChangeResponse<bool?>? RenameEpisodes { get; init; }
    public ValueChangeResponse<string?>? SeriesFolderFormat { get; init; }
    public ValueChangeResponse<string?>? SeasonFolderFormat { get; init; }
    public ValueChangeResponse<string?>? StandardEpisodeFormat { get; init; }
    public ValueChangeResponse<string?>? DailyEpisodeFormat { get; init; }
    public ValueChangeResponse<string?>? AnimeEpisodeFormat { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record RadarrNamingUpdateResponse
{
    public ValueChangeResponse<bool?>? RenameMovies { get; init; }
    public ValueChangeResponse<string?>? StandardMovieFormat { get; init; }
    public ValueChangeResponse<string?>? MovieFolderFormat { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SonarrNamingOutcomesResponse(
    IReadOnlyList<NamingReferenceMismatchResponse<SonarrNamingField>> ReferenceMismatches
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record RadarrNamingOutcomesResponse(
    IReadOnlyList<NamingReferenceMismatchResponse<RadarrNamingField>> ReferenceMismatches
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SonarrNamingPipelineResponse(
    PipelineStatus Status,
    SonarrNamingOutcomesResponse Outcomes,
    IReadOnlyList<SonarrNamingUpdateResponse> Updates
)
{
    public BlockingPipeline? BlockedBy { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record RadarrNamingPipelineResponse(
    PipelineStatus Status,
    RadarrNamingOutcomesResponse Outcomes,
    IReadOnlyList<RadarrNamingUpdateResponse> Updates
)
{
    public BlockingPipeline? BlockedBy { get; init; }
}
