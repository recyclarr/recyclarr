using Autofac;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Client.V1;
using Recyclarr.Config.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TestLibrary.Autofac;
using SupportedServices = Recyclarr.TrashGuide.SupportedServices;

namespace Recyclarr.Cli.Tests.Processors.Sync;

// Drives the real command handler against a real in-process server over HTTP. Everything the CLI
// observes here (job creation, polling, terminal status) travels the same wire it does in
// production; only the sync engine behind the server is stubbed.
internal sealed class SyncCommandHandlerHttpTest : CliServerHttpFixture
{
    private const string InstanceName = "real-instance";

    private static readonly SyncInstanceResult Succeeded = new(
        InstanceName,
        SupportedServices.Radarr,
        []
    );

    private static readonly SyncInstanceResult Failed = new(
        "other-instance",
        SupportedServices.Radarr,
        [],
        failure: new ServiceUnavailableFailure()
    );

    // Read lazily by the substitute below, so each test decides how the sync "went" before it
    // triggers a job.
    private SyncRunResult _result = new([Succeeded]);

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterMockFor<ISyncOrchestrator>(m =>
            m.RunAsync(
                    Arg.Any<IReadOnlyList<IServiceConfiguration>>(),
                    Arg.Any<ISyncSettings>(),
                    Arg.Any<IInstanceSyncProgress>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(_ => Task.FromResult(_result))
        );
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
        _result = new SyncRunResult([Succeeded, Failed]);

        var result = await RunSync();

        result.Should().Be(ExitStatus.Succeeded);
        (await TerminalStatusOfTheOnlyJob()).Should().Be("Partial");
    }

    [Test]
    public async Task A_sync_that_failed_exits_with_failure()
    {
        AddInstanceConfig(InstanceName);
        _result = new SyncRunResult([Failed]);

        var result = await RunSync();

        result.Should().Be(ExitStatus.Failed);
        (await TerminalStatusOfTheOnlyJob()).Should().Be("Failed");
    }

    // The server refuses a request naming an unconfigured instance and never creates a job.
    [Test]
    public async Task A_refused_request_fails_the_command_and_leaves_no_job_to_poll()
    {
        AddInstanceConfig(InstanceName);

        var result = await RunSync(new CreateSyncJobRequest { Instances = ["no-such-instance"] });

        result.Should().Be(ExitStatus.Failed);
        (await ListJobs()).Should().BeEmpty();
    }

    private async Task<ExitStatus> RunSync(CreateSyncJobRequest? request = null)
    {
        return await ResolveCli<SyncCommandHandler>()
            .RunAsync(
                Api,
                request ?? new CreateSyncJobRequest(),
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
}
