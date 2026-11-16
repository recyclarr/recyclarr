using Recyclarr.Pipelines;
using Recyclarr.Pipelines.MediaNaming;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.MediaNaming;

internal sealed class SonarrMediaNamingSyncTest
{
    [Test]
    public async Task Compute_returns_configured_values_and_retains_reference_mismatch()
    {
        var api = Substitute.For<ISonarrNamingService>();
        api.GetNaming(default)
            .ReturnsForAnyArgs(
                new SonarrNamingData
                {
                    RenameEpisodes = true,
                    SeriesFolderFormat = "series-current",
                    SeasonFolderFormat = "season-current",
                    StandardEpisodeFormat = "standard-current",
                    DailyEpisodeFormat = "daily-current",
                    AnimeEpisodeFormat = "anime-current",
                }
            );
        var sut = CreateSut(api);
        var mismatch = new SonarrNamingReferenceMismatchOutcome(
            SonarrNamingFormatField.AnimeEpisodeFormat,
            "unknown"
        );
        var plan = new PlannedSonarrMediaNaming
        {
            Data = new SonarrNamingData
            {
                RenameEpisodes = false,
                StandardEpisodeFormat = "standard-desired",
                DailyEpisodeFormat = "daily-current",
            },
            Mismatches = [mismatch],
        };

        var result = await Execute(sut, plan, preview: true);

        result.Status.Should().Be(SyncResultStatus.Partial);
        result.Outcomes.Should().Equal(mismatch);
        // Unconfigured fields are absent; a configured field that already matches is unchanged.
        result
            .Settings.Should()
            .Be(
                new PlannedSonarrNamingResource
                {
                    Action = PlannedAction.Update,
                    RenameEpisodes = PlannedValue.Compared<bool?>(true, false),
                    StandardEpisodeFormat = PlannedValue.Compared<string?>(
                        "standard-current",
                        "standard-desired"
                    ),
                    DailyEpisodeFormat = PlannedValue.Compared<string?>(
                        "daily-current",
                        "daily-current"
                    ),
                }
            );
    }

    [Test]
    public async Task All_invalid_fields_fail_without_reading_or_writing_service()
    {
        var api = Substitute.For<ISonarrNamingService>();
        var sut = CreateSut(api);
        var plan = new PlannedSonarrMediaNaming
        {
            Data = new SonarrNamingData(),
            Mismatches =
            [
                new SonarrNamingReferenceMismatchOutcome(
                    SonarrNamingFormatField.SeriesFolderFormat,
                    "unknown"
                ),
            ],
        };

        var result = await Execute(sut, plan, preview: false);

        result.Status.Should().Be(SyncResultStatus.Failed);
        result.Settings.Should().BeNull();
        await api.DidNotReceiveWithAnyArgs().GetNaming(default);
        await api.DidNotReceiveWithAnyArgs().UpdateNaming(default!, default);
    }

    [Test]
    public async Task Persist_writes_changed_settings_once()
    {
        var api = Substitute.For<ISonarrNamingService>();
        api.GetNaming(default).ReturnsForAnyArgs(new SonarrNamingData { RenameEpisodes = true });
        var sut = CreateSut(api);
        var changedPlan = Plan(new SonarrNamingData { RenameEpisodes = false });

        await Execute(sut, changedPlan, preview: false);
        await api.Received(1)
            .UpdateNaming(new SonarrNamingData { RenameEpisodes = false }, CancellationToken.None);
    }

    [Test]
    public async Task Persist_skips_unchanged_settings()
    {
        var api = Substitute.For<ISonarrNamingService>();
        api.GetNaming(default).ReturnsForAnyArgs(new SonarrNamingData { RenameEpisodes = true });
        var sut = CreateSut(api);
        var plan = Plan(new SonarrNamingData { RenameEpisodes = true });

        await Execute(sut, plan, preview: false);

        await api.DidNotReceiveWithAnyArgs().UpdateNaming(default!, default);
    }

    [Test]
    public async Task Read_cancellation_propagates()
    {
        var api = Substitute.For<ISonarrNamingService>();
        api.GetNaming(default)
            .ReturnsForAnyArgs(Task.FromCanceled<SonarrNamingData>(new CancellationToken(true)));
        var sut = CreateSut(api);
        var plan = Plan(new SonarrNamingData { RenameEpisodes = false });

        var act = () => Execute(sut, plan, preview: true);

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    private static PlannedSonarrMediaNaming Plan(SonarrNamingData data) => new() { Data = data };

    private static MediaNamingSyncOperation CreateSut(ISonarrNamingService api) =>
        new(new SonarrNamingSync(Substitute.For<ILogger>(), api));

    private static async Task<SonarrNamingPipelineResult> Execute(
        MediaNamingSyncOperation sut,
        PlannedMediaNaming plan,
        bool preview
    )
    {
        var result = await ((ISyncOperation<PlannedMediaNaming>)sut).Execute(
            preview,
            plan,
            [],
            _ => { },
            CancellationToken.None
        );
        return result.Should().BeOfType<SonarrNamingPipelineResult>().Which;
    }
}
