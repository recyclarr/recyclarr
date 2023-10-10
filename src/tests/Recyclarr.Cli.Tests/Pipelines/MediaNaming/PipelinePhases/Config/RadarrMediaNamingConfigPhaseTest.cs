using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.TrashGuide.MediaNaming;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming.PipelinePhases.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class RadarrMediaNamingConfigPhaseTest
{
    private static readonly RadarrMediaNamingData RadarrNamingData = new()
    {
        Folder = new Dictionary<string, string>
        {
            {"default", "folder_default"},
            {"plex", "folder_plex"},
            {"emby", "folder_emby"}
        },
        File = new Dictionary<string, string>
        {
            {"default", "file_default"},
            {"emby", "file_emby"},
            {"jellyfin", "file_jellyfin"}
        }
    };

    [Test, AutoMockData]
    public async Task Radarr_naming(
        [Frozen] IMediaNamingGuideService guide,
        RadarrMediaNamingConfigPhase sut)
    {
        guide.GetRadarrNamingData().Returns(RadarrNamingData);

        var config = new RadarrConfiguration
        {
            InstanceName = "radarr",
            MediaNaming = new RadarrMediaNamingConfig
            {
                Folder = "plex",
                Movie = new RadarrMovieNamingConfig
                {
                    Rename = true,
                    Standard = "emby"
                }
            }
        };

        var result = await sut.ProcessNaming(config, guide, new NamingFormatLookup());

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new RadarrMediaNamingDto
            {
                RenameMovies = true,
                StandardMovieFormat = "file_emby",
                MovieFolderFormat = "folder_plex"
            },
            o => o.RespectingRuntimeTypes());
    }
}
