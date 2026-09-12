using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using Recyclarr.Server.Features.Sync.CreateJob;
using Recyclarr.Server.Features.Sync.GetJob;
using Recyclarr.Server.Sync;
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

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
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
            ProblemDetails
        >(new CreateSyncJobRequest { Service = (SupportedServices)999, Instances = ["anything"] });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        problem.Should().NotBeNull();
        problem.Status.Should().Be(400);
        problem.Type.Should().NotBeNullOrEmpty();
        problem.Title.Should().NotBeNullOrEmpty();
        problem.Errors.Should().ContainSingle(e => e.Name == "service");
    }

    [Test]
    public async Task Invalid_server_configuration_returns_safe_internal_error()
    {
        var config = Paths.ConfigDirectory.File("broken.yml");
        Fs.AddFile(
            config,
            new MockFileData(
                """
                radarr:
                  broken:
                    base_url: http://secret-service:7878
                    api_key: secret-api-key
                    unknown_property: invalid
                """
            )
        );

        using var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest
            {
                Instances = ["unknown-instance"],
                Configs = [config.FullName],
            }
        );
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        body.Should().NotContain("secret-service").And.NotContain("secret-api-key");
        body.Should().NotContain(config.FullName).And.NotContain("unknown_property");
    }

    [Test]
    public async Task Mixed_instance_selection_is_rejected_without_exposing_configuration()
    {
        var config = Paths.ConfigDirectory.File("selection.yml");
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
        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest
            {
                Instances = ["available-instance", "unknown-instance"],
                Configs = [config.FullName],
            }
        );
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().NotContain("available-instance").And.NotContain("unknown-instance");
    }

    [Test]
    public async Task Missing_explicit_config_returns_safe_bad_request()
    {
        var missing = Paths.ConfigDirectory.File("private-missing.yml");
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest { Configs = [missing.FullName] }
        );
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().NotContain(missing.FullName);
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
    public async Task No_default_configuration_returns_internal_error()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest()
        );

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
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

    [TestCase(
        """
            radarr:
              invalid-instance:
                base_url: http://private-invalid:7878
                api_key: ""
            """,
        TestName = "Invalid instance returns safe internal error"
    )]
    [TestCase(
        """
            radarr:
              duplicate-instance:
                base_url: http://private-radarr:7878
                api_key: first-secret
            sonarr:
              duplicate-instance:
                base_url: http://private-sonarr:8989
                api_key: second-secret
            """,
        TestName = "Duplicate instance returns safe internal error"
    )]
    [TestCase(
        """
            radarr:
              split-one:
                base_url: http://private-shared:7878
                api_key: first-secret
              split-two:
                base_url: http://private-shared:7878
                api_key: second-secret
            """,
        TestName = "Split instance returns safe internal error"
    )]
    public async Task Invalid_configuration_variants_return_safe_internal_error(string yaml)
    {
        var config = Paths.ConfigDirectory.File("invalid-variant.yml");
        Fs.AddFile(config, new MockFileData(yaml));
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest { Configs = [config.FullName] }
        );
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        body.Should().NotContain("private-").And.NotContain("secret");
        Services.GetRequiredService<ISyncJobStore>().GetAll(null).Should().BeEmpty();
    }
}
