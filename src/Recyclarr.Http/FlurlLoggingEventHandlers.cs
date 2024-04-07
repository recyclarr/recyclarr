using Flurl.Http;
using JetBrains.Annotations;
using Serilog;

namespace Recyclarr.Http;

[UsedImplicitly]
public class FlurlBeforeCallLogRedactor(ILogger log) : FlurlSpecificEventHandler
{
    public override FlurlEventType EventType => FlurlEventType.BeforeCall;

    public override void Handle(FlurlEventType eventType, FlurlCall call)
    {
        var url = FlurlLogging.SanitizeUrl(call.Request.Url.Clone());
        log.Debug("HTTP Request: {Method} {Url}", call.HttpRequestMessage.Method, url);
        FlurlLogging.LogBody(log, url, "Request", call.HttpRequestMessage.Method, call.RequestBody);
    }
}

[UsedImplicitly]
public class FlurlAfterCallLogRedactor(ILogger log) : FlurlSpecificEventHandler
{
    public override FlurlEventType EventType => FlurlEventType.AfterCall;

    public override async Task HandleAsync(FlurlEventType eventType, FlurlCall call)
    {
        var statusCode = call.Response?.StatusCode.ToString() ?? "(No response)";
        var url = FlurlLogging.SanitizeUrl(call.Request.Url.Clone());
        log.Debug("HTTP Response: {Status} {Method} {Url}", statusCode, call.HttpRequestMessage.Method, url);

        var content = call.Response?.ResponseMessage.Content;
        if (content is not null)
        {
            FlurlLogging.LogBody(log, url, "Response", call.HttpRequestMessage.Method,
                await content.ReadAsStringAsync());
        }
    }
}

[UsedImplicitly]
public class FlurlRedirectPreventer(ILogger log) : FlurlSpecificEventHandler
{
    public override FlurlEventType EventType => FlurlEventType.OnRedirect;

    public override void Handle(FlurlEventType eventType, FlurlCall call)
    {
        log.Warning("HTTP Redirect received; this indicates a problem with your URL and/or reverse proxy: {Url}",
            FlurlLogging.SanitizeUrl(call.Redirect.Url));

        // Must follow redirect because we want an exception to be thrown eventually. If it is set to false, HTTP
        // communication stops and existing methods will return nothing / null. This messes with Observable
        // pipelines (which normally either expect a response object or an exception)
        call.Redirect.Follow = true;
    }
}
