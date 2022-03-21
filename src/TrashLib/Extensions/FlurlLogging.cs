using Flurl;
using Flurl.Http.Configuration;
using Serilog;

namespace TrashLib.Extensions;

public static class FlurlLogging
{
    public static void SetupLogging(FlurlHttpSettings settings, ILogger log, Func<Url, Url>? urlInterceptor = null)
    {
        urlInterceptor ??= url => url;

        settings.BeforeCall = call =>
        {
            var url = urlInterceptor(call.Request.Url.Clone());
            log.Debug("HTTP Request to {Url}", url);
        };

        settings.AfterCall = call =>
        {
            var statusCode = call.Response?.StatusCode.ToString() ?? "(No response)";
            var url = urlInterceptor(call.Request.Url.Clone());
            log.Debug("HTTP Response {Status} from {Url}", statusCode, url);
        };

        settings.OnRedirect = call =>
        {
            log.Warning("HTTP Redirect received; this indicates a problem with your URL and/or reverse proxy: {Url}",
                call.Redirect.Url);

            call.Redirect.Follow = false;
        };
    }
}
