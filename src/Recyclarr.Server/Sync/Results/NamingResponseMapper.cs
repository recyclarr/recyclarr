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
        var outcomes = result
            .Outcomes.OfType<SonarrNamingReferenceMismatchOutcome>()
            .Select(x => MapSonarrMismatch(x))
            .ToList();
        ResultValueMapper.EnsureAllMapped(
            "Sonarr naming outcome",
            result.Outcomes.Count,
            outcomes.Count
        );

        return new SonarrNamingPipelineResponse(
            ResultValueMapper.MapStatus(result.Status),
            new SonarrNamingOutcomesResponse(outcomes),
            result.Delta is null ? [] : [MapSonarrUpdate(result.Delta)]
        )
        {
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };
    }

    public static RadarrNamingPipelineResponse ToResponse(RadarrNamingPipelineResult result)
    {
        var outcomes = result
            .Outcomes.OfType<RadarrNamingReferenceMismatchOutcome>()
            .Select(x => MapRadarrMismatch(x))
            .ToList();
        ResultValueMapper.EnsureAllMapped(
            "Radarr naming outcome",
            result.Outcomes.Count,
            outcomes.Count
        );

        return new RadarrNamingPipelineResponse(
            ResultValueMapper.MapStatus(result.Status),
            new RadarrNamingOutcomesResponse(outcomes),
            result.Delta is null ? [] : [MapRadarrUpdate(result.Delta)]
        )
        {
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

    private static partial NamingReferenceMismatchResponse<SonarrNamingField> MapSonarrMismatch(
        SonarrNamingReferenceMismatchOutcome source
    );

    private static partial NamingReferenceMismatchResponse<RadarrNamingField> MapRadarrMismatch(
        RadarrNamingReferenceMismatchOutcome source
    );
}
