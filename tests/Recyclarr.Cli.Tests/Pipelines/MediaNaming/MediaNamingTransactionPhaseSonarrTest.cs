using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming;

[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
internal sealed class MediaNamingTransactionPhaseSonarrTest
{
    private static TestPlan CreatePlan(SonarrMediaNamingDto dto)
    {
        var plan = new TestPlan { SonarrMediaNaming = new PlannedSonarrMediaNaming { Dto = dto } };
        return plan;
    }

    [Test, AutoMockData]
    public async Task Sonarr_left_null(SonarrNamingTransactionPhase sut)
    {
        var configDto = new SonarrMediaNamingDto
        {
            RenameEpisodes = true,
            SeasonFolderFormat = "season_default",
            SeriesFolderFormat = "series_plex",
            StandardEpisodeFormat = "episodes_standard_default_3",
            DailyEpisodeFormat = "episodes_daily_default_3",
            AnimeEpisodeFormat = "episodes_anime_default_3",
        };

        var context = new SonarrNamingPipelineContext
        {
            ApiFetchOutput = new SonarrMediaNamingDto(),
            Plan = CreatePlan(configDto),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(configDto, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Sonarr_right_null(SonarrNamingTransactionPhase sut)
    {
        var apiDto = new SonarrMediaNamingDto
        {
            RenameEpisodes = true,
            SeasonFolderFormat = "season_default",
            SeriesFolderFormat = "series_plex",
            StandardEpisodeFormat = "episodes_standard_default_3",
            DailyEpisodeFormat = "episodes_daily_default_3",
            AnimeEpisodeFormat = "episodes_anime_default_3",
        };

        var context = new SonarrNamingPipelineContext
        {
            ApiFetchOutput = apiDto,
            Plan = CreatePlan(new SonarrMediaNamingDto()),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(apiDto, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Sonarr_right_and_left_with_rename(SonarrNamingTransactionPhase sut)
    {
        var configDto = new SonarrMediaNamingDto
        {
            RenameEpisodes = true,
            SeasonFolderFormat = "season_default2",
            SeriesFolderFormat = "series_plex2",
            StandardEpisodeFormat = "episodes_standard_default2",
            DailyEpisodeFormat = "episodes_daily_default2",
            AnimeEpisodeFormat = "episodes_anime_default2",
        };

        var context = new SonarrNamingPipelineContext
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
            Plan = CreatePlan(configDto),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(configDto, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Sonarr_right_and_left_without_rename(SonarrNamingTransactionPhase sut)
    {
        var context = new SonarrNamingPipelineContext
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
            Plan = CreatePlan(
                new SonarrMediaNamingDto
                {
                    RenameEpisodes = false,
                    SeasonFolderFormat = "season_default2",
                    SeriesFolderFormat = "series_plex2",
                    StandardEpisodeFormat = "episodes_standard_default2",
                    DailyEpisodeFormat = "episodes_daily_default2",
                    AnimeEpisodeFormat = "episodes_anime_default2",
                }
            ),
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
