using System.IO.Abstractions;
using Recyclarr.Repo;
using Recyclarr.TestLibrary;
using Recyclarr.TrashGuide.MediaNaming;

namespace Recyclarr.IntegrationTests.TrashGuide;

[TestFixture]
public class MediaNamingGuideServiceTest : IntegrationTestFixture
{
    private void SetupMetadata()
    {
        var repo = Resolve<ITrashGuidesRepo>();
        const string metadataJson =
            """
            {
              "json_paths": {
                "radarr": {
                  "naming": ["radarr/naming1", "radarr/naming2"]
                },
                "sonarr": {
                  "naming": ["sonarr/naming1", "sonarr/naming2"]
                }
              }
            }
            """;

        Fs.AddFile(repo.Path.File("metadata.json"), new MockFileData(metadataJson));
    }

    [Test]
    public void Radarr_naming()
    {
        SetupMetadata();

        var repo = Resolve<ITrashGuidesRepo>();
        var jsonPath = repo.Path.SubDirectory("radarr");
        Fs.AddSameFileFromEmbeddedResource(jsonPath.SubDirectory("naming1").File("radarr_naming1.json"), GetType());
        Fs.AddSameFileFromEmbeddedResource(jsonPath.SubDirectory("naming2").File("radarr_naming2.json"), GetType());

        var sut = Resolve<MediaNamingGuideService>();

        var result = sut.GetRadarrNamingData();
        result.Should().BeEquivalentTo(new RadarrMediaNamingData
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
        });
    }

    [Test]
    public void Sonarr_naming()
    {
        SetupMetadata();

        var repo = Resolve<ITrashGuidesRepo>();
        var jsonPath = repo.Path.SubDirectory("sonarr");
        Fs.AddSameFileFromEmbeddedResource(jsonPath.SubDirectory("naming1").File("sonarr_naming1.json"), GetType());
        Fs.AddSameFileFromEmbeddedResource(jsonPath.SubDirectory("naming2").File("sonarr_naming2.json"), GetType());

        var sut = Resolve<MediaNamingGuideService>();

        var result = sut.GetSonarrNamingData();
        result.Should().BeEquivalentTo(new SonarrMediaNamingData
        {
            Season = new Dictionary<string, string>
            {
                {"default", "season_default"}
            },
            Series = new Dictionary<string, string>
            {
                {"default", "series_default"},
                {"plex", "series_plex"},
                {"emby", "series_emby"}
            },
            Episodes = new SonarrEpisodeNamingData
            {
                Standard = new Dictionary<string, string>
                {
                    {"default:3", "episodes_standard_default_3"},
                    {"default:4", "episodes_standard_default_4"},
                    {"original", "episodes_standard_original"}
                },
                Daily = new Dictionary<string, string>
                {
                    {"default:3", "episodes_daily_default_3"},
                    {"default:4", "episodes_daily_default_4"},
                    {"original", "episodes_daily_original"}
                },
                Anime = new Dictionary<string, string>
                {
                    {"default:3", "episodes_anime_default_3"},
                    {"default:4", "episodes_anime_default_4"}
                }
            }
        });
    }
}
