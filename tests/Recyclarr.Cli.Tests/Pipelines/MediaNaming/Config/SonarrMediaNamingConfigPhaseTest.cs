using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.TrashGuide.MediaNaming;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrMediaNamingConfigPhaseTest
{
    private static readonly SonarrMediaNamingData SonarrNamingData = new()
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
    };

    [Test, AutoMockData]
    public async Task Sonarr_v4_naming(
        [Frozen] ISonarrCapabilityFetcher capabilities,
        [Frozen] IMediaNamingGuideService guide,
        SonarrMediaNamingConfigPhase sut)
    {
        guide.GetSonarrNamingData().Returns(SonarrNamingData);

        var config = new SonarrConfiguration
        {
            InstanceName = "sonarr",
            MediaNaming = new SonarrMediaNamingConfig
            {
                Season = "default",
                Series = "plex",
                Episodes = new SonarrEpisodeNamingConfig
                {
                    Rename = true,
                    Standard = "default",
                    Daily = "default",
                    Anime = "default"
                }
            }
        };

        var result = await sut.ProcessNaming(config, guide, new NamingFormatLookup());

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new SonarrMediaNamingDto
        {
            RenameEpisodes = true,
            SeasonFolderFormat = "season_default",
            SeriesFolderFormat = "series_plex",
            StandardEpisodeFormat = "episodes_standard_default_4",
            DailyEpisodeFormat = "episodes_daily_default_4",
            AnimeEpisodeFormat = "episodes_anime_default_4"
        });
    }

    [Test, AutoMockData]
    public async Task Sonarr_invalid_names(
        [Frozen] ISonarrCapabilityFetcher capabilities,
        [Frozen] IMediaNamingGuideService guide,
        SonarrMediaNamingConfigPhase sut)
    {
        guide.GetSonarrNamingData().Returns(SonarrNamingData);

        var config = new SonarrConfiguration
        {
            InstanceName = "sonarr",
            MediaNaming = new SonarrMediaNamingConfig
            {
                Season = "bad1",
                Series = "bad2",
                Episodes = new SonarrEpisodeNamingConfig
                {
                    Rename = true,
                    Standard = "bad3",
                    Daily = "bad4",
                    Anime = "bad5"
                }
            }
        };

        var lookup = new NamingFormatLookup();
        var result = await sut.ProcessNaming(config, guide, lookup);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new SonarrMediaNamingDto
        {
            RenameEpisodes = true
        });

        lookup.Errors.Should().BeEquivalentTo(new[]
        {
            new InvalidNamingConfig("Season Folder Format", "bad1"),
            new InvalidNamingConfig("Series Folder Format", "bad2"),
            new InvalidNamingConfig("Standard Episode Format", "bad3"),
            new InvalidNamingConfig("Daily Episode Format", "bad4"),
            new InvalidNamingConfig("Anime Episode Format", "bad5")
        });
    }
}
