using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Server.Features.Sync.GetResults;
using Riok.Mapperly.Abstractions;

namespace Recyclarr.Server.Sync.Results;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class NamingResponseMapper
{
    public static SonarrNamingPipelineResponse ToResponse(SonarrNamingPipelineResult result)
    {
        return new SonarrNamingPipelineResponse
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Outcomes = result.Outcomes.Select(OutcomeResponseMapper.Map).ToList(),
            Updates = result.Delta is null ? [] : [MapSonarrUpdate(result.Delta)],
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };
    }

    public static RadarrNamingPipelineResponse ToResponse(RadarrNamingPipelineResult result)
    {
        return new RadarrNamingPipelineResponse
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Outcomes = result.Outcomes.Select(OutcomeResponseMapper.Map).ToList(),
            Updates = result.Delta is null ? [] : [MapRadarrUpdate(result.Delta)],
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };
    }

    private static SonarrNamingUpdateResponse MapSonarrUpdate(SonarrNamingDelta delta) =>
        new()
        {
            RenameEpisodes = MapNullableValue(delta.RenameEpisodes),
            SeriesFolderFormat = MapNullableValue(delta.SeriesFolderFormat),
            SeasonFolderFormat = MapNullableValue(delta.SeasonFolderFormat),
            StandardEpisodeFormat = MapNullableValue(delta.StandardEpisodeFormat),
            DailyEpisodeFormat = MapNullableValue(delta.DailyEpisodeFormat),
            AnimeEpisodeFormat = MapNullableValue(delta.AnimeEpisodeFormat),
        };

    private static RadarrNamingUpdateResponse MapRadarrUpdate(RadarrNamingDelta delta) =>
        new()
        {
            RenameMovies = MapNullableValue(delta.RenameMovies),
            StandardMovieFormat = MapNullableValue(delta.StandardMovieFormat),
            MovieFolderFormat = MapNullableValue(delta.MovieFolderFormat),
        };

    private static ValueChangeResponse<T>? MapNullableValue<T>(
        Recyclarr.Sync.Results.ValueDelta<T>? value
    ) => value is null ? null : ResultValueMapper.MapValue(value);
}
