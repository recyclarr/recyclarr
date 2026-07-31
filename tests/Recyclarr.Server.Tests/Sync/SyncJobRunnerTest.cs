using Autofac;
using NSubstitute.ExceptionExtensions;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Tests.Reusable;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Tests.Sync;

internal sealed class SyncJobRunnerTest : ServerIntegrationFixture
{
    private readonly INotificationService _notify = Substitute.For<INotificationService>();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
        builder.RegisterInstance(_notify).As<INotificationService>();
    }

    private async Task<SyncJob> RunJob()
    {
        var settings = new ServerSyncSettings(
            Service: null,
            Instances: [],
            Preview: false,
            Configs: []
        );

        var store = Resolve<ISyncJobStore>();
        var job = store.Create(settings);

        using var scope = Resolve<SyncRunScopeFactory>().Start<SyncJobRunner>();
        await scope.Entry.RunAsync(job.Id, [], settings, CancellationToken.None);

        return store.Get(job.Id)!;
    }

    [Test]
    public async Task Notification_is_sent_when_the_run_reaches_a_terminal_state()
    {
        var job = await RunJob();

        job.Status.Should().Be(SyncJobStatus.Succeeded);
        await _notify.Received().SendNotification();
    }

    [Test]
    public async Task Failure_to_notify_is_reported_as_a_diagnostic_on_the_finished_job()
    {
        _notify.SendNotification().ThrowsAsync(new InvalidOperationException("apprise is down"));

        var job = await RunJob();

        // Recorded alongside the terminal status so a client that polls once sees both.
        job.Status.Should().Be(SyncJobStatus.Succeeded);
        job.Diagnostics.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new SyncDiagnosticEvent(
                    null,
                    SyncDiagnosticLevel.Warning,
                    "Failed to send notification: apprise is down"
                )
            );
    }

    [TestCase(SyncResultStatus.Succeeded, SyncJobStatus.Succeeded)]
    [TestCase(SyncResultStatus.Partial, SyncJobStatus.Partial)]
    [TestCase(SyncResultStatus.Failed, SyncJobStatus.Failed)]
    public async Task Returned_result_is_retained_and_determines_job_status(
        SyncResultStatus resultStatus,
        SyncJobStatus expected
    )
    {
        var result = CreateRunResult(resultStatus);
        Resolve<ISyncOrchestrator>()
            .RunAsync(default!, default!, default)
            .ReturnsForAnyArgs(result);

        var job = await RunJob();

        job.Result.Should().BeSameAs(result);
        job.Status.Should().Be(expected);
    }

    [Test]
    public async Task Unexpected_run_failure_remains_an_operational_diagnostic()
    {
        var orchestrator = Resolve<ISyncOrchestrator>();
        orchestrator
            .RunAsync(
                Arg.Any<IReadOnlyList<IServiceConfiguration>>(),
                Arg.Any<ISyncSettings>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new InvalidOperationException("unexpected failure"));

        var job = await RunJob();

        job.Status.Should().Be(SyncJobStatus.Failed);
        job.Result.Should().NotBeNull();
        job.Result.Fault.Should().NotBeNull();
        var diagnostic = job.Diagnostics.Should().ContainSingle().Which;
        diagnostic
            .Should()
            .BeEquivalentTo(
                new SyncDiagnosticEvent(null, SyncDiagnosticLevel.Error, "unexpected failure")
            );
        diagnostic.Outcome.Should().BeNull();
    }

    private static SyncRunResult CreateRunResult(SyncResultStatus status) =>
        status switch
        {
            SyncResultStatus.Succeeded => new SyncRunResult([]),
            SyncResultStatus.Partial => new SyncRunResult(
                [new SyncInstanceResult("instance", SupportedServices.Radarr, [])],
                new SyncFault("fault")
            ),
            SyncResultStatus.Failed => new SyncRunResult([], new SyncFault("fault")),
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
}
