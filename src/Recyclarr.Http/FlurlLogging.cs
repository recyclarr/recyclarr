using System.Text.Json;
using Flurl;
using Serilog;

namespace Recyclarr.Http;

public static class FlurlLogging
{
    public static void LogBody(ILogger log, Url url, string direction, HttpMethod method, string? body)
    {
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
