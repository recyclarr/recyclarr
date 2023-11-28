using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.Common;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.TrashGuide.MediaNaming;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming;

[TestFixture]
public class MediaNamingConfigPhaseTest
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
    public async Task Sonarr_v3_naming(
        [Frozen] ISonarrCapabilityFetcher capabilities,
        [Frozen] IMediaNamingGuideService guide,
        MediaNamingConfigPhase sut)
    {
        capabilities.GetCapabilities(default!).ReturnsForAnyArgs(new SonarrCapabilities
        {
            SupportsCustomFormats = false
        });

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

        var result = await sut.Execute(config);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new ProcessedNamingConfig
            {
                Dto = new SonarrMediaNamingDto
                {
                    RenameEpisodes = true,
                    SeasonFolderFormat = "season_default",
                    SeriesFolderFormat = "series_plex",
                    StandardEpisodeFormat = "episodes_standard_default_3",
                    DailyEpisodeFormat = "episodes_daily_default_3",
                    AnimeEpisodeFormat = "episodes_anime_default_3"
                }
            },
            o => o.RespectingRuntimeTypes());
    }

    [Test, AutoMockData]
    public async Task Sonarr_v4_naming(
        [Frozen] ISonarrCapabilityFetcher capabilities,
        [Frozen] IMediaNamingGuideService guide,
        MediaNamingConfigPhase sut)
    {
        capabilities.GetCapabilities(default!).ReturnsForAnyArgs(new SonarrCapabilities
        {
            SupportsCustomFormats = true
        });

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

        var result = await sut.Execute(config);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new ProcessedNamingConfig
            {
                Dto = new SonarrMediaNamingDto
                {
                    RenameEpisodes = true,
                    SeasonFolderFormat = "season_default",
                    SeriesFolderFormat = "series_plex",
                    StandardEpisodeFormat = "episodes_standard_default_4",
                    DailyEpisodeFormat = "episodes_daily_default_4",
                    AnimeEpisodeFormat = "episodes_anime_default_4"
                }
            },
            o => o.RespectingRuntimeTypes());
    }

    [Test, AutoMockData]
    public async Task Sonarr_invalid_names(
        [Frozen] ISonarrCapabilityFetcher capabilities,
        [Frozen] IMediaNamingGuideService guide,
        MediaNamingConfigPhase sut)
    {
        capabilities.GetCapabilities(default!).ReturnsForAnyArgs(new SonarrCapabilities
        {
            SupportsCustomFormats = true
        });

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

        var result = await sut.Execute(config);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new ProcessedNamingConfig
            {
                Dto = new SonarrMediaNamingDto
                {
                    RenameEpisodes = true
                },
                InvalidNaming = new[]
                {
                    new InvalidNamingConfig("Season Folder Format", "bad1"),
                    new InvalidNamingConfig("Series Folder Format", "bad2"),
                    new InvalidNamingConfig("Standard Episode Format", "bad3"),
                    new InvalidNamingConfig("Daily Episode Format", "bad4"),
                    new InvalidNamingConfig("Anime Episode Format", "bad5")
                }
            },
            o => o.RespectingRuntimeTypes());
    }

    [Test, AutoMockData]
    public async Task Radarr_naming(
        [Frozen] IMediaNamingGuideService guide,
        MediaNamingConfigPhase sut)
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

        var result = await sut.Execute(config);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new ProcessedNamingConfig
            {
                Dto = new RadarrMediaNamingDto
                {
                    RenameMovies = true,
                    StandardMovieFormat = "file_emby",
                    MovieFolderFormat = "folder_plex"
                }
            },
            o => o.RespectingRuntimeTypes());
    }

    private sealed record UnsupportedConfigType : ServiceConfiguration
    {
        public override SupportedServices ServiceType => default!;
    }

    [Test, AutoMockData]
    public async Task Throw_on_unknown_config_type(
        MediaNamingConfigPhase sut)
    {
        var act = () => sut.Execute(new UnsupportedConfigType {InstanceName = ""});
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test, AutoMockData]
    public async Task Assign_null_when_config_null(
        [Frozen] IMediaNamingGuideService guide,
        MediaNamingConfigPhase sut)
    {
        guide.GetRadarrNamingData().Returns(RadarrNamingData);

        var config = new RadarrConfiguration
        {
            InstanceName = "",
            MediaNaming = new RadarrMediaNamingConfig
            {
                Folder = null
            }
        };

        var result = await sut.Execute(config);

        result.Should().NotBeNull();
        result.Dto.Should().BeOfType<RadarrMediaNamingDto>()
            .Which.MovieFolderFormat.Should().BeNull();
    }
}
