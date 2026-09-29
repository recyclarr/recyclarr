using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Recyclarr.ErrorHandling;
using Refit;

namespace Recyclarr.Core.Tests.ErrorHandling;

[TestFixture]
internal sealed class HttpExceptionStrategyTest
{
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "ApiException.Create takes ownership of request and response"
    )]
    private static async Task<ApiException> CreateApiException(
        HttpStatusCode statusCode,
        string body,
        string? requestBody = null
    )
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            "http://localhost/api/v3/qualityprofile/7"
        );
        if (requestBody is not null)
        {
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        }

        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        var exception = await ApiException.Create(
            request,
            HttpMethod.Put,
            response,
            new RefitSettings()
        );
        exception.RequestContent = requestBody;
        return exception;
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

        var outcome = result.Should().BeOfType<HttpApiFailure>().Which;
        outcome.StatusCode.Should().Be(400);
        outcome
            .ResponseMessages.Should()
            .BeEquivalentTo([
                new HttpApiResponseMessage("Minimum Custom Format Score can never be satisfied"),
            ]);
    }

    [Test]
    public async Task Single_message_object_extracts_message()
    {
        const string body = """{"message":"Request body can't be empty"}""";

        var sut = new HttpExceptionStrategy();
        var result = await sut.HandleAsync(
            await CreateApiException(HttpStatusCode.BadRequest, body)
        );

        var outcome = result.Should().BeOfType<HttpApiFailure>().Which;
        outcome
            .ResponseMessages.Should()
            .BeEquivalentTo([new HttpApiResponseMessage("Request body can't be empty")]);
    }

    [Test]
    public async Task Empty_body_retains_no_response_messages()
    {
        var sut = new HttpExceptionStrategy();
        var result = await sut.HandleAsync(await CreateApiException(HttpStatusCode.BadRequest, ""));

        var outcome = result.Should().BeOfType<HttpApiFailure>().Which;
        outcome.ResponseMessages.Should().BeEmpty();
    }

    [Test]
    public async Task Connection_error_is_a_connection_failure()
    {
        var sut = new HttpExceptionStrategy();
        var result = await sut.HandleAsync(new HttpRequestException("Connection refused"));

        result.Should().BeOfType<HttpConnectionFailure>();
    }

    [Test]
    public async Task Unauthorized_retains_status_code()
    {
        var sut = new HttpExceptionStrategy();
        var result = await sut.HandleAsync(
            await CreateApiException(HttpStatusCode.Unauthorized, "")
        );

        var outcome = result.Should().BeOfType<HttpApiFailure>().Which;
        outcome.StatusCode.Should().Be(401);
    }

    [Test]
    public async Task Unauthorized_retains_response_context()
    {
        const string body = """{"Title":"Invalid request","Errors":{"apiKey":["Expired"]}}""";
        var sut = new HttpExceptionStrategy();

        var result = await sut.HandleAsync(
            await CreateApiException(HttpStatusCode.Unauthorized, body)
        );

        var outcome = result.Should().BeOfType<HttpApiFailure>().Which;
        HttpApiFailureMessage[] expected =
        [
            new HttpApiResponseMessage("Invalid request"),
            new HttpApiFieldError("apiKey", "Expired"),
        ];
        outcome.ResponseMessages.Should().BeEquivalentTo(expected);
    }

    [Test]
    public async Task Service_error_retains_title_and_field_errors()
    {
        const string body =
            """{"Title":"Validation failed","Errors":{"name":["Required"],"cutoff":["Invalid"]}}""";
        var sut = new HttpExceptionStrategy();

        var result = await sut.HandleAsync(
            await CreateApiException(HttpStatusCode.BadRequest, body)
        );

        var outcome = result.Should().BeOfType<HttpApiFailure>().Which;
        HttpApiFailureMessage[] expected =
        [
            new HttpApiResponseMessage("Validation failed"),
            new HttpApiFieldError("name", "Required"),
            new HttpApiFieldError("cutoff", "Invalid"),
        ];
        outcome.ResponseMessages.Should().BeEquivalentTo(expected);
    }

    [Test]
    public async Task Malformed_body_retains_status_fallback()
    {
        var sut = new HttpExceptionStrategy();

        var result = await sut.HandleAsync(
            await CreateApiException(HttpStatusCode.BadRequest, "not-json")
        );

        var outcome = result.Should().BeOfType<HttpApiFailure>().Which;
        outcome.ResponseMessages.Should().BeEmpty();
    }

    [Test]
    public async Task Request_content_is_retained()
    {
        var sut = new HttpExceptionStrategy();

        var result = await sut.HandleAsync(
            await CreateApiException(HttpStatusCode.BadRequest, "", "{\"name\":\"WEB\"}")
        );

        var outcome = result.Should().BeOfType<HttpApiFailure>().Which;
        outcome.HasRequestContent.Should().BeTrue();
        outcome.RequestBody.Should().Be("{\"name\":\"WEB\"}");
    }
}
