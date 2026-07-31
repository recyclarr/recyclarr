using Autofac;
using Autofac.Core;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.ErrorHandling;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using SemanticPipelineResult = Recyclarr.Sync.Results.PipelineResult;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class SyncOrchestratorIntegrationTest
{
    [Test]
    public async Task Later_fault_retains_ordered_completed_results()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);
        var configs = new IServiceConfiguration[]
        {
            Config("first"),
            Config("fault"),
            Config("never-started"),
        };

        var result = await sut.RunAsync(
            configs,
            Substitute.For<ISyncSettings>(),
            CancellationToken.None
        );

        result.Status.Should().Be(SyncResultStatus.Partial);
        result.Instances.Select(x => x.InstanceName).Should().Equal("first", "fault");
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
        result.Fault.Should().NotBeNull();
        reporter.Reference.Should().Be(result.Fault.Reference);
        reporter.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Test]
    public async Task Fault_reporter_failure_does_not_erase_result()
    {
        var reporter = new RecordingFaultReporter { ThrowOnReport = true };
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("fault")],
            Substitute.For<ISyncSettings>(),
            CancellationToken.None
        );

        result.Fault.Should().NotBeNull();
        result.Instances.Should().ContainSingle();
    }

    [Test]
    public async Task Fault_before_pipeline_execution_does_not_invent_an_instance_result()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("early-fault")],
            Substitute.For<ISyncSettings>(),
            CancellationToken.None
        );

        result.Status.Should().Be(SyncResultStatus.Failed);
        result.Instances.Should().BeEmpty();
        result.Fault.Should().NotBeNull();
    }

    [Test]
    public async Task Scope_creation_fault_retains_prior_completed_instances()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("first"), Config("scope-fault")],
            Substitute.For<ISyncSettings>(),
            CancellationToken.None
        );

        result.Instances.Should().ContainSingle().Which.InstanceName.Should().Be("first");
        result.Fault.Should().NotBeNull();
        reporter.Exception.Should().BeOfType<DependencyResolutionException>();
    }

    [Test]
    public async Task Scope_disposal_fault_retains_completed_instance()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var result = await sut.RunAsync(
            [Config("dispose-fault")],
            Substitute.For<ISyncSettings>(),
            CancellationToken.None
        );

        result.Instances.Should().ContainSingle().Which.InstanceName.Should().Be("dispose-fault");
        result.Fault.Should().NotBeNull();
        reporter.Exception.Should().BeOfType<InvalidOperationException>();
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
            CancellationToken.None
        );

        result.Status.Should().Be(SyncResultStatus.Partial);
        result.Instances.Should().HaveCount(2);
        result.Instances[0].Failure.Should().BeOfType<ServiceIncompatibleFailure>();
        result.Instances[1].Status.Should().Be(SyncResultStatus.Succeeded);
        result.Fault.Should().BeNull();
    }

    [Test]
    public async Task Cancellation_propagates_without_starting_later_instances()
    {
        var reporter = new RecordingFaultReporter();
        using var container = BuildContainer(reporter);
        var sut = new SyncOrchestrator(new InstanceScopeFactory(container), reporter);

        var act = () =>
            sut.RunAsync(
                [Config("cancel"), Config("never-started")],
                Substitute.For<ISyncSettings>(),
                CancellationToken.None
            );

        await act.Should().ThrowAsync<OperationCanceledException>();
        reporter.Reference.Should().BeNull();
    }

    private static IContainer BuildContainer(ISyncFaultReporter reporter)
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
        builder.RegisterInstance(Substitute.For<IInstancePublisher>());
        builder.RegisterInstance(Array.Empty<IPlanComponent>().OrderBy(_ => 0));
        builder.RegisterInstance(Array.Empty<IExceptionStrategy>().AsEnumerable());
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

        public TestPipelineExecutor(IServiceConfiguration config)
        {
            if (config.InstanceName == "scope-fault")
            {
                throw new InvalidOperationException("scope creation failed");
            }

            _config = config;
        }

        public Task<IReadOnlyList<SemanticPipelineResult>> Execute(
            ISyncSettings settings,
            PipelinePlan plan,
            IInstancePublisher instancePublisher,
            PipelineExecutionBuffer buffer,
            CancellationToken ct
        )
        {
            if (_config.InstanceName == "cancel")
            {
                throw new OperationCanceledException(ct);
            }

            if (_config.InstanceName == "early-fault")
            {
                throw new InvalidOperationException("unexpected");
            }

            var status =
                _config.InstanceName == "fault"
                    ? SyncResultStatus.Failed
                    : SyncResultStatus.Succeeded;
            var result = new TestPipelineResult(status);
            buffer.Capture(PipelineType.CustomFormat, result);

            return _config.InstanceName == "fault"
                ? Task.FromException<IReadOnlyList<SemanticPipelineResult>>(
                    new InvalidOperationException("unexpected")
                )
                : Task.FromResult<IReadOnlyList<SemanticPipelineResult>>([result]);
        }

        public void InterruptAll(IInstancePublisher instancePublisher) { }

        public void Dispose()
        {
            if (_config.InstanceName == "dispose-fault")
            {
                throw new InvalidOperationException("scope disposal failed");
            }
        }
    }

    private sealed class TestServiceInformation(IServiceConfiguration config) : IServiceInformation
    {
        public Task<string> GetAppName(CancellationToken ct) =>
            Task.FromResult(config.InstanceName == "incompatible" ? "Sonarr" : "Radarr");

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

        public void Report(string reference, Exception exception)
        {
            Reference = reference;
            Exception = exception;

            if (ThrowOnReport)
            {
                throw new IOException("reporting failed");
            }
        }
    }
}
