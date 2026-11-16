using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Server.Features.Sync.GetResults;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync.Results;

internal static class NamingResponseMapper
{
    public static SonarrNamingPipelineResponse ToResponse(SonarrNamingPipelineResult result)
    {
        return new SonarrNamingPipelineResponse
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Outcomes = result.Outcomes.Select(OutcomeResponseMapper.Map).ToList(),
            Settings = result.Settings is { } x ? MapSonarrSettings(x) : null,
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };
    }

    public static RadarrNamingPipelineResponse ToResponse(RadarrNamingPipelineResult result)
    {
        return new RadarrNamingPipelineResponse
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Outcomes = result.Outcomes.Select(OutcomeResponseMapper.Map).ToList(),
            Settings = result.Settings is { } x ? MapRadarrSettings(x) : null,
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };
    }

    private static SonarrNamingSettingsResponse MapSonarrSettings(
        PlannedSonarrNamingResource settings
    ) =>
        new()
        {
            Action = ResultValueMapper.MapAction(settings.Action),
            RenameEpisodes = MapConfigured(settings.RenameEpisodes),
            SeriesFolderFormat = MapConfigured(settings.SeriesFolderFormat),
            SeasonFolderFormat = MapConfigured(settings.SeasonFolderFormat),
            StandardEpisodeFormat = MapConfigured(settings.StandardEpisodeFormat),
            DailyEpisodeFormat = MapConfigured(settings.DailyEpisodeFormat),
            AnimeEpisodeFormat = MapConfigured(settings.AnimeEpisodeFormat),
        };

    private static RadarrNamingSettingsResponse MapRadarrSettings(
        PlannedRadarrNamingResource settings
    ) =>
        new()
        {
            Action = ResultValueMapper.MapAction(settings.Action),
            RenameMovies = MapConfigured(settings.RenameMovies),
            StandardMovieFormat = MapConfigured(settings.StandardMovieFormat),
            MovieFolderFormat = MapConfigured(settings.MovieFolderFormat),
        };

    private static PlannedValueResponse<T>? MapConfigured<T>(PlannedValue<T>? value) =>
        value is null ? null : ResultValueMapper.MapValue(value);
}
