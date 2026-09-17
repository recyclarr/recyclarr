using System.Diagnostics;
using Autofac;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Notifications;
using Recyclarr.Server.Sync.Progress;
using Recyclarr.Server.Sync.Results;
using Recyclarr.Server.Tests.Reusable;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Tests.Sync;

internal sealed class SyncJobLauncherTest : ServerIntegrationFixture
{
    [Test]
    public async Task Canceled_run_finishes_with_a_fault_result()
    {
        var firstResult = new SyncInstanceResult("first", SupportedServices.Radarr, []);
        Resolve<ISyncOrchestrator>()
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
                return Task.FromException<SyncRunResult>(new OperationCanceledException());
            });
        var launcher = Resolve<SyncJobLauncher>();

        var job = launcher.Launch(NewSettings(), [Config("first"), Config("second")]);
        var completed = await WaitForCompletion(Resolve<ISyncJobStore>(), job.Id);

        completed.Status.Should().Be(SyncJobStatus.Partial);
        completed.Result?.Fault?.Reference.Should().NotBeNullOrWhiteSpace();
        completed.Result?.Instances.Should().ContainSingle().Which.Should().BeSameAs(firstResult);
        completed
            .Progress.Instances.Select(instance => instance.Status)
            .Should()
            .Equal(InstanceProgressStatus.Succeeded, InstanceProgressStatus.Interrupted);
    }

    [Test]
    public async Task Scope_setup_failure_finishes_with_a_fault_result()
    {
        using var scope = new ContainerBuilder().Build();
        var store = new InMemorySyncJobStore();
        var launcher = new SyncJobLauncher(
            Substitute.For<ILogger>(),
            store,
            new SyncRunScopeFactory(scope),
            new SyncJobFinalizer(store)
        );

        var job = launcher.Launch(NewSettings(), [Config("not-started")]);
        var completed = await WaitForCompletion(store, job.Id);

        completed.Status.Should().Be(SyncJobStatus.Failed);
        completed.Result?.Fault?.Reference.Should().NotBeNullOrWhiteSpace();
        completed
            .Progress.Instances.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(InstanceProgressStatus.NotRun);
    }

    [Test]
    public async Task Scope_cleanup_failure_preserves_the_completed_result()
    {
        var store = new InMemorySyncJobStore();
        var builder = new ContainerBuilder();
        builder.RegisterInstance(Substitute.For<ILogger>());
        builder.RegisterInstance(store).As<ISyncJobStore>();
        builder.RegisterInstance(Substitute.For<INotificationService>());
        builder.RegisterType<SyncRunScope>().AsImplementedInterfaces().InstancePerLifetimeScope();
        builder.RegisterType<SyncDiagnosticsLogger>();
        builder.RegisterType<SyncResultLogger>();
        builder.RegisterType<SyncJobFinalizer>();
        builder.RegisterType<SyncJobProgress>();
        builder.RegisterType<SyncJobRunner>().InstancePerMatchingLifetimeScope("run");
        builder
            .Register(_ => new SuccessfulOrchestrator())
            .As<ISyncOrchestrator>()
            .InstancePerLifetimeScope()
            .OnRelease(_ => throw new InvalidOperationException("cleanup secret"));
        using var scope = builder.Build();
        var log = Substitute.For<ILogger>();
        var launcher = new SyncJobLauncher(
            log,
            store,
            new SyncRunScopeFactory(scope),
            new SyncJobFinalizer(store)
        );

        var job = launcher.Launch(NewSettings(), []);
        var completed = await WaitForCompletion(store, job.Id);

        completed.Status.Should().Be(SyncJobStatus.Succeeded);
        completed.Result.Should().NotBeNull();
        completed.Result?.Fault.Should().BeNull();
    }

    private static ServerSyncSettings NewSettings() =>
        new(Service: null, Instances: [], Preview: false, Configs: []);

    private static RadarrConfiguration Config(string name) =>
        new()
        {
            InstanceName = name,
            BaseUrl = new Uri("http://localhost"),
            ApiKey = "api-key",
        };

    private static async Task<SyncJob> WaitForCompletion(ISyncJobStore store, JobId id)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < TimeSpan.FromSeconds(5))
        {
            var job = store.Get(id);
            if (job?.Status.IsTerminal() is true)
            {
                return job;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException("Sync job did not finish");
    }

    private sealed class SuccessfulOrchestrator : ISyncOrchestrator
    {
        public Task<SyncRunResult> RunAsync(
            IReadOnlyList<IServiceConfiguration> configs,
            ISyncSettings settings,
            IInstanceSyncProgress progress,
            CancellationToken ct
        ) => Task.FromResult(new SyncRunResult([]));
    }
}
