using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Recyclarr.Logging;

namespace Recyclarr.Http;

internal sealed class HttpLoggingHandler(ILogger log) : DelegatingHandler
{
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Logging must not throw; best-effort body formatting"
    )]
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        // non-null: HttpRequestMessage.RequestUri is always set by Refit/HttpClient
        var originalUri = request.RequestUri!;
        var sanitizedUrl = Sanitize.Url(originalUri);
        log.Debug("HTTP Request: {Method} {Url}", request.Method, sanitizedUrl);
        await LogBody(sanitizedUrl, "Request", request.Method, request.Content, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);

        log.Debug(
            "HTTP Response: {Status} {Method} {Url}",
            (int)response.StatusCode,
            request.Method,
            sanitizedUrl
        );
        await LogBody(
            sanitizedUrl,
            "Response",
            request.Method,
            response.Content,
            cancellationToken
        );

        WarnOnFailedRedirect(response, originalUri, sanitizedUrl);

        return response;
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Logging must not throw; best-effort body formatting"
    )]
    private async Task LogBody(
        Uri url,
        string direction,
        HttpMethod method,
        HttpContent? content,
        CancellationToken ct
    )
    {
        if (content is null)
        {
            return;
        }

        var body = await content.ReadAsStringAsync(ct);
        if (string.IsNullOrEmpty(body))
        {
            return;
        }

        try
        {
            body = JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(body));
        }
        catch (JsonException)
        {
            // Best-effort pretty-print; log raw body on failure
        }

        log.Verbose("HTTP {Direction} Body: {Method} {Url} {Body}", direction, method, url, body);
    }

    private void WarnOnFailedRedirect(
        HttpResponseMessage response,
        Uri originalUri,
        Uri sanitizedOriginalUri
    )
    {
        if (
            !response.IsSuccessStatusCode
            && response.RequestMessage?.RequestUri is { } finalUri
            && finalUri != originalUri
        )
        {
            log.Warning(
                "HTTP redirect was followed from {OriginalUrl} to {FinalUrl}; "
                    + "this may indicate a problem with your URL and/or reverse proxy",
                sanitizedOriginalUri,
                Sanitize.Url(finalUri)
            );
        }
    }
}
