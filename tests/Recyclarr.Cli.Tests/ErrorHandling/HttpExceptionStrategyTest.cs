using System.Net;
using System.Text;
using Flurl.Http;
using Recyclarr.Cli.ErrorHandling.Strategies;

namespace Recyclarr.Cli.Tests.ErrorHandling;

[TestFixture]
internal class HttpExceptionStrategyTest
{
    private static FlurlHttpException CreateFlurlException(HttpStatusCode statusCode, string body)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        var call = new FlurlCall
        {
            HttpRequestMessage = new HttpRequestMessage(
                HttpMethod.Put,
                "http://localhost/api/v3/qualityprofile/7"
            ),
            HttpResponseMessage = response,
            Request = new FlurlRequest(),
        };

        call.Response = new FlurlResponse(call);
        return new FlurlHttpException(call);
    }

    [Test]
    public async Task Validation_error_array_extracts_errorMessage()
    {
        const string body =
            """[{"propertyName":"","errorMessage":"Minimum Custom Format Score can never be satisfied","severity":"error"}]""";

        var sut = new HttpExceptionStrategy();
        var result = await sut.HandleAsync(CreateFlurlException(HttpStatusCode.BadRequest, body));

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
        var result = await sut.HandleAsync(CreateFlurlException(HttpStatusCode.BadRequest, body));

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo("HTTP 400: Request body can't be empty");
    }

    [Test]
    public async Task Empty_body_falls_back_to_status_text()
    {
        var sut = new HttpExceptionStrategy();
        var result = await sut.HandleAsync(CreateFlurlException(HttpStatusCode.BadRequest, ""));

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo("HTTP 400");
    }
}
