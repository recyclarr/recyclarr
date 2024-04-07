using System.Text.RegularExpressions;
using Flurl.Http;

namespace Recyclarr.Http;

public static partial class FlurlExtensions
{
    public static string SanitizedExceptionMessage(this FlurlHttpException exception)
    {
        // Replace full URLs
        var result = UrlRegex().Replace(exception.Message, Sanitize);

        // There are sometimes parenthetical parts of the message that contain the host but are not
        // detected as true URLs. Just strip those out completely.
        return HostRegex().Replace(result, "");
    }

    private static string Sanitize(Match match)
    {
        return FlurlLogging.SanitizeUrl(match.Value).ToString() ?? match.Value;
    }

    [GeneratedRegex(@"\([-a-zA-Z0-9@:%._+~#=]{1,256}(?::[0-9]+)?\)")]
    private static partial Regex HostRegex();

    [GeneratedRegex(@"https?://(www\.)?[-a-zA-Z0-9@:%._+~#=]{1,256}(:[0-9]+)?\b([-a-zA-Z0-9()@:%_+.~#?&/=]*)")]
    private static partial Regex UrlRegex();
}
