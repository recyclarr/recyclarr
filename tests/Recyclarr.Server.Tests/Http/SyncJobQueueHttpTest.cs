using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Json;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Recyclarr.Config.Models;
using Recyclarr.Server.Features.Sync.CreateJob;
using Recyclarr.Server.Sync;
using Recyclarr.Server.TestLibrary;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using ISyncApi = Recyclarr.Client.V1.ISyncApi;
using RestService = Refit.RestService;

namespace Recyclarr.Server.Tests.Http;

// Exercises the job queue through the API: jobs created over HTTP run through the real Quartz
// scheduler, with only the sync engine stubbed.
internal sealed class SyncJobQueueHttpTest : ServerHttpFixture
{
    private readonly BlockingOrchestrator _orchestrator = new();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterInstance(_orchestrator).As<ISyncOrchestrator>();
    }

    [SetUp]
    public void SetUp()
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                """
                radarr:
                  first:
                    base_url: http://first:7878
                    api_key: asdf
                  second:
                    base_url: http://second:7878
                    api_key: asdf
                  third:
                    base_url: http://third:7878
                    api_key: asdf
                """
            )
        );
    }

    // The overlap check catches a parallel queue only when a later job starts before the release;
    // without a real-time wait it cannot prove that no overlap could ever happen.
    [Test]
    public async Task Jobs_run_one_at_a_time_in_creation_order()
    {
        using var client = CreateClient();

        await CreateJob(client, "first");
        await _orchestrator.Started("first");
        await CreateJob(client, "second");
        await CreateJob(client, "third");
        _orchestrator.ReleaseAll();
        await _orchestrator.Started("third");

        _orchestrator.RunOrder.Should().Equal("first", "second", "third");
        _orchestrator.MaxConcurrentRuns.Should().Be(1);
    }

    [Test]
    public async Task Job_that_cannot_be_queued_fails()
    {
        using var client = CreateClient();
        await Services.GetRequiredService<IScheduler>().Shutdown();

        var id = await CreateJob(client, "first");

        var job = (await RestService.For<ISyncApi>(client).JobsGet(id)).Content;
        job?.Status.Should().Be(nameof(SyncJobStatus.Failed));
    }

    private static async Task<Guid> CreateJob(HttpClient client, string instance)
    {
        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest { Instances = [instance] }
        );
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var created = await response.Content.ReadFromJsonAsync<CreateSyncJobResponse>();
        return created!.Id;
    }

    // Holds every run until released, recording which instance each run synced and how many runs
    // overlapped.
    private sealed class BlockingOrchestrator : ISyncOrchestrator
    {
        private readonly TaskCompletionSource _release = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _started = [];
        private readonly ConcurrentQueue<string> _runOrder = [];
        private int _running;
        private int _maxRunning;

        public IReadOnlyCollection<string> RunOrder => _runOrder;
        public int MaxConcurrentRuns => _maxRunning;

        public Task Started(string instance) =>
            Signal(instance).Task.WaitAsync(TimeSpan.FromSeconds(10));

        public void ReleaseAll() => _release.TrySetResult();

        public async Task<SyncRunResult> RunAsync(
            IReadOnlyList<IServiceConfiguration> configs,
            ISyncSettings settings,
            IInstanceSyncProgress progress,
            CancellationToken ct
        )
        {
            var running = Interlocked.Increment(ref _running);
            InterlockedMax(ref _maxRunning, running);
            var instance = configs.Single().InstanceName;
            _runOrder.Enqueue(instance);
            Signal(instance).TrySetResult();
            try
            {
                await _release.Task.WaitAsync(ct);
                return new SyncRunResult([]);
            }
            finally
            {
                Interlocked.Decrement(ref _running);
            }
        }

        private TaskCompletionSource Signal(string instance) =>
            _started.GetOrAdd(
                instance,
                _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
            );

        private static void InterlockedMax(ref int target, int value)
        {
            var current = Volatile.Read(ref target);
            while (value > current)
            {
                var previous = Interlocked.CompareExchange(ref target, value, current);
                if (previous == current)
                {
                    return;
                }

                current = previous;
            }
        }
    }
}
