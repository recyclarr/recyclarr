using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.TrashGuide.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;

public class RadarrMediaNamingConfigPhase : ServiceBasedMediaNamingConfigPhase<RadarrConfiguration>
{
    protected override Task<MediaNamingDto> ProcessNaming(
        RadarrConfiguration config,
        IMediaNamingGuideService guide,
        NamingFormatLookup lookup)
    {
        var guideData = guide.GetRadarrNamingData();
        var configData = config.MediaNaming;

        return Task.FromResult<MediaNamingDto>(new RadarrMediaNamingDto
        {
            StandardMovieFormat =
                lookup.ObtainFormat(guideData.File, configData.Movie?.Standard, "Standard Movie Format"),
            MovieFolderFormat = lookup.ObtainFormat(guideData.Folder, configData.Folder, "Movie Folder Format"),
            RenameMovies = configData.Movie?.Rename
        });
    }
}
