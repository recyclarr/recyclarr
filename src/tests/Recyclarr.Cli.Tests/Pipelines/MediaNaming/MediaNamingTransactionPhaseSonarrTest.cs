using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class MediaNamingTransactionPhaseSonarrTest
{
    [Test, AutoMockData]
    public void Sonarr_left_null(
        MediaNamingTransactionPhase sut)
    {
        var left = new SonarrMediaNamingDto();

        var right = new ProcessedNamingConfig
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
        };

        var result = sut.Execute(left, right);

        result.Should().BeEquivalentTo(right.Dto, o => o.RespectingRuntimeTypes());
    }

    [Test, AutoMockData]
    public void Sonarr_right_null(
        MediaNamingTransactionPhase sut)
    {
        var left = new SonarrMediaNamingDto
        {
            RenameEpisodes = true,
            SeasonFolderFormat = "season_default",
            SeriesFolderFormat = "series_plex",
            StandardEpisodeFormat = "episodes_standard_default_3",
            DailyEpisodeFormat = "episodes_daily_default_3",
            AnimeEpisodeFormat = "episodes_anime_default_3"
        };

        var right = new ProcessedNamingConfig
        {
            Dto = new SonarrMediaNamingDto()
        };

        var result = sut.Execute(left, right);

        result.Should().BeEquivalentTo(left, o => o.RespectingRuntimeTypes());
    }

    [Test, AutoMockData]
    public void Sonarr_right_and_left_with_rename(
        MediaNamingTransactionPhase sut)
    {
        var left = new SonarrMediaNamingDto
        {
            RenameEpisodes = false,
            SeasonFolderFormat = "season_default",
            SeriesFolderFormat = "series_plex",
            StandardEpisodeFormat = "episodes_standard_default",
            DailyEpisodeFormat = "episodes_daily_default",
            AnimeEpisodeFormat = "episodes_anime_default"
        };

        var right = new ProcessedNamingConfig
        {
            Dto = new SonarrMediaNamingDto
            {
                RenameEpisodes = true,
                SeasonFolderFormat = "season_default2",
                SeriesFolderFormat = "series_plex2",
                StandardEpisodeFormat = "episodes_standard_default2",
                DailyEpisodeFormat = "episodes_daily_default2",
                AnimeEpisodeFormat = "episodes_anime_default2"
            }
        };

        var result = sut.Execute(left, right);

        result.Should().BeEquivalentTo(right.Dto, o => o.RespectingRuntimeTypes());
    }

    [Test, AutoMockData]
    public void Sonarr_right_and_left_without_rename(
        MediaNamingTransactionPhase sut)
    {
        var left = new SonarrMediaNamingDto
        {
            RenameEpisodes = true,
            SeasonFolderFormat = "season_default",
            SeriesFolderFormat = "series_plex",
            StandardEpisodeFormat = "episodes_standard_default",
            DailyEpisodeFormat = "episodes_daily_default",
            AnimeEpisodeFormat = "episodes_anime_default"
        };

        var right = new ProcessedNamingConfig
        {
            Dto = new SonarrMediaNamingDto
            {
                RenameEpisodes = false,
                SeasonFolderFormat = "season_default2",
                SeriesFolderFormat = "series_plex2",
                StandardEpisodeFormat = "episodes_standard_default2",
                DailyEpisodeFormat = "episodes_daily_default2",
                AnimeEpisodeFormat = "episodes_anime_default2"
            }
        };

        var result = sut.Execute(left, right);

        result.Should().BeEquivalentTo(new SonarrMediaNamingDto
            {
                RenameEpisodes = false,
                SeasonFolderFormat = "season_default2",
                SeriesFolderFormat = "series_plex2",
                StandardEpisodeFormat = "episodes_standard_default",
                DailyEpisodeFormat = "episodes_daily_default",
                AnimeEpisodeFormat = "episodes_anime_default"
            },
            o => o.RespectingRuntimeTypes());
    }
}
