using Autofac;
using NSubstitute.ExceptionExtensions;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Notifications;
using Recyclarr.Server.Sync.Progress;
using Recyclarr.Server.Tests.Reusable;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;
using Serilog.Events;

namespace Recyclarr.Server.Tests.Sync;

internal sealed class SyncJobRunnerTest : ServerIntegrationFixture
{
    private readonly INotificationService _notify = Substitute.For<INotificationService>();
    private readonly RecordingLogger _log = new();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
        builder.RegisterInstance(_log).As<ILogger>();
        builder.RegisterInstance(_notify).As<INotificationService>();
    }

    private async Task<SyncJob> RunJob(IReadOnlyList<IServiceConfiguration>? configs = null)
    {
        configs ??= [];
        var settings = new ServerSyncSettings(
            Service: null,
            Instances: [],
            Preview: false,
            Configs: []
        );

        var store = Resolve<ISyncJobStore>();
        var job = store.Create(settings, configs.Select(config => config.InstanceName).ToList());

        using var scope = Resolve<SyncRunScopeFactory>().Start<SyncJobRunner>();
        await scope.Entry.RunAsync(job.Id, configs, settings, CancellationToken.None);

        return store.Get(job.Id)!;
    }

    [Test]
    public async Task Notification_is_sent_when_the_run_reaches_a_terminal_state()
    {
        var job = await RunJob();

        job.Status.Should().Be(SyncJobStatus.Succeeded);
        await _notify.Received().SendNotification(job.Result!);
    }

    [Test]
    public async Task Failure_to_notify_does_not_change_the_finished_job()
    {
        _notify
            .SendNotification(default!)
            .ThrowsAsyncForAnyArgs(new InvalidOperationException("apprise is down"));

        var job = await RunJob();

        job.Status.Should().Be(SyncJobStatus.Succeeded);
        job.Result.Should().NotBeNull();
        await _notify.Received().SendNotification(job.Result!);
        _log.Events.Should()
            .ContainSingle(evt =>
                evt.Level == LogEventLevel.Warning
                && evt.MessageTemplate.Text.Equals(
                    "Failed to send notification",
                    StringComparison.Ordinal
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
            .RunAsync(default!, default!, default!, default)
            .ReturnsForAnyArgs(result);

        var job = await RunJob();

        job.Result.Should().BeSameAs(result);
        job.Status.Should().Be(expected);
    }

    [Test]
    public async Task Unexpected_run_failure_is_retained_and_logged()
    {
        var firstResult = new SyncInstanceResult("first", SupportedServices.Radarr, []);
        var orchestrator = Resolve<ISyncOrchestrator>();
        orchestrator
            .RunAsync(
                Arg.Any<IReadOnlyList<IServiceConfiguration>>(),
                Arg.Any<ISyncSettings>(),
                Arg.Any<IInstanceSyncProgress>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(call =>
            {
                var progress = call.Arg<IInstanceSyncProgress>();
                progress.InstanceStarted("first");
                progress.InstanceCompleted(firstResult);
                progress.InstanceStarted("second");
                return Task.FromException<SyncRunResult>(
                    new InvalidOperationException("unexpected failure")
                );
            });

        var job = await RunJob([Config("first"), Config("second")]);

        job.Status.Should().Be(SyncJobStatus.Partial);
        job.Result.Should().NotBeNull();
        job.Result.Fault.Should().NotBeNull();
        job.Result.Instances.Should().ContainSingle().Which.Should().BeSameAs(firstResult);
        job.Progress.Instances.Select(instance => instance.Status)
            .Should()
            .Equal(InstanceProgressStatus.Succeeded, InstanceProgressStatus.Interrupted);
        _log.Events.Should()
            .ContainSingle(evt =>
                evt.Level == LogEventLevel.Error
                && evt.Exception != null
                && evt.Exception.Message == "unexpected failure"
                && evt.MessageTemplate.Text.Equals(
                    "Unexpected sync runner fault {Reference}",
                    StringComparison.Ordinal
                )
            );
    }

    [Test]
    public async Task Store_exposes_cumulative_progress_between_instances()
    {
        var firstResult = new SyncInstanceResult("first", SupportedServices.Radarr, []);
        var secondResult = new SyncInstanceResult("second", SupportedServices.Sonarr, []);
        var firstStarted = NewSignal();
        var releaseFirst = NewSignal();
        var secondStarted = NewSignal();
        var releaseSecond = NewSignal();
        Resolve<ISyncOrchestrator>()
            .RunAsync(default!, default!, default!, default)
            .ReturnsForAnyArgs(call =>
                ExecuteControlled(
                    call.ArgAt<IInstanceSyncProgress>(2),
                    firstResult,
                    secondResult,
                    firstStarted,
                    releaseFirst,
                    secondStarted,
                    releaseSecond
                )
            );
        var configs = new[] { Config("first"), Config("second", SupportedServices.Sonarr) };
        var settings = new ServerSyncSettings(null, [], Preview: false, []);
        var store = Resolve<ISyncJobStore>();
        var job = store.Create(settings, ["first", "second"]);

        using var scope = Resolve<SyncRunScopeFactory>().Start<SyncJobRunner>();
        var run = scope.Entry.RunAsync(job.Id, configs, settings, CancellationToken.None);

        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        store
            .Get(job.Id)!
            .Progress.Instances.Select(instance => instance.Status)
            .Should()
            .Equal(InstanceProgressStatus.Running, InstanceProgressStatus.Pending);

        releaseFirst.SetResult();
        await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var between = store.Get(job.Id)!;
        between
            .Progress.Instances.Select(instance => instance.Status)
            .Should()
            .Equal(InstanceProgressStatus.Succeeded, InstanceProgressStatus.Running);
        between.Progress.Instances[0].Result.Should().BeSameAs(firstResult);

        releaseSecond.SetResult();
        await run;
        var completed = store.Get(job.Id)!;
        completed.Status.Should().Be(SyncJobStatus.Succeeded);
        completed
            .Progress.Instances.Select(instance => instance.Status)
            .Should()
            .Equal(InstanceProgressStatus.Succeeded, InstanceProgressStatus.Succeeded);
    }

    [Test]
    public async Task Terminal_reconciliation_recovers_a_missed_completion_callback()
    {
        var result = new SyncRunResult([
            new SyncInstanceResult("instance", SupportedServices.Radarr, []),
        ]);
        Resolve<ISyncOrchestrator>()
            .RunAsync(default!, default!, default!, default)
            .ReturnsForAnyArgs(result);

        var job = await RunJob([Config("instance")]);

        var instance = job.Progress.Instances.Should().ContainSingle().Which;
        instance.Status.Should().Be(InstanceProgressStatus.Succeeded);
        instance.Result.Should().BeSameAs(result.Instances[0]);
    }

    private static async Task<SyncRunResult> ExecuteControlled(
        IInstanceSyncProgress progress,
        SyncInstanceResult firstResult,
        SyncInstanceResult secondResult,
        TaskCompletionSource firstStarted,
        TaskCompletionSource releaseFirst,
        TaskCompletionSource secondStarted,
        TaskCompletionSource releaseSecond
    )
    {
        progress.InstanceStarted(firstResult.InstanceName);
        firstStarted.SetResult();
        await releaseFirst.Task;
        progress.InstanceCompleted(firstResult);
        progress.InstanceStarted(secondResult.InstanceName);
        secondStarted.SetResult();
        await releaseSecond.Task;
        progress.InstanceCompleted(secondResult);
        return new SyncRunResult([firstResult, secondResult]);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static IServiceConfiguration Config(
        string name,
        SupportedServices service = SupportedServices.Radarr
    ) =>
        service switch
        {
            SupportedServices.Radarr => new RadarrConfiguration
            {
                InstanceName = name,
                BaseUrl = new Uri("http://localhost"),
                ApiKey = "api-key",
            },
            SupportedServices.Sonarr => new SonarrConfiguration
            {
                InstanceName = name,
                BaseUrl = new Uri("http://localhost"),
                ApiKey = "api-key",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
        };

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

    private sealed class RecordingLogger : ILogger
    {
        public List<LogEvent> Events { get; } = [];

        public void Write(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}
