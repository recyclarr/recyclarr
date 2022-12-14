using Flurl;
using Flurl.Http.Configuration;
using Newtonsoft.Json;
using Serilog;

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
            LogBody(log, call.RequestBody);
        };

        settings.AfterCallAsync = async call =>
        {
            var statusCode = call.Response?.StatusCode.ToString() ?? "(No response)";
            var url = urlInterceptor(call.Request.Url.Clone());
            log.Debug("HTTP Response: {Status} {Method} {Url}", statusCode, call.HttpRequestMessage.Method, url);

            var content = call.Response?.ResponseMessage.Content;
            if (content is not null)
            {
                LogBody(log, await content.ReadAsStringAsync());
            }
        };

        settings.OnRedirect = call =>
        {
            log.Warning("HTTP Redirect received; this indicates a problem with your URL and/or reverse proxy: {Url}",
                urlInterceptor(call.Redirect.Url));

            call.Redirect.Follow = false;
        };
    }

    private static void LogBody(ILogger log, string? body)
    {
        if (body is null)
        {
            return;
        }

        body = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(body));
        log.Debug("HTTP Body: {Body}", body);
    }

    public static Url SanitizeUrl(Url url)
    {
        // Replace hostname and API key for user privacy
        url.Host = "REDACTED";
        if (url.QueryParams.Contains("apikey"))
        {
            url.QueryParams.AddOrReplace("apikey", "REDACTED");
        }

        return url;
    }
}
