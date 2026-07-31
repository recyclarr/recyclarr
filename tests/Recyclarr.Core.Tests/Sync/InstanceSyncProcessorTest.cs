using NSubstitute.ExceptionExtensions;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ErrorHandling;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Serilog.Events;

namespace Recyclarr.Core.Tests.Sync;

internal sealed class InstanceSyncProcessorTest
{
    [Test]
    public async Task Handled_failure_is_retained_and_interrupts_pipelines()
    {
        var exception = new InvalidOperationException("handled");
        var serviceInfo = Substitute.For<IServiceInformation>();
        serviceInfo.GetAppName(default).ThrowsAsync(exception);
        var failure = new GitFailure(ExitCode: 2);
        var strategy = Substitute.For<IExceptionStrategy>();
        strategy.HandleAsync(exception).Returns(failure);
        var log = new RecordingLogger();
        var publisher = new RecordingInstancePublisher();
        var pipelines = new RecordingPipelineExecutor();
        var sut = CreateSut(log, serviceInfo, publisher, pipelines, [strategy]);

        var result = await sut.Process(Substitute.For<ISyncSettings>(), CancellationToken.None);

        result.Should().Be(ExitStatus.Failed);
        publisher.Outcomes.Should().ContainSingle().Which.Should().BeSameAs(failure);
        pipelines.Interrupted.Should().BeTrue();
        log.Events.Where(evt => evt.Level == LogEventLevel.Debug)
            .Should()
            .ContainSingle()
            .Which.Exception.Should()
            .BeSameAs(exception);
    }

    [Test]
    public async Task Unexpected_failure_is_rethrown_without_an_outcome()
    {
        var exception = new InvalidOperationException("unexpected");
        var serviceInfo = Substitute.For<IServiceInformation>();
        serviceInfo.GetAppName(default).ThrowsAsync(exception);
        var log = new RecordingLogger();
        var publisher = new RecordingInstancePublisher();
        var pipelines = new RecordingPipelineExecutor();
        var sut = CreateSut(log, serviceInfo, publisher, pipelines, []);

        var act = () => sut.Process(Substitute.For<ISyncSettings>(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("unexpected");
        publisher.Outcomes.Should().BeEmpty();
        pipelines.Interrupted.Should().BeFalse();
        log.Events.Should().NotContain(evt => evt.Exception == exception);
    }

    private static InstanceSyncProcessor CreateSut(
        ILogger log,
        IServiceInformation serviceInfo,
        IInstancePublisher publisher,
        IPipelineExecutor pipelines,
        IEnumerable<IExceptionStrategy> strategies
    )
    {
        IPlanComponent[] components = [];
        var planBuilder = new PlanBuilder(components.OrderBy(_ => 0), publisher, log);
        var enforcer = new ServiceAgnosticCapabilityEnforcer(
            serviceInfo,
            new SonarrCapabilityEnforcer(Substitute.For<ISonarrCapabilityFetcher>()),
            new RadarrCapabilityEnforcer(Substitute.For<IRadarrCapabilityFetcher>())
        );
        return new InstanceSyncProcessor(
            log,
            NewConfig.Radarr(),
            publisher,
            planBuilder,
            pipelines,
            enforcer,
            strategies
        );
    }

    private sealed class RecordingInstancePublisher : IInstancePublisher
    {
        public List<SyncOutcome> Outcomes { get; } = [];

        public void Add(SyncOutcome outcome) => Outcomes.Add(outcome);

        public void AddError(string message) { }

        public void AddWarning(string message) { }

        public void AddDeprecation(string message) { }

        public IPipelinePublisher ForPipeline(PipelineType type) => IPipelinePublisher.Noop;
    }

    private sealed class RecordingPipelineExecutor : IPipelineExecutor
    {
        public bool Interrupted { get; private set; }

        public Task<PipelineResult> Execute(
            ISyncSettings settings,
            PipelinePlan plan,
            IInstancePublisher instancePublisher,
            string instanceName,
            CancellationToken ct
        ) => throw new InvalidOperationException("Pipeline execution was not expected");

        public void InterruptAll(IInstancePublisher instancePublisher)
        {
            Interrupted = true;
        }
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<LogEvent> Events { get; } = [];

        public void Write(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}
