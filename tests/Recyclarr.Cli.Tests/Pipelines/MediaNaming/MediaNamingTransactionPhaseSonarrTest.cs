using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming;

[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
internal sealed class MediaNamingTransactionPhaseSonarrTest
{
    [Test, AutoMockData]
    public async Task Sonarr_left_null(MediaNamingTransactionPhase sut)
    {
        var context = new MediaNamingPipelineContext
        {
            ApiFetchOutput = new SonarrMediaNamingDto(),
            ConfigOutput = new ProcessedNamingConfig
            {
                Dto = new SonarrMediaNamingDto
                {
                    RenameEpisodes = true,
                    SeasonFolderFormat = "season_default",
                    SeriesFolderFormat = "series_plex",
                    StandardEpisodeFormat = "episodes_standard_default_3",
                    DailyEpisodeFormat = "episodes_daily_default_3",
                    AnimeEpisodeFormat = "episodes_anime_default_3",
                },
            },
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(context.ConfigOutput.Dto, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Sonarr_right_null(MediaNamingTransactionPhase sut)
    {
        var context = new MediaNamingPipelineContext
        {
            ApiFetchOutput = new SonarrMediaNamingDto
            {
                RenameEpisodes = true,
                SeasonFolderFormat = "season_default",
                SeriesFolderFormat = "series_plex",
                StandardEpisodeFormat = "episodes_standard_default_3",
                DailyEpisodeFormat = "episodes_daily_default_3",
                AnimeEpisodeFormat = "episodes_anime_default_3",
            },
            ConfigOutput = new ProcessedNamingConfig { Dto = new SonarrMediaNamingDto() },
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(context.ApiFetchOutput, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Sonarr_right_and_left_with_rename(MediaNamingTransactionPhase sut)
    {
        var context = new MediaNamingPipelineContext
        {
            ApiFetchOutput = new SonarrMediaNamingDto
            {
                RenameEpisodes = false,
                SeasonFolderFormat = "season_default",
                SeriesFolderFormat = "series_plex",
                StandardEpisodeFormat = "episodes_standard_default",
                DailyEpisodeFormat = "episodes_daily_default",
                AnimeEpisodeFormat = "episodes_anime_default",
            },
            ConfigOutput = new ProcessedNamingConfig
            {
                Dto = new SonarrMediaNamingDto
                {
                    RenameEpisodes = true,
                    SeasonFolderFormat = "season_default2",
                    SeriesFolderFormat = "series_plex2",
                    StandardEpisodeFormat = "episodes_standard_default2",
                    DailyEpisodeFormat = "episodes_daily_default2",
                    AnimeEpisodeFormat = "episodes_anime_default2",
                },
            },
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(context.ConfigOutput.Dto, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Sonarr_right_and_left_without_rename(MediaNamingTransactionPhase sut)
    {
        var context = new MediaNamingPipelineContext
        {
            ApiFetchOutput = new SonarrMediaNamingDto
            {
                RenameEpisodes = true,
                SeasonFolderFormat = "season_default",
                SeriesFolderFormat = "series_plex",
                StandardEpisodeFormat = "episodes_standard_default",
                DailyEpisodeFormat = "episodes_daily_default",
                AnimeEpisodeFormat = "episodes_anime_default",
            },
            ConfigOutput = new ProcessedNamingConfig
            {
                Dto = new SonarrMediaNamingDto
                {
                    RenameEpisodes = false,
                    SeasonFolderFormat = "season_default2",
                    SeriesFolderFormat = "series_plex2",
                    StandardEpisodeFormat = "episodes_standard_default2",
                    DailyEpisodeFormat = "episodes_daily_default2",
                    AnimeEpisodeFormat = "episodes_anime_default2",
                },
            },
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new SonarrMediaNamingDto
                {
                    RenameEpisodes = false,
                    SeasonFolderFormat = "season_default2",
                    SeriesFolderFormat = "series_plex2",
                    StandardEpisodeFormat = "episodes_standard_default2",
                    DailyEpisodeFormat = "episodes_daily_default2",
                    AnimeEpisodeFormat = "episodes_anime_default2",
                },
                o => o.PreferringRuntimeMemberTypes()
            );
    }
}
