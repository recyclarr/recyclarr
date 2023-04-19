using Flurl;
using Flurl.Http.Configuration;
using Newtonsoft.Json;

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
            LogBody(log, url, call.RequestBody);
        };

        settings.AfterCallAsync = async call =>
        {
            var statusCode = call.Response?.StatusCode.ToString() ?? "(No response)";
            var url = urlInterceptor(call.Request.Url.Clone());
            log.Debug("HTTP Response: {Status} {Method} {Url}", statusCode, call.HttpRequestMessage.Method, url);

            var content = call.Response?.ResponseMessage.Content;
            if (content is not null)
            {
                LogBody(log, url, await content.ReadAsStringAsync());
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

    private static void LogBody(ILogger log, Url url, string? body)
    {
        if (body is null)
        {
            return;
        }

        body = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(body));
        log.Verbose("HTTP Body: {Url} {Body}", url, body);
    }

    public static Url SanitizeUrl(Url url)
    {
        // Replace hostname for user privacy
        url.Host = "REDACTED";
        return url;
    }
}
