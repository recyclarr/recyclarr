using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using Recyclarr.Server.Features.Sync.CreateJob;
using Recyclarr.Server.Sync;
using Recyclarr.Server.TestLibrary;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;
using CreateJobEndpoint = Recyclarr.Server.Features.Sync.CreateJob.Endpoint;
using GeneratedProgressStatus = Recyclarr.Client.V1.InstanceProgressStatusResponse;
using ISyncApi = Recyclarr.Client.V1.ISyncApi;
using RestService = Refit.RestService;

namespace Recyclarr.Server.Tests.Http;

// Exercises sync jobs through the real ASP.NET Core pipeline (routing, api/v version prefix,
// FastEndpoints middleware, Problem Details, wire JSON) rather than calling HandleAsync()
// directly.
internal sealed class SyncJobsHttpTest : ServerHttpFixture
{
    [Test]
    public async Task Created_job_is_retrievable_from_its_location_header()
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                """
                radarr:
                  real-instance:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );

        using var client = CreateClient();

        var (createResponse, created) = await client.POSTAsync<
            CreateJobEndpoint,
            CreateSyncJobRequest,
            CreateSyncJobResponse
        >(new CreateSyncJobRequest { Instances = ["real-instance"] });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var location = createResponse.Headers.Location;
        location.Should().NotBeNull();
        location.OriginalString.Should().Be($"/api/v1/sync/jobs/{created.Id}");

        var getResponse = await RestService.For<ISyncApi>(client).JobsGet(created.Id);
        var job = getResponse.Content;

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        job.Should().NotBeNull();
        job.Id.Should().Be(created.Id);
        job.Instances.Should().Equal("real-instance");
        job.Preview.Should().BeFalse();
    }

    [Test]
    public async Task Generated_client_reads_cumulative_instance_only_progress()
    {
        var store = Services.GetRequiredService<ISyncJobStore>();
        var job = store.Create(
            new ServerSyncSettings(null, [], Preview: false),
            ["completed", "running", "pending", "partial", "failed"]
        );
        var completed = new SyncInstanceResult("completed", SupportedServices.Radarr, []);
        var partial = new SyncInstanceResult(
            "partial",
            SupportedServices.Radarr,
            [new TestPipelineResult(SyncResultStatus.Succeeded)],
            fault: new SyncFault("partial-fault")
        );
        var failed = new SyncInstanceResult(
            "failed",
            SupportedServices.Radarr,
            [],
            fault: new SyncFault("failed-fault")
        );
        store.Update(
            job.Id,
            current =>
            {
                current.Status = SyncJobStatus.Running;
                current.Progress = current.Progress.Start("completed").Complete(completed);
                current.Progress = current.Progress.Start("running");
                current.Progress = current.Progress.Start("partial").Complete(partial);
                current.Progress = current.Progress.Start("failed").Complete(failed);
            }
        );

        using var client = CreateClient();
        var response = await RestService.For<ISyncApi>(client).JobsGet(job.Id.Value);

        response.Content.Should().NotBeNull();
        response
            .Content.Progress.Select(instance => (instance.Name, instance.Status))
            .Should()
            .Equal(
                ("completed", GeneratedProgressStatus.Succeeded),
                ("running", GeneratedProgressStatus.Running),
                ("pending", GeneratedProgressStatus.Pending),
                ("partial", GeneratedProgressStatus.Partial),
                ("failed", GeneratedProgressStatus.Failed)
            );

        var raw = await client.GetStringAsync(
            new Uri($"/api/v1/sync/jobs/{job.Id.Value}", UriKind.Relative)
        );
        using var document = JsonDocument.Parse(raw);
        document
            .RootElement.GetProperty("progress")[0]
            .EnumerateObject()
            .Select(property => property.Name)
            .Should()
            .Equal("name", "status");
    }

    [Test]
    public async Task Slow_poll_observes_completed_and_stopped_instance_states()
    {
        var store = Services.GetRequiredService<ISyncJobStore>();
        var job = store.Create(
            new ServerSyncSettings(null, [], Preview: false),
            ["completed", "interrupted", "not-run"]
        );
        var completed = new SyncInstanceResult("completed", SupportedServices.Sonarr, []);
        store.Update(
            job.Id,
            current =>
            {
                current.Progress = current.Progress.Start("completed").Complete(completed);
                current.Progress = current.Progress.Start("interrupted").Stop();
                current.Result = new SyncRunResult([completed], new SyncFault("run-fault"));
                current.Status = current.Result.Status.ToJobStatus();
            }
        );

        using var client = CreateClient();
        var response = await RestService.For<ISyncApi>(client).JobsGet(job.Id.Value);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response
            .Content.Progress.Select(instance => instance.Status)
            .Should()
            .Equal(
                GeneratedProgressStatus.Succeeded,
                GeneratedProgressStatus.Interrupted,
                GeneratedProgressStatus.NotRun
            );
    }

    [Test]
    public async Task Unknown_job_id_yields_problem_details()
    {
        using var client = CreateClient();

        var response = await client.GetAsync(
            new Uri($"/api/v1/sync/jobs/{Guid.NewGuid()}", UriKind.Relative)
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    // Factory.Create bypasses the global error response builder, so only the real pipeline can
    // verify that validation failures fit CreateSyncJobProblemDetails.
    [Test]
    public async Task Validation_failure_fits_the_declared_problem_details_schema()
    {
        using var client = CreateClient();

        var (response, problem) = await client.POSTAsync<
            CreateJobEndpoint,
            CreateSyncJobRequest,
            CreateSyncJobProblemDetails
        >(new CreateSyncJobRequest { Service = (SupportedServices)999, Instances = ["anything"] });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        problem.Should().NotBeNull();
        problem.Status.Should().Be(400);
        problem.Title.Should().NotBeNullOrEmpty();
        problem.Errors.Should().ContainKey("service");
        problem.UnknownInstances.Should().BeNull();
    }

    [Test]
    public async Task Unknown_instances_are_rejected_with_unknown_and_available_names()
    {
        var config = Paths.YamlConfigDirectory.File("selection.yml");
        Fs.AddFile(
            config,
            new MockFileData(
                """
                radarr:
                  available-instance:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );

        using var client = CreateClient();
        var (response, problem) = await client.POSTAsync<
            CreateJobEndpoint,
            CreateSyncJobRequest,
            CreateSyncJobProblemDetails
        >(new CreateSyncJobRequest { Instances = ["available-instance", "unknown-instance"] });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        problem.UnknownInstances.Should().Equal("unknown-instance");
        problem.AvailableInstances.Should().Equal("available-instance");
        Services.GetRequiredService<ISyncJobStore>().GetAll(null).Should().BeEmpty();
    }

    [Test]
    public async Task Configuration_edited_after_startup_is_not_used()
    {
        var config = Paths.ConfigDirectory.File("recyclarr.yml");
        Fs.AddFile(
            config,
            new MockFileData(
                """
                radarr:
                  startup-instance:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );
        using var client = CreateClient();
        Fs.AddFile(
            config,
            new MockFileData(
                """
                radarr:
                  edited-instance:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );

        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest { Instances = ["edited-instance"] }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Empty_service_selection_returns_bad_request()
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                """
                radarr:
                  radarr-only:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest { Service = SupportedServices.Sonarr }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Server_without_instances_rejects_sync_with_conflict()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest()
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Test]
    public async Task Deprecation_warning_does_not_reject_job()
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                """
                radarr:
                  deprecated-setting:
                    base_url: http://localhost:7878
                    api_key: asdf
                    replace_existing_custom_formats: true
                """
            )
        );
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest()
        );

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    private sealed record TestPipelineResult : PipelineResult
    {
        public TestPipelineResult(SyncResultStatus status)
            : base(status) { }

        internal override PipelineResult WithStatus(
            SyncResultStatus status,
            Recyclarr.Sync.PipelineType? blockedBy = null
        ) => new TestPipelineResult(status);
    }
}
