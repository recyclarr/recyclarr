using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Json;
using Autofac;
using Recyclarr.Config.Models;
using Recyclarr.Server.Features.Sync.CreateJob;
using Recyclarr.Server.Sync;
using Recyclarr.Server.TestLibrary;
using Recyclarr.Server.Tests.Reusable;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using ISyncApi = Recyclarr.Client.V1.ISyncApi;
using RestService = Refit.RestService;

namespace Recyclarr.Server.Tests.Http;

// A sync that runs past server.sync_timeout must fail and free the single worker, so the next
// queued job runs. The limit runs on the server's clock, which these tests advance.
internal sealed class SyncJobTimeoutHttpTest : ServerHttpFixture
{
    private readonly TimerObservingTimeProvider _time = new();
    private readonly HangingOrchestrator _orchestrator = new();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterInstance(_time).As<TimeProvider>();
        builder.RegisterInstance(_orchestrator).As<ISyncOrchestrator>();
    }

    private void AddConfig(int? syncTimeout = null)
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                """
                radarr:
                  hung:
                    base_url: http://hung:7878
                    api_key: asdf
                  next:
                    base_url: http://next:7878
                    api_key: asdf
                """
            )
        );
        if (syncTimeout is { } minutes)
        {
            Fs.AddFile(
                Paths.ConfigDirectory.File("settings.yml"),
                new MockFileData($"server:\n  sync_timeout: {minutes}\n")
            );
        }
    }

    [Test]
    public async Task Sync_exceeding_the_time_limit_fails_and_the_next_job_runs()
    {
        AddConfig();
        using var client = CreateClient();

        var hung = await CreateJob(client, "hung");
        await _orchestrator.Started("hung");
        var next = await CreateJob(client, "next");
        _time.Advance(TimeSpan.FromMinutes(5));
        await _orchestrator.Started("next");

        (await StatusOf(client, hung)).Should().Be(nameof(SyncJobStatus.Failed));
        (await StatusOf(client, next)).Should().NotBe(nameof(SyncJobStatus.Pending));
    }

    [Test]
    public async Task Raised_time_limit_lets_a_longer_sync_continue()
    {
        AddConfig(syncTimeout: 10);
        using var client = CreateClient();

        var hung = await CreateJob(client, "hung");
        await _orchestrator.Started("hung");
        _time.Advance(TimeSpan.FromMinutes(5));

        // Fake time fires due timers and cancels their tokens inside Advance, so a canceled run
        // would already show here.
        _orchestrator.HungRunCanceled.Should().BeFalse();
        (await StatusOf(client, hung)).Should().Be(nameof(SyncJobStatus.Running));
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

    private static async Task<string?> StatusOf(HttpClient client, Guid id) =>
        (await RestService.For<ISyncApi>(client).JobsGet(id)).Content?.Status;

    // The "hung" instance runs until canceled; every other instance finishes at once.
    private sealed class HangingOrchestrator : ISyncOrchestrator
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _started = [];
        private CancellationToken _hungToken;

        public bool HungRunCanceled => _hungToken.IsCancellationRequested;

        public Task Started(string instance) =>
            Signal(instance).Task.WaitAsync(TimeSpan.FromSeconds(10));

        public async Task<SyncRunResult> RunAsync(
            IReadOnlyList<IServiceConfiguration> configs,
            ISyncSettings settings,
            IInstanceSyncProgress progress,
            CancellationToken ct
        )
        {
            var instance = configs.Single().InstanceName;
            if (instance != "hung")
            {
                Signal(instance).TrySetResult();
                return new SyncRunResult([]);
            }

            _hungToken = ct;
            var canceled = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            await using var registration = ct.Register(() => canceled.TrySetCanceled(ct));
            Signal(instance).TrySetResult();
            await canceled.Task;
            return new SyncRunResult([]);
        }

        private TaskCompletionSource Signal(string instance) =>
            _started.GetOrAdd(
                instance,
                _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
            );
    }
}
