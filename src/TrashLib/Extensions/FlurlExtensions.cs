using System.Text.RegularExpressions;
using Flurl;
using Flurl.Http;
using Serilog;

namespace TrashLib.Extensions;

public static class FlurlExtensions
{
    public static IFlurlRequest SanitizedLogging(this Url url, ILogger log)
        => new FlurlRequest(url).SanitizedLogging(log);

    public static IFlurlRequest SanitizedLogging(this IFlurlRequest request, ILogger log)
    {
        return request.ConfigureRequest(settings => FlurlLogging.SetupLogging(settings, log, SanitizeUrl));
    }

    public static string SanitizedExceptionMessage(this FlurlHttpException exception)
    {
        const string expression =
            @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}(\:[0-9]+)?\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";

        return Regex.Replace(exception.ToString(), expression, match => SanitizeUrl(match.Value).ToString());
    }

    private static Url SanitizeUrl(Url url)
    {
        // Replace hostname and API key for user privacy
        url.Host = "hostname";
        if (url.QueryParams.Contains("apikey"))
        {
            url.QueryParams.AddOrReplace("apikey", "SNIP");
        }

        return url;
    }
}
