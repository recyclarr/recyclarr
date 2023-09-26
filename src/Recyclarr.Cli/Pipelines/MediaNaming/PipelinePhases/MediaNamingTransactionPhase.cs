using System.Diagnostics.CodeAnalysis;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingTransactionPhase
{
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public MediaNamingDto Execute(MediaNamingDto serviceData, ProcessedNamingConfig config)
    {
        return serviceData switch
        {
            RadarrMediaNamingDto dto => UpdateRadarrDto(dto, config),
            SonarrMediaNamingDto dto => UpdateSonarrDto(dto, config),
            _ => throw new ArgumentException("Config type not supported in media naming transation phase")
        };
    }

    private static RadarrMediaNamingDto UpdateRadarrDto(RadarrMediaNamingDto serviceDto, ProcessedNamingConfig config)
    {
        var configDto = (RadarrMediaNamingDto) config.Dto;
        var combinedDto = serviceDto with
        {
            RenameMovies = configDto.RenameMovies,
            MovieFolderFormat = configDto.MovieFolderFormat
        };

        if (configDto.RenameMovies is true)
        {
            combinedDto.StandardMovieFormat = configDto.StandardMovieFormat;
        }

        return combinedDto;
    }

    private static SonarrMediaNamingDto UpdateSonarrDto(SonarrMediaNamingDto serviceDto, ProcessedNamingConfig config)
    {
        var configDto = (SonarrMediaNamingDto) config.Dto;
        var combinedDto = serviceDto with
        {
            RenameEpisodes = configDto.RenameEpisodes,
            SeriesFolderFormat = configDto.SeriesFolderFormat,
            SeasonFolderFormat = configDto.SeasonFolderFormat
        };

        if (configDto.RenameEpisodes is true)
        {
            combinedDto.StandardEpisodeFormat = configDto.StandardEpisodeFormat;
            combinedDto.DailyEpisodeFormat = configDto.DailyEpisodeFormat;
            combinedDto.AnimeEpisodeFormat = configDto.AnimeEpisodeFormat;
        }

        return combinedDto;
    }
}
