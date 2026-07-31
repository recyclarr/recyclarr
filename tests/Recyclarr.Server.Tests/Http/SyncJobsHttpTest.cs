using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Json;
using FastEndpoints;
using Recyclarr.Server.Features.Sync.CreateJob;
using Recyclarr.Server.Features.Sync.GetJob;
using Recyclarr.Server.TestLibrary;
using Recyclarr.TrashGuide;
using CreateJobEndpoint = Recyclarr.Server.Features.Sync.CreateJob.Endpoint;

namespace Recyclarr.Server.Tests.Http;

// Exercises sync jobs through the real ASP.NET Core pipeline (routing, api/v version prefix,
// FastEndpoints middleware, Problem Details, wire JSON) rather than calling HandleAsync()
// directly. The endpoint-level tests under Features/ cover handler logic; these cover the
// adapter that only exists at the HTTP boundary.
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

        var getResponse = await client.GetAsync(location);
        var job = await getResponse.Content.ReadFromJsonAsync<GetSyncJobResponse>();

        getResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
        job.Should().NotBeNull();
        job.Id.Should().Be(created.Id);
        job.Instances.Should().Equal("real-instance");
        job.Preview.Should().BeFalse();
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

    // The gap that motivated the harness: Factory.Create emits FastEndpoints' default error shape
    // instead of the Problem Details shape UseProblemDetails() produces, so an endpoint-level test
    // cannot verify that validation failures fit CreateSyncJobProblemDetails.
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
        problem.Type.Should().NotBeNullOrEmpty();
        problem.Title.Should().NotBeNullOrEmpty();
        problem.Errors.Should().ContainSingle(e => e.Name == "service");
    }
}
