using Autofac;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Client.V1;
using Recyclarr.Sync;
using Recyclarr.TestLibrary.Autofac;
using Refit;

namespace Recyclarr.Cli.Tests.Processors.Sync;

// The poller is what turns the server's 202/200 protocol into a stream of snapshots, so it is
// tested against the real job resource rather than a stand-in for it.
internal sealed class SyncJobPollerHttpTest : CliServerHttpFixture
{
    private const string InstanceName = "real-instance";

    // Holds the sync open so the job stays non-terminal for as long as the test needs it to.
    private readonly TaskCompletionSource<ExitStatus> _sync = new();

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterMockFor<ISyncOrchestrator>(m =>
            m.RunAsync(default!, default!, default).ReturnsForAnyArgs(_sync.Task)
        );
    }

    [Test]
    public async Task Snapshots_are_yielded_until_the_job_reports_a_terminal_status()
    {
        AddInstanceConfig(InstanceName);

        using var createResponse = await Api.JobsPost(
            new CreateSyncJobRequest { Instances = [InstanceName], Preview = false },
            Ct
        );

        createResponse.IsSuccessful.Should().BeTrue();

        // non-null: a 202 always carries the created job
        var jobId = createResponse.Content!.Id;

        var statuses = new List<string>();

        await foreach (var snapshot in SyncJobPoller.PollAsync(Api, jobId, Ct))
        {
            statuses.Add(snapshot.Status);

            // Released only once the poll has seen the job in flight, so there is guaranteed to be
            // at least one intermediate snapshot ahead of the terminal one.
            if (statuses.Count == 2)
            {
                _sync.SetResult(ExitStatus.Succeeded);
            }
        }

        statuses.Should().HaveCountGreaterThanOrEqualTo(3);
        statuses[^1].Should().Be("Succeeded");
        statuses[..^1].Should().AllSatisfy(s => s.Should().NotBe("Succeeded"));
    }

    [Test]
    public async Task An_error_response_stops_the_poll_and_surfaces_the_reason()
    {
        var act = async () =>
            await SyncJobPoller.PollAsync(Api, Guid.NewGuid(), Ct).ToListAsync(Ct);

        await act.Should().ThrowAsync<ApiException>();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // A test that failed before releasing the sync would otherwise leave the server's
            // background run hanging on this task forever.
            _sync.TrySetResult(ExitStatus.Succeeded);
        }

        base.Dispose(disposing);
    }
}
