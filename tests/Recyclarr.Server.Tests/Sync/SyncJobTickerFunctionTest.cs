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
using TickerQ.Utilities.Base;

namespace Recyclarr.Server.Tests.Sync;

internal sealed class SyncJobTickerFunctionTest : ServerIntegrationFixture
{
    [Test]
    public async Task Terminal_job_is_not_run()
    {
        var store = Resolve<ISyncJobStore>();
        var job = CreateJob(store, Config("left-over"));
        store.InterruptActive();

        await Execute(Resolve<SyncRunScopeFactory>(), job.Id);

        await Resolve<ISyncOrchestrator>()
            .DidNotReceiveWithAnyArgs()
            .RunAsync(default!, default!, default!, default);
        store.Get(job.Id)?.Status.Should().Be(SyncJobStatus.Interrupted);
    }

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
        var store = Resolve<ISyncJobStore>();
        var job = CreateJob(store, Config("first"), Config("second"));

        await Execute(Resolve<SyncRunScopeFactory>(), job.Id);
        var completed = store.Get(job.Id)!;

        completed.Status.Should().Be(SyncJobStatus.Partial);
        completed.FaultReference.Should().NotBeNullOrWhiteSpace();
        completed
            .Progress.Instances.Select(instance => instance.Status)
            .Should()
            .Equal(InstanceProgressStatus.Succeeded, InstanceProgressStatus.Interrupted);
        completed.Progress.Instances[0].Result?.Name.Should().Be(firstResult.InstanceName);
    }

    [Test]
    public async Task Scope_setup_failure_finishes_with_a_fault_result()
    {
        using var scope = new ContainerBuilder().Build();
        var store = Resolve<ISyncJobStore>();
        var job = CreateJob(store, Config("not-started"));

        await Execute(new SyncRunScopeFactory(scope), job.Id);
        var completed = store.Get(job.Id)!;

        completed.Status.Should().Be(SyncJobStatus.Failed);
        completed.FaultReference.Should().NotBeNullOrWhiteSpace();
        completed
            .Progress.Instances.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(InstanceProgressStatus.NotRun);
    }

    [Test]
    public async Task Scope_cleanup_failure_preserves_the_completed_result()
    {
        var store = Resolve<ISyncJobStore>();
        var builder = new ContainerBuilder();
        builder.RegisterInstance(Substitute.For<ILogger>());
        builder.RegisterInstance(TimeProvider.System);
        builder.RegisterInstance(store).As<ISyncJobStore>();
        builder.RegisterInstance(Substitute.For<INotificationService>());
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
        var job = CreateJob(store);

        await Execute(new SyncRunScopeFactory(scope), job.Id);
        var completed = store.Get(job.Id)!;

        completed.Status.Should().Be(SyncJobStatus.Succeeded);
        completed.FaultReference.Should().BeNull();
    }

    // Publishes the configs as the server configuration and creates a job selecting all of them.
    private SyncJob CreateJob(ISyncJobStore store, params IServiceConfiguration[] configs)
    {
        Resolve<ServerConfigurationStore>().Publish(new ServerConfiguration(configs));
        return store.Create(
            new ServerSyncSettings(Service: null, Instances: [], Preview: false),
            [.. configs.Select(x => x.InstanceName)]
        );
    }

    private Task Execute(SyncRunScopeFactory scopes, JobId id)
    {
        var function = new SyncJobTickerFunction(
            Substitute.For<ILogger>(),
            Resolve<ISyncJobStore>(),
            Resolve<ServerConfigurationStore>(),
            new SyncExecutionGate(),
            scopes,
            Resolve<SyncJobFinalizer>()
        );
        var context = new TickerFunctionContext<SyncJobTickerRequest>(
            new TickerFunctionContext(),
            new SyncJobTickerRequest(id.Value)
        );
        return function.ExecuteAsync(context, CancellationToken.None);
    }

    private static RadarrConfiguration Config(string name) =>
        new()
        {
            InstanceName = name,
            BaseUrl = new Uri("http://localhost"),
            ApiKey = "api-key",
        };

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
