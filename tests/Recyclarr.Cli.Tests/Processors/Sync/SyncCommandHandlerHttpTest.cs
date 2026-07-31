using System.Reactive.Linq;
using Autofac;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Client.V1;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.TestLibrary.Autofac;

namespace Recyclarr.Cli.Tests.Processors.Sync;

// Drives the real command handler against a real in-process server over HTTP. Everything the CLI
// observes here (job creation, polling, terminal status) travels the same wire it does in
// production; only the sync engine behind the server is stubbed.
internal sealed class SyncCommandHandlerHttpTest : CliServerHttpFixture
{
    private const string InstanceName = "real-instance";

    // Read lazily by the substitutes below, so each test decides how the sync "went" before it
    // triggers a job.
    private ExitStatus _exitStatus = ExitStatus.Succeeded;
    private IObservable<PipelineEvent> _pipelines = Observable.Never<PipelineEvent>();

    private sealed record Settings : ISyncSettings
    {
        public TrashGuide.SupportedServices? Service => null;
        public IReadOnlyCollection<string> Configs => [];
        public bool Preview => false;
        public IReadOnlyCollection<string>? Instances { get; init; }
    }

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterMockFor<ISyncOrchestrator>(m =>
            m.RunAsync(default!, default!, default)
                .ReturnsForAnyArgs(_ => Task.FromResult(_exitStatus))
        );

        builder.RegisterMockFor<ISyncRunScope>(m =>
        {
            m.Pipelines.Returns(_ => _pipelines);
            m.Diagnostics.Returns(Observable.Never<SyncDiagnosticEvent>());
        });
    }

    [Test]
    public async Task A_sync_that_succeeded_exits_successfully()
    {
        AddInstanceConfig(InstanceName);

        var result = await RunSync();

        result.Should().Be(ExitStatus.Succeeded);
        (await TerminalStatusOfTheOnlyJob()).Should().Be("Succeeded");
    }

    // A partial sync applied everything it could, which the CLI has always reported as success.
    [Test]
    public async Task A_sync_that_was_only_partial_still_exits_successfully()
    {
        AddInstanceConfig(InstanceName);
        _pipelines = EveryPipeline(PipelineProgressStatus.Partial);

        var result = await RunSync();

        result.Should().Be(ExitStatus.Succeeded);
        (await TerminalStatusOfTheOnlyJob()).Should().Be("Partial");
    }

    [Test]
    public async Task A_sync_that_failed_exits_with_failure()
    {
        AddInstanceConfig(InstanceName);
        _exitStatus = ExitStatus.Failed;

        var result = await RunSync();

        result.Should().Be(ExitStatus.Failed);
        (await TerminalStatusOfTheOnlyJob()).Should().Be("Failed");
    }

    // Naming an instance the config does not define leaves the server with nothing to sync, so it
    // refuses the request outright and never creates a job to poll.
    [Test]
    public async Task A_refused_request_fails_the_command_and_leaves_no_job_to_poll()
    {
        AddInstanceConfig(InstanceName);

        var result = await RunSync(new Settings { Instances = ["no-such-instance"] });

        result.Should().Be(ExitStatus.Failed);
        (await ListJobs()).Should().BeEmpty();
    }

    private async Task<ExitStatus> RunSync(Settings? settings = null)
    {
        return await ResolveCli<SyncCommandHandler>()
            .RunAsync(
                Api,
                settings ?? new Settings(),
                TestContext.CurrentContext.CancellationToken
            );
    }

    private async Task<string> TerminalStatusOfTheOnlyJob()
    {
        var jobs = await ListJobs();
        return jobs.Should().ContainSingle().Subject.Status;
    }

    private async Task<IReadOnlyList<SyncJobSummaryResponse>> ListJobs()
    {
        using var response = await Api.JobsGet(
            status: null,
            TestContext.CurrentContext.CancellationToken
        );

        response.IsSuccessful.Should().BeTrue();

        // non-null: a successful response always carries the job list
        return response.Content!.Jobs;
    }

    // An instance only reaches a terminal state once every pipeline has, so the intended status
    // has to be reported for all of them.
    private static IObservable<PipelineEvent> EveryPipeline(PipelineProgressStatus status)
    {
        return Enum.GetValues<PipelineType>()
            .Select(type => new PipelineEvent(InstanceName, type, status, Count: 0))
            .ToObservable();
    }
}
