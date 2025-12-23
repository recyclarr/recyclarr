using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

internal class MediaNamingTransactionPhase : IPipelinePhase<MediaNamingPipelineContext>
{
    public Task<PipelineFlow> Execute(MediaNamingPipelineContext context, CancellationToken ct)
    {
        var planned = context.Plan.MediaNaming;
        context.TransactionOutput = context.ApiFetchOutput switch
        {
            RadarrMediaNamingDto dto => UpdateRadarrDto(dto, planned),
            SonarrMediaNamingDto dto => UpdateSonarrDto(dto, planned),
            _ => throw new ArgumentException(
                "Config type not supported in media naming transaction phase"
            ),
        };

        return Task.FromResult(PipelineFlow.Continue);
    }

    private static RadarrMediaNamingDto UpdateRadarrDto(
        RadarrMediaNamingDto serviceDto,
        PlannedMediaNaming planned
    )
    {
        var configDto = (RadarrMediaNamingDto)planned.Dto;
        return serviceDto with
        {
            RenameMovies = configDto.RenameMovies,
            MovieFolderFormat = configDto.MovieFolderFormat,
            StandardMovieFormat = configDto.StandardMovieFormat,
        };
    }

    private static SonarrMediaNamingDto UpdateSonarrDto(
        SonarrMediaNamingDto serviceDto,
        PlannedMediaNaming planned
    )
    {
        var configDto = (SonarrMediaNamingDto)planned.Dto;
        return serviceDto with
        {
            RenameEpisodes = configDto.RenameEpisodes,
            SeriesFolderFormat = configDto.SeriesFolderFormat,
            SeasonFolderFormat = configDto.SeasonFolderFormat,
            StandardEpisodeFormat = configDto.StandardEpisodeFormat,
            DailyEpisodeFormat = configDto.DailyEpisodeFormat,
            AnimeEpisodeFormat = configDto.AnimeEpisodeFormat,
        };
    }
}
