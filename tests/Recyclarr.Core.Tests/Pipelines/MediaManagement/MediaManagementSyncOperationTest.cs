using Recyclarr.Config.Models;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.MediaManagement;

internal sealed class MediaManagementSyncOperationTest
{
    [Test]
    public async Task Compute_returns_delta_for_changed_mode()
    {
        var (sut, api) = CreateOperation(PropersAndRepacksMode.PreferAndUpgrade);

        var result = await ExecuteOperation(sut, PropersAndRepacksMode.DoNotUpgrade, preview: true);

        result.Status.Should().Be(SyncResultStatus.Succeeded);
        result
            .Delta.Should()
            .BeEquivalentTo(
                new MediaManagementDelta(
                    new ValueDelta<PropersAndRepacksMode?>(
                        PropersAndRepacksMode.PreferAndUpgrade,
                        PropersAndRepacksMode.DoNotUpgrade
                    )
                )
            );
        await api.DidNotReceiveWithAnyArgs().UpdateMediaManagement(default!, default);
    }

    [Test]
    public async Task Compute_omits_delta_for_unchanged_mode()
    {
        var mode = PropersAndRepacksMode.DoNotPrefer;
        var (sut, _) = CreateOperation(mode);

        var result = await ExecuteOperation(sut, mode, preview: true);

        result.Status.Should().Be(SyncResultStatus.Succeeded);
        result.Delta.Should().BeNull();
    }

    [Test]
    public async Task Compute_preserves_null_current_value_in_delta()
    {
        var (sut, _) = CreateOperation(null);

        var result = await ExecuteOperation(
            sut,
            PropersAndRepacksMode.PreferAndUpgrade,
            preview: true
        );

        result
            .Delta.Should()
            .BeEquivalentTo(
                new MediaManagementDelta(
                    new ValueDelta<PropersAndRepacksMode?>(
                        null,
                        PropersAndRepacksMode.PreferAndUpgrade
                    )
                )
            );
    }

    [Test]
    public async Task Read_failure_propagates_without_write()
    {
        var api = Substitute.For<IMediaManagementService>();
        api.GetMediaManagement(default)
            .ReturnsForAnyArgs(Task.FromException<MediaManagementData>(new IOException("failed")));
        var sut = new MediaManagementSyncOperation(Substitute.For<ILogger>(), api);

        var act = () => ExecuteOperation(sut, PropersAndRepacksMode.DoNotPrefer, preview: true);

        await act.Should().ThrowAsync<IOException>().WithMessage("failed");
        await api.DidNotReceiveWithAnyArgs().UpdateMediaManagement(default!, default);
    }

    [Test]
    public async Task Read_cancellation_propagates_requested_token()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var ct = cts.Token;
        var api = Substitute.For<IMediaManagementService>();
        api.GetMediaManagement(ct).Returns(Task.FromCanceled<MediaManagementData>(ct));
        var sut = new MediaManagementSyncOperation(Substitute.For<ILogger>(), api);

        var act = () =>
            ExecuteOperation(sut, PropersAndRepacksMode.DoNotPrefer, preview: true, ct: ct);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await api.Received(1).GetMediaManagement(ct);
        await api.DidNotReceiveWithAnyArgs().UpdateMediaManagement(default!, default);
    }

    [Test]
    public async Task Persist_writes_changed_mode_once_and_reports_one_change()
    {
        var (sut, api) = CreateOperation(PropersAndRepacksMode.PreferAndUpgrade);
        MediaManagementData? written = null;
        api.WhenForAnyArgs(x => x.UpdateMediaManagement(default!, default))
            .Do(x => written = x.Arg<MediaManagementData>());
        var publisher = Substitute.For<IPipelinePublisher>();

        await ExecuteOperation(
            sut,
            PropersAndRepacksMode.DoNotPrefer,
            preview: false,
            publisher: publisher
        );

        await api.ReceivedWithAnyArgs(1).UpdateMediaManagement(default!, default);
        written.Should().NotBeNull();
        written.Id.Should().Be(42);
        written.PropersAndRepacks.Should().Be(PropersAndRepacksMode.DoNotPrefer);
        publisher.Received().SetStatus(PipelineProgressStatus.Succeeded, 1);
    }

    [Test]
    public async Task Persist_skips_unchanged_mode_and_reports_zero_changes()
    {
        var mode = PropersAndRepacksMode.DoNotPrefer;
        var (sut, api) = CreateOperation(mode);
        var publisher = Substitute.For<IPipelinePublisher>();

        await ExecuteOperation(sut, mode, preview: false, publisher: publisher);

        await api.DidNotReceiveWithAnyArgs().UpdateMediaManagement(default!, default);
        publisher.Received().SetStatus(PipelineProgressStatus.Succeeded, 0);
    }

    [Test]
    public async Task Write_failure_propagates_after_one_attempt_and_keeps_delta()
    {
        var (sut, api) = CreateOperation(PropersAndRepacksMode.DoNotUpgrade);
        api.UpdateMediaManagement(default!, default)
            .ReturnsForAnyArgs(Task.FromException(new IOException("failed")));
        var act = () => ExecuteOperation(sut, PropersAndRepacksMode.DoNotPrefer, preview: false);

        await act.Should().ThrowAsync<IOException>().WithMessage("failed");
        await api.ReceivedWithAnyArgs(1).UpdateMediaManagement(default!, default);
    }

    [Test]
    public async Task Write_cancellation_propagates_requested_token()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var ct = cts.Token;
        var (sut, api) = CreateOperation(PropersAndRepacksMode.DoNotUpgrade);
        var receivedToken = CancellationToken.None;
        api.WhenForAnyArgs(x => x.UpdateMediaManagement(default!, default))
            .Do(x => receivedToken = x.ArgAt<CancellationToken>(1));
        api.UpdateMediaManagement(default!, ct).ReturnsForAnyArgs(Task.FromCanceled(ct));
        var act = () =>
            ExecuteOperation(sut, PropersAndRepacksMode.DoNotPrefer, preview: false, ct: ct);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await api.ReceivedWithAnyArgs(1).UpdateMediaManagement(default!, default);
        receivedToken.Should().Be(ct);
    }

    [Test]
    public async Task Recompute_after_write_is_unchanged_without_second_write()
    {
        var mode = PropersAndRepacksMode.DoNotUpgrade;
        var api = Substitute.For<IMediaManagementService>();
        api.GetMediaManagement(default)
            .ReturnsForAnyArgs(_ => new MediaManagementData { Id = 42, PropersAndRepacks = mode });
        api.WhenForAnyArgs(x => x.UpdateMediaManagement(default!, default))
            .Do(x =>
                mode =
                    x.Arg<MediaManagementData>().PropersAndRepacks
                    ?? throw new InvalidOperationException("Expected configured mode")
            );
        var sut = new MediaManagementSyncOperation(Substitute.For<ILogger>(), api);

        var first = await ExecuteOperation(sut, PropersAndRepacksMode.DoNotPrefer, preview: false);
        var second = await ExecuteOperation(sut, PropersAndRepacksMode.DoNotPrefer, preview: false);

        first.Delta.Should().NotBeNull();
        second.Delta.Should().BeNull();
        await api.ReceivedWithAnyArgs(1).UpdateMediaManagement(default!, default);
    }

    [Test]
    public async Task Preview_and_apply_return_same_delta_but_only_apply_writes()
    {
        var preview = await Execute(preview: true);
        var apply = await Execute(preview: false);

        preview.Result.Should().BeEquivalentTo(apply.Result);
        await preview.Api.DidNotReceiveWithAnyArgs().UpdateMediaManagement(default!, default);
        await apply.Api.ReceivedWithAnyArgs(1).UpdateMediaManagement(default!, default);
        preview.Publisher.Received().SetStatus(PipelineProgressStatus.Succeeded);
        apply.Publisher.Received().SetStatus(PipelineProgressStatus.Succeeded, 1);
    }

    [Test]
    public async Task Execute_omits_unconfigured_media_management()
    {
        var (sut, api) = CreateOperation(PropersAndRepacksMode.DoNotUpgrade);
        var executor = new CompositeSyncPipeline(Substitute.For<ILogger>(), [sut]);
        var instancePublisher = Substitute.For<IInstancePublisher>();
        instancePublisher.ForPipeline(default).ReturnsForAnyArgs(IPipelinePublisher.Noop);

        var results = await executor.Execute(
            Substitute.For<ISyncSettings>(),
            new TestPlan(),
            instancePublisher,
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        results.Should().BeEmpty();
        await api.DidNotReceiveWithAnyArgs().GetMediaManagement(default);
    }

    private static (MediaManagementSyncOperation Sut, IMediaManagementService Api) CreateOperation(
        PropersAndRepacksMode? current
    )
    {
        var api = Substitute.For<IMediaManagementService>();
        api.GetMediaManagement(default)
            .ReturnsForAnyArgs(new MediaManagementData { Id = 42, PropersAndRepacks = current });
        return (new MediaManagementSyncOperation(Substitute.For<ILogger>(), api), api);
    }

    private static async Task<MediaManagementPipelineResult> ExecuteOperation(
        MediaManagementSyncOperation sut,
        PropersAndRepacksMode desired,
        bool preview,
        IPipelinePublisher? publisher = null,
        CancellationToken ct = default
    )
    {
        var result = await ((ISyncOperation)sut).Execute(
            preview,
            Plan(desired),
            publisher ?? IPipelinePublisher.Noop,
            _ => { },
            ct
        );
        return result.Should().BeOfType<MediaManagementPipelineResult>().Which;
    }

    private static async Task<(
        MediaManagementPipelineResult Result,
        IMediaManagementService Api,
        IPipelinePublisher Publisher
    )> Execute(bool preview)
    {
        var (sut, api) = CreateOperation(PropersAndRepacksMode.DoNotUpgrade);
        var executor = new CompositeSyncPipeline(Substitute.For<ILogger>(), [sut]);
        var publisher = Substitute.For<IPipelinePublisher>();
        var instancePublisher = Substitute.For<IInstancePublisher>();
        instancePublisher.ForPipeline(default).ReturnsForAnyArgs(publisher);
        var settings = Substitute.For<ISyncSettings>();
        settings.Preview.Returns(preview);

        var results = await executor.Execute(
            settings,
            Plan(PropersAndRepacksMode.DoNotPrefer),
            instancePublisher,
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        var result = results.Should().ContainSingle().Which;
        return (result.Should().BeOfType<MediaManagementPipelineResult>().Which, api, publisher);
    }

    private static TestPlan Plan(PropersAndRepacksMode desired) =>
        new() { MediaManagement = new PlannedMediaManagement(desired) };
}
