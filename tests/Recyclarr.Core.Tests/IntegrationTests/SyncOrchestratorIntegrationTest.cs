using Autofac;
using Autofac.Core;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using SemanticPipelineResult = Recyclarr.Sync.Results.PipelineResult;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class SyncOrchestratorIntegrationTest
{
    [Test]
    public async Task Faulted_instance_retains_work_and_later_instances_run()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);
        var configs = new IServiceConfiguration[]
        {
            Config("first"),
            Config("fault"),
            Config("last"),
        };

        var result = await sut.RunAsync(
            configs,
            Substitute.For<ISyncSettings>(),
            new NoopProgress(),
            CancellationToken.None
        );

        result.Status.Should().Be(SyncResultStatus.Partial);
        result.Instances.Select(x => x.InstanceName).Should().Equal("first", "fault", "last");
        result
            .Instances[0]
            .Pipelines.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(SyncResultStatus.Succeeded);
        result
            .Instances[1]
            .Pipelines.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(SyncResultStatus.Failed);
        var fault = result.Instances[1].Fault.Should().BeOfType<SyncFault>().Which;
        result.Instances[2].Status.Should().Be(SyncResultStatus.Succeeded);
        result.Fault.Should().BeNull();
        reporter.Reference.Should().Be(fault.Reference);
        reporter.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Test]
    public async Task Fault_reporter_failure_does_not_erase_result()
    {
        var reporter = new RecordingFaultReporter { ThrowOnReport = true };
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("fault"), Config("last")],
            Substitute.For<ISyncSettings>(),
            new NoopProgress(),
            CancellationToken.None
        );

        result.Fault.Should().BeNull();
        result.Instances.Should().HaveCount(2);
        result.Instances[0].Fault.Should().NotBeNull();
        result.Instances[1].InstanceName.Should().Be("last");
    }

    [Test]
    public async Task Fault_before_pipeline_execution_produces_faulted_instance_and_continues()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("early-fault"), Config("last")],
            Substitute.For<ISyncSettings>(),
            new NoopProgress(),
            CancellationToken.None
        );

        result.Status.Should().Be(SyncResultStatus.Partial);
        result.Instances.Select(x => x.InstanceName).Should().Equal("early-fault", "last");
        result.Instances[0].Pipelines.Should().BeEmpty();
        result.Instances[0].Fault.Should().NotBeNull();
        result.Fault.Should().BeNull();
    }

    [Test]
    public async Task Scope_creation_fault_produces_faulted_instance_and_continues()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("first"), Config("scope-fault"), Config("last")],
            Substitute.For<ISyncSettings>(),
            new NoopProgress(),
            CancellationToken.None
        );

        result.Instances.Select(x => x.InstanceName).Should().Equal("first", "scope-fault", "last");
        result.Instances[1].Fault.Should().NotBeNull();
        result.Fault.Should().BeNull();
        reporter.Exception.Should().BeOfType<DependencyResolutionException>();
    }

    [Test]
    public async Task Scope_disposal_fault_retains_completed_instance_and_continues()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("dispose-fault"), Config("last")],
            Substitute.For<ISyncSettings>(),
            new NoopProgress(),
            CancellationToken.None
        );

        result.Instances.Select(x => x.InstanceName).Should().Equal("dispose-fault", "last");
        result.Instances[0].Pipelines.Should().ContainSingle();
        result.Instances[0].Fault.Should().NotBeNull();
        result.Fault.Should().BeNull();
        reporter.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Test]
    public async Task Cleanup_fault_retains_expected_operational_failure()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("incompatible-dispose-fault"), Config("last")],
            Substitute.For<ISyncSettings>(),
            new NoopProgress(),
            CancellationToken.None
        );

        result.Instances[0].Failure.Should().BeOfType<ServiceIncompatibleFailure>();
        result.Instances[0].Fault.Should().NotBeNull();
        result.Instances[1].Status.Should().Be(SyncResultStatus.Succeeded);
    }

    [Test]
    public async Task Two_faulted_instances_each_retain_their_own_fault()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("early-fault"), Config("fault"), Config("last")],
            Substitute.For<ISyncSettings>(),
            new NoopProgress(),
            CancellationToken.None
        );

        result.Instances.Should().HaveCount(3);
        result.Instances[0].Fault.Should().NotBeNull();
        result.Instances[1].Fault.Should().NotBeNull();
        result.Instances[0].Fault.Should().NotBe(result.Instances[1].Fault);
        result.Instances[2].Status.Should().Be(SyncResultStatus.Succeeded);
        reporter.Reports.Should().HaveCount(2);
    }

    [Test]
    public async Task Processing_and_disposal_faults_are_reported_together()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("fault-dispose-fault"), Config("last")],
            Substitute.For<ISyncSettings>(),
            new NoopProgress(),
            CancellationToken.None
        );

        result.Instances[0].Fault.Should().NotBeNull();
        result.Instances[1].Status.Should().Be(SyncResultStatus.Succeeded);
        reporter
            .Exception.Should()
            .BeOfType<AggregateException>()
            .Which.InnerExceptions.Should()
            .SatisfyRespectively(
                exception => exception.Should().BeOfType<InvalidOperationException>(),
                exception => exception.Should().BeOfType<InvalidOperationException>()
            );
    }

    [Test]
    public async Task Fault_after_planning_retains_planning_outcomes()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("planning-fault")],
            Substitute.For<ISyncSettings>(),
            new NoopProgress(),
            CancellationToken.None
        );

        result
            .Instances.Should()
            .ContainSingle()
            .Which.PlanningOutcomes.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<TestPlanningOutcome>();
    }

    [Test]
    public async Task Expected_instance_failure_allows_later_instances_to_run()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("incompatible"), Config("second")],
            Substitute.For<ISyncSettings>(),
            new NoopProgress(),
            CancellationToken.None
        );

        result.Status.Should().Be(SyncResultStatus.Partial);
        result.Instances.Should().HaveCount(2);
        result.Instances[0].Failure.Should().BeOfType<ServiceIncompatibleFailure>();
        result.Instances[1].Status.Should().Be(SyncResultStatus.Succeeded);
        result.Fault.Should().BeNull();
    }

    [Test]
    public async Task Lifecycle_callbacks_are_ordered_and_completion_follows_cleanup()
    {
        var reporter = new RecordingFaultReporter();
        var recorder = new ExecutionRecorder();
        var progress = new RecordingProgress(recorder);
        using var container = BuildContainer(reporter, recorder);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("first")],
            Substitute.For<ISyncSettings>(),
            progress,
            CancellationToken.None
        );

        recorder
            .Events.Should()
            .Equal("started:first", "executed:first", "disposed:first", "completed:first");
        progress.Completed.Should().ContainSingle().Which.Should().BeSameAs(result.Instances[0]);
    }

    [Test]
    public async Task Faulted_instance_reports_completion_before_later_instance_starts()
    {
        var reporter = new RecordingFaultReporter();
        var recorder = new ExecutionRecorder();
        var progress = new RecordingProgress(recorder);
        using var container = BuildContainer(reporter, recorder);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        await sut.RunAsync(
            [Config("early-fault"), Config("last")],
            Substitute.For<ISyncSettings>(),
            progress,
            CancellationToken.None
        );

        recorder
            .Events.Should()
            .Equal(
                "started:early-fault",
                "executed:early-fault",
                "disposed:early-fault",
                "completed:early-fault",
                "started:last",
                "executed:last",
                "disposed:last",
                "completed:last"
            );
        progress.Completed[0].Fault.Should().NotBeNull();
    }

    [Test]
    public async Task Empty_run_reports_no_lifecycle_callbacks()
    {
        var reporter = new RecordingFaultReporter();
        var progress = new RecordingProgress(new ExecutionRecorder());
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [],
            Substitute.For<ISyncSettings>(),
            progress,
            CancellationToken.None
        );

        result.Instances.Should().BeEmpty();
        progress.Completed.Should().BeEmpty();
        progress.Started.Should().BeEmpty();
    }

    [Test]
    public async Task Lifecycle_callback_failures_do_not_change_execution_or_results()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);
        var progress = new RecordingProgress(new ExecutionRecorder()) { ThrowOnReport = true };

        var result = await sut.RunAsync(
            [Config("first"), Config("last")],
            Substitute.For<ISyncSettings>(),
            progress,
            CancellationToken.None
        );

        result.Instances.Select(instance => instance.InstanceName).Should().Equal("first", "last");
        result.Status.Should().Be(SyncResultStatus.Succeeded);
    }

    [Test]
    public async Task Cancellation_with_cleanup_fault_propagates_without_starting_later_instances()
    {
        var reporter = new RecordingFaultReporter();
        var recorder = new ExecutionRecorder();
        var progress = new RecordingProgress(recorder);
        using var cancellation = new CancellationTokenSource();
        using var container = BuildContainer(reporter, recorder, cancellation);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var act = () =>
            sut.RunAsync(
                [Config("cancel-dispose-fault"), Config("never-started")],
                Substitute.For<ISyncSettings>(),
                progress,
                cancellation.Token
            );

        await act.Should().ThrowAsync<OperationCanceledException>();
        reporter.Exception.Should().BeOfType<InvalidOperationException>();
        recorder
            .Events.Should()
            .Equal(
                "started:cancel-dispose-fault",
                "executed:cancel-dispose-fault",
                "disposed:cancel-dispose-fault"
            );
        progress.Completed.Should().BeEmpty();
    }

    [Test]
    public async Task Unsignaled_processing_cancellation_is_isolated_as_an_instance_fault()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("unsignaled-cancellation"), Config("last")],
            Substitute.For<ISyncSettings>(),
            new NoopProgress(),
            CancellationToken.None
        );

        result
            .Instances.Select(x => x.InstanceName)
            .Should()
            .Equal("unsignaled-cancellation", "last");
        result.Instances[0].Fault.Should().NotBeNull();
        result.Instances[1].Status.Should().Be(SyncResultStatus.Succeeded);
        reporter.Exception.Should().BeOfType<OperationCanceledException>();
    }

    [Test]
    public async Task Signaled_cleanup_cancellation_propagates_without_completing_the_instance()
    {
        var reporter = new RecordingFaultReporter();
        var progress = new RecordingProgress(new ExecutionRecorder());
        using var cancellation = new CancellationTokenSource();
        using var container = BuildContainer(reporter, cancellation: cancellation);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var act = () =>
            sut.RunAsync(
                [Config("cleanup-cancellation"), Config("never-started")],
                Substitute.For<ISyncSettings>(),
                progress,
                cancellation.Token
            );

        await act.Should().ThrowAsync<OperationCanceledException>();
        progress.Started.Should().Equal("cleanup-cancellation");
        progress.Completed.Should().BeEmpty();
    }

    [Test]
    public async Task Unsignaled_cleanup_cancellation_is_isolated_as_an_instance_fault()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("unsignaled-cleanup-cancellation"), Config("last")],
            Substitute.For<ISyncSettings>(),
            new NoopProgress(),
            CancellationToken.None
        );

        result
            .Instances.Select(x => x.InstanceName)
            .Should()
            .Equal("unsignaled-cleanup-cancellation", "last");
        result.Instances[0].Fault.Should().NotBeNull();
        result.Instances[1].Status.Should().Be(SyncResultStatus.Succeeded);
        reporter.Exception.Should().BeOfType<OperationCanceledException>();
    }

    private static IContainer BuildContainer(
        ISyncFaultReporter reporter,
        ExecutionRecorder? recorder = null,
        CancellationTokenSource? cancellation = null
    )
    {
        var radarrCapabilities = Substitute.For<IRadarrCapabilityFetcher>();
        radarrCapabilities
            .GetCapabilities(default)
            .ReturnsForAnyArgs(new RadarrCapabilities(new Version(6, 0)));

        var builder = new ContainerBuilder();
        builder.RegisterInstance(Substitute.For<ILogger>());
        builder.RegisterType<TestServiceInformation>().As<IServiceInformation>();
        builder.RegisterInstance(radarrCapabilities).As<IRadarrCapabilityFetcher>();
        builder.RegisterInstance(Substitute.For<ISonarrCapabilityFetcher>());
        builder.RegisterInstance(recorder ?? new ExecutionRecorder());
        if (cancellation is null)
        {
            builder.RegisterType<CancellationTokenSource>().SingleInstance();
        }
        else
        {
            builder.RegisterInstance(cancellation).ExternallyOwned();
        }
        builder
            .Register(c =>
                new IPlanComponent[]
                {
                    new TestPlanningComponent(c.Resolve<IServiceConfiguration>()),
                }.OrderBy(_ => 0)
            )
            .As<IOrderedEnumerable<IPlanComponent>>();
        builder.RegisterInstance(reporter).As<ISyncFaultReporter>();
        builder.RegisterType<RadarrCapabilityEnforcer>();
        builder.RegisterType<SonarrCapabilityEnforcer>();
        builder.RegisterType<ServiceAgnosticCapabilityEnforcer>();
        builder.RegisterType<PlanBuilder>();
        builder.RegisterType<TestPipelineExecutor>().As<IPipelineExecutor>();
        builder.RegisterType<InstanceSyncProcessor>();
        return builder.Build();
    }

    private static RadarrConfiguration Config(string name) =>
        new()
        {
            InstanceName = name,
            BaseUrl = new Uri("http://localhost"),
            ApiKey = "api-key",
        };

    private sealed class TestPipelineExecutor : IPipelineExecutor, IDisposable
    {
        private readonly IServiceConfiguration _config;
        private readonly ExecutionRecorder _recorder;
        private readonly CancellationTokenSource _cancellation;

        public TestPipelineExecutor(
            IServiceConfiguration config,
            ExecutionRecorder recorder,
            CancellationTokenSource cancellation
        )
        {
            if (config.InstanceName == "scope-fault")
            {
                throw new InvalidOperationException("scope creation failed");
            }

            _config = config;
            _recorder = recorder;
            _cancellation = cancellation;
        }

        public async Task<IReadOnlyList<SemanticPipelineResult>> Execute(
            ISyncSettings settings,
            PipelinePlan plan,
            PipelineExecutionBuffer buffer,
            CancellationToken ct
        )
        {
            _recorder.Events.Add($"executed:{_config.InstanceName}");

            if (_config.InstanceName == "cancel-dispose-fault")
            {
                await _cancellation.CancelAsync();
                throw new OperationCanceledException(ct);
            }

            if (_config.InstanceName == "unsignaled-cancellation")
            {
                throw new OperationCanceledException(ct);
            }

            if (_config.InstanceName == "cleanup-cancellation")
            {
                await _cancellation.CancelAsync();
            }

            if (_config.InstanceName is "early-fault" or "planning-fault")
            {
                throw new InvalidOperationException("unexpected");
            }

            var status = _config.InstanceName is "fault" or "fault-dispose-fault"
                ? SyncResultStatus.Failed
                : SyncResultStatus.Succeeded;
            var result = new TestPipelineResult(status);
            buffer.Capture(PipelineType.CustomFormat, result);

            if (_config.InstanceName is "fault" or "fault-dispose-fault")
            {
                throw new InvalidOperationException("unexpected");
            }

            return [result];
        }

        public void Dispose()
        {
            _recorder.Events.Add($"disposed:{_config.InstanceName}");

            if (
                _config.InstanceName
                is "dispose-fault"
                    or "fault-dispose-fault"
                    or "incompatible-dispose-fault"
                    or "cancel-dispose-fault"
            )
            {
                throw new InvalidOperationException("scope disposal failed");
            }

            if (_config.InstanceName is "cleanup-cancellation" or "unsignaled-cleanup-cancellation")
            {
                throw new OperationCanceledException();
            }
        }
    }

    private sealed class TestPlanningComponent(IServiceConfiguration config) : IPlanComponent
    {
        public void Process(PipelinePlan plan)
        {
            if (config.InstanceName == "planning-fault")
            {
                plan.AddPlanningOutcome(new TestPlanningOutcome());
            }
        }
    }

    private sealed record TestPlanningOutcome : PlanningOutcome;

    private sealed class ExecutionRecorder
    {
        public List<string> Events { get; } = [];
    }

    private sealed class RecordingProgress(ExecutionRecorder recorder) : IInstanceSyncProgress
    {
        public bool ThrowOnReport { get; init; }
        public List<string> Started { get; } = [];
        public List<SyncInstanceResult> Completed { get; } = [];

        public void InstanceStarted(string instanceName)
        {
            recorder.Events.Add($"started:{instanceName}");
            Started.Add(instanceName);
            ThrowIfRequested();
        }

        public void InstanceCompleted(SyncInstanceResult result)
        {
            recorder.Events.Add($"completed:{result.InstanceName}");
            Completed.Add(result);
            ThrowIfRequested();
        }

        private void ThrowIfRequested()
        {
            if (ThrowOnReport)
            {
                throw new InvalidOperationException("progress failed");
            }
        }
    }

    private sealed class NoopProgress : IInstanceSyncProgress
    {
        public void InstanceStarted(string instanceName) { }

        public void InstanceCompleted(SyncInstanceResult result) { }
    }

    private sealed class TestServiceInformation(IServiceConfiguration config) : IServiceInformation
    {
        public Task<string> GetAppName(CancellationToken ct) =>
            Task.FromResult(
                config.InstanceName is "incompatible" or "incompatible-dispose-fault"
                    ? "Sonarr"
                    : "Radarr"
            );

        public Task<Version> GetVersion(CancellationToken ct) => Task.FromResult(new Version(6, 0));
    }

    private sealed record TestPipelineResult : SemanticPipelineResult
    {
        public TestPipelineResult(SyncResultStatus status)
            : base(status) { }

        internal override SemanticPipelineResult WithStatus(
            SyncResultStatus status,
            PipelineType? blockedBy = null
        ) => new TestPipelineResult(status);
    }

    private sealed class RecordingFaultReporter : ISyncFaultReporter
    {
        public bool ThrowOnReport { get; init; }
        public string? Reference { get; private set; }
        public Exception? Exception { get; private set; }
        public List<(string Reference, Exception Exception)> Reports { get; } = [];

        public void Report(string reference, Exception exception)
        {
            Reference = reference;
            Exception = exception;
            Reports.Add((reference, exception));

            if (ThrowOnReport)
            {
                throw new IOException("reporting failed");
            }
        }
    }
}
