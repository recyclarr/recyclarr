using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming;

[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
internal sealed class MediaNamingTransactionPhaseSonarrTest
{
    private static TestPlan CreatePlan(SonarrNamingData data)
    {
        var plan = new TestPlan
        {
            SonarrMediaNaming = new PlannedSonarrMediaNaming { Data = data },
        };
        return plan;
    }

    [Test, AutoMockData]
    public async Task Sonarr_left_null(SonarrNamingTransactionPhase sut)
    {
        var configData = new SonarrNamingData
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
            ApiFetchOutput = new SonarrNamingData(),
            Plan = CreatePlan(configData),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(configData, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Sonarr_right_null(SonarrNamingTransactionPhase sut)
    {
        var apiData = new SonarrNamingData
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
            ApiFetchOutput = apiData,
            Plan = CreatePlan(new SonarrNamingData()),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(apiData, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Sonarr_right_and_left_with_rename(SonarrNamingTransactionPhase sut)
    {
        var configData = new SonarrNamingData
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
            ApiFetchOutput = new SonarrNamingData
            {
                RenameEpisodes = false,
                SeasonFolderFormat = "season_default",
                SeriesFolderFormat = "series_plex",
                StandardEpisodeFormat = "episodes_standard_default",
                DailyEpisodeFormat = "episodes_daily_default",
                AnimeEpisodeFormat = "episodes_anime_default",
            },
            Plan = CreatePlan(configData),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(configData, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Sonarr_right_and_left_without_rename(SonarrNamingTransactionPhase sut)
    {
        var context = new SonarrNamingPipelineContext
        {
            ApiFetchOutput = new SonarrNamingData
            {
                RenameEpisodes = true,
                SeasonFolderFormat = "season_default",
                SeriesFolderFormat = "series_plex",
                StandardEpisodeFormat = "episodes_standard_default",
                DailyEpisodeFormat = "episodes_daily_default",
                AnimeEpisodeFormat = "episodes_anime_default",
            },
            Plan = CreatePlan(
                new SonarrNamingData
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
                new SonarrNamingData
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
