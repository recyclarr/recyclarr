using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using Recyclarr.Cli.ErrorHandling.Strategies;
using Refit;

namespace Recyclarr.Cli.Tests.ErrorHandling;

[TestFixture]
internal class HttpExceptionStrategyTest
{
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "ApiException.Create takes ownership of request and response"
    )]
    private static async Task<ApiException> CreateApiException(
        HttpStatusCode statusCode,
        string body
    )
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            "http://localhost/api/v3/qualityprofile/7"
        );

        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        return await ApiException.Create(request, HttpMethod.Put, response, new RefitSettings());
    }

    [Test]
    public async Task Validation_error_array_extracts_errorMessage()
    {
        const string body =
            """[{"propertyName":"","errorMessage":"Minimum Custom Format Score can never be satisfied","severity":"error"}]""";

        var sut = new HttpExceptionStrategy();
        var result = await sut.HandleAsync(
            await CreateApiException(HttpStatusCode.BadRequest, body)
        );

        result.Should().NotBeNull();
        result
            .Should()
            .BeEquivalentTo("HTTP 400: Minimum Custom Format Score can never be satisfied");
    }

    [Test]
    public async Task Single_message_object_extracts_message()
    {
        const string body = """{"message":"Request body can't be empty"}""";

        var sut = new HttpExceptionStrategy();
        var result = await sut.HandleAsync(
            await CreateApiException(HttpStatusCode.BadRequest, body)
        );

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo("HTTP 400: Request body can't be empty");
    }

    [Test]
    public async Task Empty_body_falls_back_to_status_text()
    {
        var sut = new HttpExceptionStrategy();
        var result = await sut.HandleAsync(await CreateApiException(HttpStatusCode.BadRequest, ""));

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo("HTTP 400");
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "ApiException.Create takes ownership of request and response"
    )]
    [Test]
    public async Task Unreadable_success_response_reports_the_read_failure()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"authentication":"oidc"}""", Encoding.UTF8),
        };
        var exception = await ApiException.Create(
            "An error occured deserializing the response.",
            new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/v3/system/status"),
            HttpMethod.Get,
            response,
            new RefitSettings(),
            new JsonException("The JSON value could not be converted. Path: $.authentication")
        );

        var sut = new HttpExceptionStrategy();
        var result = await sut.HandleAsync(exception);

        result
            .Should()
            .BeEquivalentTo(
                "HTTP 200: Could not read the response: "
                    + "The JSON value could not be converted. Path: $.authentication"
            );
    }

    [Test]
    public async Task Connection_error_returns_check_base_url()
    {
        var sut = new HttpExceptionStrategy();
        var result = await sut.HandleAsync(new HttpRequestException("Connection refused"));

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo("Connection failed - check your base_url");
    }

    [Test]
    public async Task Unauthorized_returns_check_api_key()
    {
        var sut = new HttpExceptionStrategy();
        var result = await sut.HandleAsync(
            await CreateApiException(HttpStatusCode.Unauthorized, "")
        );

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo("HTTP 401: Unauthorized - check your api_key");
    }
}
