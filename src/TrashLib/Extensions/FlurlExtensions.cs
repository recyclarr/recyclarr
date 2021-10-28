using System;
using Flurl;
using Flurl.Http;
using Serilog;

namespace TrashLib.Extensions
{
    public static class FlurlExtensions
    {
        public static IFlurlRequest SanitizedLogging(this Uri url, ILogger log)
            => new FlurlRequest(url).SanitizedLogging(log);

        public static IFlurlRequest SanitizedLogging(this Url url, ILogger log)
            => new FlurlRequest(url).SanitizedLogging(log);

        public static IFlurlRequest SanitizedLogging(this string url, ILogger log)
            => new FlurlRequest(url).SanitizedLogging(log);

        public static IFlurlRequest SanitizedLogging(this IFlurlRequest request, ILogger log)
        {
            return request.ConfigureRequest(settings => FlurlLogging.SetupLogging(settings, log, SanitizeUrl));
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
}
