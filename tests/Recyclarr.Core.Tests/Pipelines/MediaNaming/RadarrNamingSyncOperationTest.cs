using Recyclarr.Pipelines;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.MediaNaming;

internal sealed class RadarrNamingSyncOperationTest
{
    [Test]
    public async Task Compute_returns_one_delta_with_only_changed_values()
    {
        var api = Substitute.For<IRadarrNamingService>();
        api.GetNaming(default)
            .ReturnsForAnyArgs(
                new RadarrNamingData
                {
                    RenameMovies = null,
                    StandardMovieFormat = "standard-current",
                    MovieFolderFormat = "folder-current",
                }
            );
        var sut = new RadarrNamingSyncOperation(Substitute.For<ILogger>(), api);
        var plan = new TestPlan
        {
            RadarrMediaNaming = new PlannedRadarrMediaNaming
            {
                Data = new RadarrNamingData
                {
                    RenameMovies = false,
                    StandardMovieFormat = "standard-current",
                    MovieFolderFormat = "",
                },
            },
        };

        var result = await Execute(sut, plan, preview: true);

        result.Status.Should().Be(SyncResultStatus.Succeeded);
        result
            .Delta.Should()
            .BeEquivalentTo(
                new RadarrNamingDelta
                {
                    RenameMovies = new ValueDelta<bool?>(null, false),
                    MovieFolderFormat = new ValueDelta<string?>("folder-current", ""),
                }
            );
    }

    [Test]
    public async Task Mixed_valid_and_invalid_unchanged_fields_are_partial_without_write()
    {
        var api = Substitute.For<IRadarrNamingService>();
        api.GetNaming(default).ReturnsForAnyArgs(new RadarrNamingData { RenameMovies = false });
        var sut = new RadarrNamingSyncOperation(Substitute.For<ILogger>(), api);
        var mismatch = new RadarrNamingReferenceMismatchOutcome(
            RadarrNamingFormatField.StandardMovieFormat,
            "unknown"
        );
        var plan = new TestPlan
        {
            RadarrMediaNaming = new PlannedRadarrMediaNaming
            {
                Data = new RadarrNamingData { RenameMovies = false },
                Mismatches = [mismatch],
            },
        };

        var result = await Execute(sut, plan, preview: false);

        result.Status.Should().Be(SyncResultStatus.Partial);
        result.Outcomes.Should().Equal(mismatch);
        result.Delta.Should().BeNull();
        await api.DidNotReceiveWithAnyArgs().UpdateNaming(default!, default);
    }

    [Test]
    public async Task Write_failure_propagates_after_one_attempt()
    {
        var api = Substitute.For<IRadarrNamingService>();
        api.GetNaming(default).ReturnsForAnyArgs(new RadarrNamingData { RenameMovies = true });
        api.UpdateNaming(default!, default)
            .ReturnsForAnyArgs(Task.FromException(new InvalidOperationException("rejected")));
        var sut = new RadarrNamingSyncOperation(Substitute.For<ILogger>(), api);
        var plan = new TestPlan
        {
            RadarrMediaNaming = new PlannedRadarrMediaNaming
            {
                Data = new RadarrNamingData { RenameMovies = false },
            },
        };
        var act = () => Execute(sut, plan, preview: false);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("rejected");
        await api.ReceivedWithAnyArgs(1).UpdateNaming(default!, default);
    }

    private static async Task<RadarrNamingPipelineResult> Execute(
        RadarrNamingSyncOperation sut,
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
        return result.Should().BeOfType<RadarrNamingPipelineResult>().Which;
    }
}
