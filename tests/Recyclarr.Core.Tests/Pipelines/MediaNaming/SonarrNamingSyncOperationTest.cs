using Recyclarr.Pipelines;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.MediaNaming;

internal sealed class SonarrNamingSyncOperationTest
{
    [Test]
    public async Task Compute_returns_changed_values_and_retains_reference_mismatch()
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
        var sut = new SonarrNamingSyncOperation(Substitute.For<ILogger>(), api);
        var mismatch = new SonarrNamingReferenceMismatchOutcome(
            SonarrNamingFormatField.AnimeEpisodeFormat,
            "unknown"
        );
        var plan = new TestPlan
        {
            SonarrMediaNaming = new PlannedSonarrMediaNaming
            {
                Data = new SonarrNamingData
                {
                    RenameEpisodes = false,
                    StandardEpisodeFormat = "standard-desired",
                    DailyEpisodeFormat = "daily-current",
                },
                Mismatches = [mismatch],
            },
        };

        var result = await Execute(sut, plan, preview: true);

        result.Status.Should().Be(SyncResultStatus.Partial);
        result.Outcomes.Should().Equal(mismatch);
        result
            .Delta.Should()
            .BeEquivalentTo(
                new SonarrNamingDelta
                {
                    RenameEpisodes = new ValueDelta<bool?>(true, false),
                    StandardEpisodeFormat = new ValueDelta<string?>(
                        "standard-current",
                        "standard-desired"
                    ),
                }
            );
    }

    [Test]
    public async Task All_invalid_fields_fail_without_reading_or_writing_service()
    {
        var api = Substitute.For<ISonarrNamingService>();
        var sut = new SonarrNamingSyncOperation(Substitute.For<ILogger>(), api);
        var plan = new TestPlan
        {
            SonarrMediaNaming = new PlannedSonarrMediaNaming
            {
                Data = new SonarrNamingData(),
                Mismatches =
                [
                    new SonarrNamingReferenceMismatchOutcome(
                        SonarrNamingFormatField.SeriesFolderFormat,
                        "unknown"
                    ),
                ],
            },
        };

        var result = await Execute(sut, plan, preview: false);

        result.Status.Should().Be(SyncResultStatus.Failed);
        result.Delta.Should().BeNull();
        await api.DidNotReceiveWithAnyArgs().GetNaming(default);
        await api.DidNotReceiveWithAnyArgs().UpdateNaming(default!, default);
    }

    [Test]
    public async Task Persist_writes_changed_settings_once()
    {
        var api = Substitute.For<ISonarrNamingService>();
        api.GetNaming(default).ReturnsForAnyArgs(new SonarrNamingData { RenameEpisodes = true });
        var sut = new SonarrNamingSyncOperation(Substitute.For<ILogger>(), api);
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
        var sut = new SonarrNamingSyncOperation(Substitute.For<ILogger>(), api);
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
        var sut = new SonarrNamingSyncOperation(Substitute.For<ILogger>(), api);
        var plan = Plan(new SonarrNamingData { RenameEpisodes = false });

        var act = () => Execute(sut, plan, preview: true);

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    private static TestPlan Plan(SonarrNamingData data) =>
        new() { SonarrMediaNaming = new PlannedSonarrMediaNaming { Data = data } };

    private static async Task<SonarrNamingPipelineResult> Execute(
        SonarrNamingSyncOperation sut,
        PipelinePlan plan,
        bool preview
    )
    {
        var result = await ((ISyncOperation)sut).Execute(
            preview,
            plan,
            Substitute.For<IPipelinePublisher>(),
            _ => { },
            CancellationToken.None
        );
        return result.Should().BeOfType<SonarrNamingPipelineResult>().Which;
    }
}
