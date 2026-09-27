using System.Diagnostics;
using Serilog.Events;

namespace Recyclarr.Cli.Server;

/// <summary>
/// Logs the CLI's exchange with the server. The CLI writes no log files, so <c>--log</c> output is
/// the only local record of it. Request lines log at Debug and payloads at Verbose. Headers are
/// never logged because they will carry the server API key.
/// </summary>
internal sealed class HttpTrafficLogHandler(ILogger log) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var logBodies = log.IsEnabled(LogEventLevel.Verbose);
        if (logBodies && request.Content is not null)
        {
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            log.Verbose("HTTP request body: {Body:l}", body);
        }

        var timer = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);

        log.Debug(
            "HTTP {Method:l} {Uri} responded {StatusCode} in {ElapsedMs} ms",
            request.Method.Method,
            request.RequestUri,
            (int)response.StatusCode,
            timer.ElapsedMilliseconds
        );

        if (logBodies)
        {
            // Buffers the content, so the caller can still read it after this.
            await response.Content.LoadIntoBufferAsync(cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            log.Verbose("HTTP response body: {Body:l}", body);
        }

        return response;
    }
}
