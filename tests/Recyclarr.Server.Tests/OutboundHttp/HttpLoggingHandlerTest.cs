using System.Globalization;
using System.Net;
using Recyclarr.Server.OutboundHttp;
using Serilog.Events;

namespace Recyclarr.Server.Tests.OutboundHttp;

internal sealed class HttpLoggingHandlerTest
{
    [TestCase(
        "<h2>Bad Request - Invalid Hostname</h2>",
        "sonarr.example.com",
        TestName = "Host rejection names the hostname"
    )]
    [TestCase("""[{"errorMessage":"Must be unique"}]""", null, TestName = "Other 400 does not")]
    public async Task Host_rejection_warning(string body, string? expectedHost)
    {
        var log = new RecordingLogger();
        using var sut = new HttpLoggingHandler(log)
        {
            InnerHandler = new FixedResponseHandler(HttpStatusCode.BadRequest, body),
        };
        using var invoker = new HttpMessageInvoker(sut);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://sonarr.example.com:8989/api/v3/system/status"
        );

        using var _ = await invoker.SendAsync(request, CancellationToken.None);

        var warnings = log
            .Events.Where(x => x.Level == LogEventLevel.Warning)
            .Select(x => x.RenderMessage(CultureInfo.InvariantCulture));
        if (expectedHost is null)
        {
            warnings.Should().BeEmpty();
            return;
        }

        warnings
            .Should()
            .ContainSingle()
            .Which.Should()
            .Contain($"\"{expectedHost}\"")
            .And.Contain("Allowed Hosts");
    }

    private sealed class FixedResponseHandler(HttpStatusCode statusCode, string body)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new HttpResponseMessage(statusCode)
                {
                    RequestMessage = request,
                    Content = new StringContent(body),
                }
            );
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<LogEvent> Events { get; } = [];

        public void Write(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}
