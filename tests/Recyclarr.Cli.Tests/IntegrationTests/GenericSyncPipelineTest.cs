using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class GenericSyncPipelineTest : CliIntegrationFixture
{
    private PipelineProgressStatus? _lastStatus;
    private IPipelinePublisher _publisher = null!;

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);

        _publisher = Substitute.For<IPipelinePublisher>();
        _publisher
            .When(x => x.SetStatus(Arg.Any<PipelineProgressStatus>(), Arg.Any<int?>()))
            .Do(ci => _lastStatus = ci.Arg<PipelineProgressStatus>());
    }

    private GenericSyncPipeline<TestPipelineContext> CreatePipeline(params TestPhase[] phases)
    {
        var orderedPhases = phases.ToList().OrderBy(p => p.Order);
        return new GenericSyncPipeline<TestPipelineContext>(
            Resolve<ILogger>(),
            orderedPhases,
            Substitute.For<IServiceConfiguration>()
        );
    }

    private GenericSyncPipeline<SkippedTestPipelineContext> CreateSkippedPipeline(
        params SkippedTestPhase[] phases
    )
    {
        var orderedPhases = phases.ToList().OrderBy(p => p.Order);
        return new GenericSyncPipeline<SkippedTestPipelineContext>(
            Resolve<ILogger>(),
            orderedPhases,
            Substitute.For<IServiceConfiguration>()
        );
    }

    [Test]
    public async Task Phases_execute_in_order_until_completion()
    {
        var executionOrder = new List<int>();
        var phase1 = new TestPhase(1, () => executionOrder.Add(1));
        var phase2 = new TestPhase(2, () => executionOrder.Add(2));
        var phase3 = new TestPhase(3, () => executionOrder.Add(3));

        var sut = CreatePipeline(phase1, phase2, phase3);
        var settings = Substitute.For<ISyncSettings>();

        var result = await sut.Execute(
            settings,
            new TestPlan(),
            _publisher,
            CancellationToken.None
        );

        result.Should().Be(PipelineResult.Completed);
        executionOrder.Should().BeEquivalentTo([1, 2, 3], opts => opts.WithStrictOrdering());
    }

    [Test]
    public async Task Phase_returning_Terminate_stops_execution()
    {
        var executionOrder = new List<int>();
        var phase1 = new TestPhase(1, () => executionOrder.Add(1));
        var phase2 = new TestPhase(2, () => executionOrder.Add(2), PipelineFlow.Terminate);
        var phase3 = new TestPhase(3, () => executionOrder.Add(3));

        var sut = CreatePipeline(phase1, phase2, phase3);
        var settings = Substitute.For<ISyncSettings>();

        var result = await sut.Execute(
            settings,
            new TestPlan(),
            _publisher,
            CancellationToken.None
        );

        result.Should().Be(PipelineResult.Completed);
        executionOrder.Should().BeEquivalentTo([1, 2], opts => opts.WithStrictOrdering());
    }

    [Test]
    public async Task PipelineInterruptException_returns_Failed_with_status()
    {
        var phase = new TestPhase(1, () => throw new PipelineInterruptException());

        var sut = CreatePipeline(phase);
        var settings = Substitute.For<ISyncSettings>();

        var result = await sut.Execute(
            settings,
            new TestPlan(),
            _publisher,
            CancellationToken.None
        );

        result.Should().Be(PipelineResult.Failed);
        _lastStatus.Should().Be(PipelineProgressStatus.Failed);
    }

    [Test]
    public async Task Unexpected_exception_sets_Failed_status_and_rethrows()
    {
        var phase = new TestPhase(1, () => throw new InvalidOperationException("test error"));

        var sut = CreatePipeline(phase);
        var settings = Substitute.For<ISyncSettings>();

        var act = () => sut.Execute(settings, new TestPlan(), _publisher, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("test error");
        _lastStatus.Should().Be(PipelineProgressStatus.Failed);
    }

    [Test]
    public async Task ShouldSkip_true_returns_Completed_with_Skipped_status()
    {
        var phaseExecuted = false;
        var phase = new SkippedTestPhase(1, () => phaseExecuted = true);

        var sut = CreateSkippedPipeline(phase);
        var settings = Substitute.For<ISyncSettings>();

        var result = await sut.Execute(
            settings,
            new TestPlan(),
            _publisher,
            CancellationToken.None
        );

        result.Should().Be(PipelineResult.Completed);
        phaseExecuted.Should().BeFalse();
        _lastStatus.Should().Be(PipelineProgressStatus.Skipped);
    }
}

internal sealed class TestPipelineContext : PipelineContext, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.CustomFormat;
    public static IReadOnlyList<PipelineType> Dependencies => [];
    public override string PipelineDescription => "Test Pipeline";
}

internal sealed class SkippedTestPipelineContext : PipelineContext, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.CustomFormat;
    public static IReadOnlyList<PipelineType> Dependencies => [];
    public override string PipelineDescription => "Skipped Test Pipeline";
    public override bool ShouldSkip => true;
}

internal sealed class TestPhase(
    int order,
    Action? onExecute = null,
    PipelineFlow flow = PipelineFlow.Continue
) : IPipelinePhase<TestPipelineContext>
{
    public int Order => order;

    public Task<PipelineFlow> Execute(TestPipelineContext context, CancellationToken ct)
    {
        onExecute?.Invoke();
        return Task.FromResult(flow);
    }
}

internal sealed class SkippedTestPhase(
    int order,
    Action? onExecute = null,
    PipelineFlow flow = PipelineFlow.Continue
) : IPipelinePhase<SkippedTestPipelineContext>
{
    public int Order => order;

    public Task<PipelineFlow> Execute(SkippedTestPipelineContext context, CancellationToken ct)
    {
        onExecute?.Invoke();
        return Task.FromResult(flow);
    }
}
