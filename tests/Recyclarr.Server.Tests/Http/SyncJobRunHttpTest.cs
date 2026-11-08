using System.Diagnostics;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Autofac;
using Recyclarr.Server.Features.Sync.CreateJob;
using Recyclarr.Server.Sync;
using Recyclarr.Server.TestLibrary;
using Recyclarr.Server.Tests.Reusable;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;
using GeneratedProgressStatus = Recyclarr.Client.V1.InstanceProgressStatusResponse;
using GetSyncJobResponse = Recyclarr.Client.V1.GetSyncJobResponse;
using ISyncApi = Recyclarr.Client.V1.ISyncApi;
using RestService = Refit.RestService;
using SyncJobResultsResponse = Recyclarr.Client.V1.SyncJobResultsResponse;

namespace Recyclarr.Server.Tests.Http;

// How a queued job's run ends, observed through the API: jobs are created over HTTP and run by the
// real queue, with only the sync engine scripted per test. Each test runs one job; a second job
// (the "sentinel" instance) starting proves the first has finished.
internal sealed class SyncJobRunHttpTest : ServerHttpFixture
{
    private readonly ScriptedOrchestrator _orchestrator = new();
    private int _throwOnRelease;

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder
            .Register(_ => _orchestrator)
            .As<ISyncOrchestrator>()
            .InstancePerMatchingLifetimeScope("run")
            .OnRelease(_ =>
            {
                if (Interlocked.Exchange(ref _throwOnRelease, 0) == 1)
                {
                    throw new InvalidOperationException("cleanup secret");
                }
            });
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
                  sentinel:
                    base_url: http://sentinel:7878
                    api_key: asdf
                """
            )
        );
    }

    [Test]
    public async Task Canceled_run_keeps_finished_instances_and_ends_partial()
    {
        var firstResult = new SyncInstanceResult("first", SupportedServices.Radarr, []);
        _orchestrator.Script = progress =>
        {
            progress.InstanceStarted("first");
            progress.InstanceCompleted(firstResult);
            progress.InstanceStarted("second");
            throw new OperationCanceledException();
        };
        using var client = CreateClient();

        var id = await CreateJob(client, "first", "second");
        var job = await Finished(client, id);

        job.Status.Should().Be(nameof(SyncJobStatus.Partial));
        job.Progress.Select(x => x.Status)
            .Should()
            .Equal(GeneratedProgressStatus.Succeeded, GeneratedProgressStatus.Interrupted);
        (await Results(client, id)).Fault?.Reference.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Faulted_run_fails_with_a_fault_reference_and_an_error_span()
    {
        using var spans = new RecordedSpans();
        _orchestrator.Script = _ => throw new InvalidOperationException("engine fault");
        using var client = CreateClient();

        var id = await CreateJob(client, "first");
        var job = await Finished(client, id);

        job.Status.Should().Be(nameof(SyncJobStatus.Failed));
        job.Progress.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(GeneratedProgressStatus.NotRun);
        (await Results(client, id)).Fault?.Reference.Should().NotBeNullOrWhiteSpace();
        spans.ForJob(id).Status.Should().Be(ActivityStatusCode.Error);
    }

    [Test]
    public async Task Cleanup_failure_after_the_run_keeps_its_result()
    {
        _throwOnRelease = 1;
        using var client = CreateClient();

        var id = await CreateJob(client, "first");
        var job = await Finished(client, id);

        job.Status.Should().Be(nameof(SyncJobStatus.Succeeded));
        (await Results(client, id)).Fault.Should().BeNull();
    }

    // The request returns long before its job runs, so the job gets its own trace and links back to
    // the request. Nothing from the request (such as its span) may be ambient while the job runs.
    [Test]
    public async Task Manual_job_runs_in_its_own_trace_linked_to_its_request()
    {
        using var spans = new RecordedSpans();
        var ambient = new StrongBox<Activity?>();
        _orchestrator.Script = _ =>
        {
            ambient.Value = Activity.Current;
            return new SyncRunResult([]);
        };
        var requestTrace = ActivityTraceId.CreateRandom();
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add(
            "traceparent",
            $"00-{requestTrace}-{ActivitySpanId.CreateRandom()}-01"
        );

        var id = await CreateJob(client, "first");
        await Finished(client, id);

        var span = spans.ForJob(id);
        span.Kind.Should().Be(ActivityKind.Consumer);
        span.ParentSpanId.Should().Be(default(ActivitySpanId));
        span.TraceId.Should().NotBe(requestTrace);
        span.Links.Should().ContainSingle().Which.Context.TraceId.Should().Be(requestTrace);
        span.GetTagItem("recyclarr.sync.job.trigger").Should().Be("manual");
        ambient.Value?.TraceId.Should().Be(span.TraceId);
    }

    private static async Task<Guid> CreateJob(HttpClient client, params string[] instances)
    {
        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest { Instances = instances }
        );
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var created = await response.Content.ReadFromJsonAsync<CreateSyncJobResponse>();
        return created!.Id;
    }

    private async Task<GetSyncJobResponse> Finished(HttpClient client, Guid id)
    {
        await CreateJob(client, "sentinel");
        await _orchestrator.Started(2);
        return (await RestService.For<ISyncApi>(client).JobsGet(id)).Content
            ?? throw new InvalidOperationException("Missing job");
    }

    private static async Task<SyncJobResultsResponse> Results(HttpClient client, Guid id) =>
        (await RestService.For<ISyncApi>(client).Results(id)).Content
        ?? throw new InvalidOperationException("Missing results");
}
