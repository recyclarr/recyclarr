using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Json;
using Autofac;
using NSubstitute.ExceptionExtensions;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using Recyclarr.Server.Features.Instances.CustomFormats.List;
using Recyclarr.Server.TestLibrary;
using Refit;

namespace Recyclarr.Server.Tests.Http;

internal sealed class InstanceCustomFormatsHttpTest : ServerHttpFixture
{
    private readonly ICustomFormatService _service = Substitute.For<ICustomFormatService>();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterInstance(_service).As<ICustomFormatService>();
    }

    private HttpClient StartWithInstance()
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                """
                radarr:
                  movies:
                    base_url: http://localhost:7878
                    api_key: secret-api-key
                """
            )
        );
        return CreateClient();
    }

    [Test]
    public async Task List_returns_the_instance_custom_formats()
    {
        _service
            .GetCustomFormats(default)
            .ReturnsForAnyArgs([new CustomFormatResource { Id = 7, Name = "HDR" }]);
        using var client = StartWithInstance();

        var response = await client.GetFromJsonAsync<ListInstanceCustomFormatsResponse>(
            new Uri("/api/v1/instances/MOVIES/custom-formats", UriKind.Relative)
        );

        response!.Items.Should().Equal(new InstanceCustomFormatSummaryResponse(7, "HDR"));
    }

    [Test]
    public async Task Unknown_instance_returns_not_found()
    {
        using var client = StartWithInstance();

        var response = await client.GetAsync(
            new Uri("/api/v1/instances/unknown/custom-formats", UriKind.Relative)
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Delete_removes_the_custom_format()
    {
        using var client = StartWithInstance();

        var response = await client.DeleteAsync(
            new Uri("/api/v1/instances/movies/custom-formats/7", UriKind.Relative)
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _service.Received().DeleteCustomFormat(7, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Deleting_a_missing_custom_format_returns_not_found()
    {
        _service
            .DeleteCustomFormat(default, default)
            .ThrowsAsyncForAnyArgs(await CreateApiException(HttpStatusCode.NotFound));
        using var client = StartWithInstance();

        var response = await client.DeleteAsync(
            new Uri("/api/v1/instances/movies/custom-formats/7", UriKind.Relative)
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Service_failure_returns_bad_gateway_with_the_failure_category()
    {
        _service
            .GetCustomFormats(default)
            .ThrowsAsyncForAnyArgs(await CreateApiException(HttpStatusCode.Unauthorized));
        using var client = StartWithInstance();

        var response = await client.GetAsync(
            new Uri("/api/v1/instances/movies/custom-formats", UriKind.Relative)
        );
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        body.Should()
            .Contain("\"failure\":\"serviceUnauthenticated\"")
            .And.NotContain("secret-api-key")
            .And.NotContain("localhost:7878");
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "ApiException.Create takes ownership of request and response"
    )]
    private static Task<ApiException> CreateApiException(HttpStatusCode statusCode)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "http://localhost:7878/api/v3/customformat?apikey=secret-api-key"
        );
        var response = new HttpResponseMessage(statusCode);
        return ApiException.Create(request, HttpMethod.Get, response, new RefitSettings());
    }
}
