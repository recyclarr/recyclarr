using AutoFixture;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.TrashGuide.MediaNaming;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming.Config;

internal sealed class RadarrMediaNamingConfigPhaseTest
{
    private static readonly RadarrMediaNamingData RadarrNamingData = new()
    {
        Folder = new Dictionary<string, string>
        {
            { "default", "folder_default" },
            { "plex", "folder_plex" },
            { "emby", "folder_emby" },
        },
        File = new Dictionary<string, string>
        {
            { "default", "file_default" },
            { "emby", "file_emby" },
            { "jellyfin", "file_jellyfin" },
        },
    };

    [Test]
    public async Task Radarr_naming()
    {
        var fixture = NSubstituteFixture.Create();

        var guide = fixture.Freeze<IMediaNamingGuideService>();
        guide.GetRadarrNamingData().Returns(RadarrNamingData);

        fixture.Inject(
            new RadarrConfiguration
            {
                InstanceName = "radarr",
                MediaNaming = new RadarrMediaNamingConfig
                {
                    Folder = "plex",
                    Movie = new RadarrMovieNamingConfig { Rename = true, Standard = "emby" },
                },
            }
        );

        var sut = fixture.Create<RadarrMediaNamingConfigPhase>();
        var result = await sut.ProcessNaming(guide, new NamingFormatLookup());

        result.Should().NotBeNull();
        result
            .Should()
            .BeEquivalentTo(
                new RadarrMediaNamingDto
                {
                    RenameMovies = true,
                    StandardMovieFormat = "file_emby",
                    MovieFolderFormat = "folder_plex",
                },
                o => o.PreferringRuntimeMemberTypes()
            );
    }
}
