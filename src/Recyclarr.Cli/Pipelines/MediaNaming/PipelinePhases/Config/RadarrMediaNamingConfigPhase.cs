using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;

internal class RadarrMediaNamingConfigPhase(RadarrConfiguration config)
    : IServiceBasedMediaNamingConfigPhase
{
    public Task<MediaNamingDto> ProcessNaming(
        MediaNamingResourceQuery guide,
        NamingFormatLookup lookup
    )
    {
        var guideData = guide.GetRadarr();
        var configData = config.MediaNaming;

        return Task.FromResult<MediaNamingDto>(
            new RadarrMediaNamingDto
            {
                StandardMovieFormat = lookup.ObtainFormat(
                    guideData.File,
                    configData.Movie?.Standard,
                    "Standard Movie Format"
                ),
                MovieFolderFormat = lookup.ObtainFormat(
                    guideData.Folder,
                    configData.Folder,
                    "Movie Folder Format"
                ),
                RenameMovies = configData.Movie?.Rename,
            }
        );
    }
}
