using System.Text.Json;
using Flurl;
using Flurl.Http.Configuration;

namespace Recyclarr.TrashLib.Http;

public static class FlurlLogging
{
    public static void SetupLogging(FlurlHttpSettings settings, ILogger log, Func<Url, Url>? urlInterceptor = null)
    {
        urlInterceptor ??= SanitizeUrl;

        settings.BeforeCall = call =>
        {
            var url = urlInterceptor(call.Request.Url.Clone());
            log.Debug("HTTP Request: {Method} {Url}", call.HttpRequestMessage.Method, url);
            LogBody(log, url, "Request", call.HttpRequestMessage.Method, call.RequestBody);
        };

        settings.AfterCallAsync = async call =>
        {
            var statusCode = call.Response?.StatusCode.ToString() ?? "(No response)";
            var url = urlInterceptor(call.Request.Url.Clone());
            log.Debug("HTTP Response: {Status} {Method} {Url}", statusCode, call.HttpRequestMessage.Method, url);

            var content = call.Response?.ResponseMessage.Content;
            if (content is not null)
            {
                LogBody(log, url, "Response", call.HttpRequestMessage.Method, await content.ReadAsStringAsync());
            }
        };

        settings.OnRedirect = call =>
        {
            log.Warning("HTTP Redirect received; this indicates a problem with your URL and/or reverse proxy: {Url}",
                urlInterceptor(call.Redirect.Url));

            // Must follow redirect because we want an exception to be thrown eventually. If it is set to false, HTTP
            // communication stops and existing methods will return nothing / null. This messes with Observable
            // pipelines (which normally either expect a response object or an exception)
            call.Redirect.Follow = true;
        };
    }

    private static void LogBody(ILogger log, Url url, string direction, HttpMethod method, string? body)
    {
        if (body is null)
        {
            return;
        }

        try
        {
            body = JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(body));
        }
        catch (JsonException)
        {
            // Ignore failures here because we'll log the body anyway.
        }

        log.Verbose("HTTP {Direction} Body: {Method} {Url} {Body}", direction, method, url, body);
    }

    public static Url SanitizeUrl(Url url)
    {
        // Replace hostname for user privacy
        url.Host = "REDACTED";
        return url;
    }
}
