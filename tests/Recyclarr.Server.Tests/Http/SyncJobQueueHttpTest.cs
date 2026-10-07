using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Channels;
using Autofac;
using Recyclarr.Config.Models;
using Recyclarr.Server.Features.Sync.CreateJob;
using Recyclarr.Server.Sync;
using Recyclarr.Server.TestLibrary;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using ISyncApi = Recyclarr.Client.V1.ISyncApi;
using RestService = Refit.RestService;

namespace Recyclarr.Server.Tests.Http;

// Jobs created through the API run through the real TickerQ queue, one at a time.
internal sealed class SyncJobQueueHttpTest : ServerHttpFixture
{
    private readonly BlockingOrchestrator _orchestrator = new();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterInstance(_orchestrator).As<ISyncOrchestrator>();
    }

    [Test]
    public async Task Second_job_waits_until_the_first_finishes()
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                """
                radarr:
                  movies:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );
        using var client = CreateClient();
        var api = RestService.For<ISyncApi>(client);

        var first = await CreateJob(client);
        await _orchestrator.WaitForRuns(1);
        var second = await CreateJob(client);

        // The queue has had time to start the second job if it were going to.
        await Task.Delay(TimeSpan.FromSeconds(1));
        (await api.JobsGet(second)).Content?.Status.Should().Be(nameof(SyncJobStatus.Pending));

        _orchestrator.Release();
        await _orchestrator.WaitForRuns(2);
        _orchestrator.Release();

        (await WaitForTerminal(api, first)).Should().Be(nameof(SyncJobStatus.Succeeded));
        (await WaitForTerminal(api, second)).Should().Be(nameof(SyncJobStatus.Succeeded));
    }

    private static async Task<Guid> CreateJob(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest()
        );
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var created = await response.Content.ReadFromJsonAsync<CreateSyncJobResponse>();
        return created!.Id;
    }

    private static async Task<string?> WaitForTerminal(ISyncApi api, Guid id)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var job = await api.JobsGet(id, timeout.Token);
            if (job.StatusCode == HttpStatusCode.OK)
            {
                return job.Content?.Status;
            }

            await Task.Delay(50, timeout.Token);
        }
    }

    // Each run blocks until released, so a test controls when a job finishes.
    private sealed class BlockingOrchestrator : ISyncOrchestrator
    {
        private readonly Channel<bool> _release = Channel.CreateUnbounded<bool>();
        private readonly Channel<bool> _started = Channel.CreateUnbounded<bool>();
        private int _runs;

        public async Task<SyncRunResult> RunAsync(
            IReadOnlyList<IServiceConfiguration> configs,
            ISyncSettings settings,
            IInstanceSyncProgress progress,
            CancellationToken ct
        )
        {
            Interlocked.Increment(ref _runs);
            _started.Writer.TryWrite(true);
            await _release.Reader.ReadAsync(ct);
            return new SyncRunResult([]);
        }

        public void Release() => _release.Writer.TryWrite(true);

        public async Task WaitForRuns(int count)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (Volatile.Read(ref _runs) < count)
            {
                await _started.Reader.ReadAsync(timeout.Token);
            }
        }
    }
}
