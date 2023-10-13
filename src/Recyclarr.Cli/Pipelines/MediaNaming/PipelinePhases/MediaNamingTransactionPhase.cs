using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingTransactionPhase : ITransactionPipelinePhase<MediaNamingPipelineContext>
{
    public void Execute(MediaNamingPipelineContext context)
    {
        context.TransactionOutput = context.ApiFetchOutput switch
        {
            RadarrMediaNamingDto dto => UpdateRadarrDto(dto, context.ConfigOutput),
            SonarrMediaNamingDto dto => UpdateSonarrDto(dto, context.ConfigOutput),
            _ => throw new ArgumentException("Config type not supported in media naming transation phase")
        };
    }

    private static RadarrMediaNamingDto UpdateRadarrDto(RadarrMediaNamingDto serviceDto, ProcessedNamingConfig config)
    {
        var configDto = (RadarrMediaNamingDto) config.Dto;
        return serviceDto with
        {
            RenameMovies = configDto.RenameMovies,
            MovieFolderFormat = configDto.MovieFolderFormat,
            StandardMovieFormat = configDto.StandardMovieFormat
        };
    }

    private static SonarrMediaNamingDto UpdateSonarrDto(SonarrMediaNamingDto serviceDto, ProcessedNamingConfig config)
    {
        var configDto = (SonarrMediaNamingDto) config.Dto;
        return serviceDto with
        {
            RenameEpisodes = configDto.RenameEpisodes,
            SeriesFolderFormat = configDto.SeriesFolderFormat,
            SeasonFolderFormat = configDto.SeasonFolderFormat,
            StandardEpisodeFormat = configDto.StandardEpisodeFormat,
            DailyEpisodeFormat = configDto.DailyEpisodeFormat,
            AnimeEpisodeFormat = configDto.AnimeEpisodeFormat
        };
    }
}
