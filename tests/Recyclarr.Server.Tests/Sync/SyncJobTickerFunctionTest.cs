using System.Collections.Concurrent;
using System.Diagnostics;
using Autofac;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Notifications;
using Recyclarr.Server.Sync.Progress;
using Recyclarr.Server.Sync.Results;
using Recyclarr.Server.Telemetry;
using Recyclarr.Server.Tests.Reusable;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;
using TickerQ.Utilities.Base;

namespace Recyclarr.Server.Tests.Sync;

internal sealed class SyncJobTickerFunctionTest : ServerIntegrationFixture
{
    private static readonly AsyncLocal<string?> CallerState = new();

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

    // The request returns long before its job runs, so the job gets its own trace and links back to
    // the request; an ambient activity on the worker thread must not become its parent.
    [Test]
    public async Task Manual_job_runs_in_its_own_trace_linked_to_its_request()
    {
        using var spans = new RecordedSpans();
        using var request = new Activity("request");
        request.Start();
        var store = Resolve<ISyncJobStore>();
        Resolve<ServerConfigurationStore>().Publish(new ServerConfiguration([]));
        var job = store.Create(new ServerSyncSettings(null, [], Preview: false), [], request.Id);
        request.Stop();
        using var ambient = new Activity("worker");
        ambient.Start();

        await Execute(Resolve<SyncRunScopeFactory>(), job.Id);

        var span = spans.For(job);
        span.Kind.Should().Be(ActivityKind.Consumer);
        span.ParentSpanId.Should().Be(default(ActivitySpanId));
        span.TraceId.Should().NotBe(request.TraceId).And.NotBe(ambient.TraceId);
        span.Links.Should()
            .ContainSingle()
            .Which.Context.Should()
            .Be(new ActivityContext(request.TraceId, request.SpanId, request.ActivityTraceFlags));
    }

    // TickerQ workers inherit the execution context of whatever created them, often an HTTP
    // request. Its async-local state (logging scope, activity) must not reach the job.
    [Test]
    public async Task Job_does_not_see_the_async_local_state_of_its_caller()
    {
        string? seen = "not run";
        Resolve<ISyncOrchestrator>()
            .RunAsync(default!, default!, default!, default)
            .ReturnsForAnyArgs(_ =>
            {
                seen = CallerState.Value;
                return new SyncRunResult([]);
            });
        var job = CreateJob(Resolve<ISyncJobStore>(), Config("movies"));
        CallerState.Value = "request";

        await Execute(Resolve<SyncRunScopeFactory>(), job.Id);

        seen.Should().BeNull();
    }

    [Test]
    public async Task Scheduled_job_runs_in_its_own_trace_without_links()
    {
        using var spans = new RecordedSpans();
        Resolve<ServerConfigurationStore>().Publish(new ServerConfiguration([]));
        var job = Resolve<ISyncJobStore>()
            .CreateScheduled(
                new ServerSyncSettings(null, [], Preview: false),
                [],
                DateTimeOffset.UnixEpoch
            );

        await Execute(Resolve<SyncRunScopeFactory>(), job.Id);

        var span = spans.For(job);
        span.Links.Should().BeEmpty();
        span.GetTagItem("recyclarr.sync.job.trigger").Should().Be("scheduled");
    }

    [Test]
    public async Task Faulted_job_marks_its_span_as_an_error()
    {
        using var spans = new RecordedSpans();
        using var scope = new ContainerBuilder().Build();
        var job = CreateJob(Resolve<ISyncJobStore>(), Config("not-started"));

        await Execute(new SyncRunScopeFactory(scope), job.Id);

        spans.For(job).Status.Should().Be(ActivityStatusCode.Error);
    }

    // Publishes the configs as the server configuration and creates a job selecting all of them.
    private SyncJob CreateJob(ISyncJobStore store, params IServiceConfiguration[] configs)
    {
        Resolve<ServerConfigurationStore>().Publish(new ServerConfiguration(configs));
        return store.CreateManualJob([.. configs.Select(x => x.InstanceName)]);
    }

    private Task Execute(SyncRunScopeFactory scopes, JobId id)
    {
        var function = new SyncJobTickerFunction(
            Substitute.For<ILogger>(),
            Resolve<ISyncJobStore>(),
            Resolve<ServerConfigurationStore>(),
            new SyncExecutionGate(),
            scopes,
            Resolve<SyncJobFinalizer>(),
            Resolve<SyncJobTelemetry>()
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

    // Records spans from the Recyclarr source. Tests run in parallel, so each test looks up only
    // the span of its own job.
    private sealed class RecordedSpans : IDisposable
    {
        private readonly ConcurrentBag<Activity> _stopped = [];
        private readonly ActivityListener _listener;

        public RecordedSpans()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == ServerTelemetry.SourceName,
                Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = _stopped.Add,
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public Activity For(SyncJob job)
        {
            var id = job.Id.Value.ToString();
            return _stopped
                .Should()
                .ContainSingle(a => (string?)a.GetTagItem("recyclarr.sync.job.id") == id)
                .Subject;
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
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
