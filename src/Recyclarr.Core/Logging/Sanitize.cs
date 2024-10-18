using System.Text.RegularExpressions;
using Flurl;

namespace Recyclarr.Logging;

public static partial class Sanitize
{
    public static string Message(string message)
    {
        // Replace full URLs
        var result = UrlRegex().Replace(message, SanitizeMatch);

        // There are sometimes parenthetical parts of the message that contain the host but are not
        // detected as true URLs. Just strip those out completely.
        return HostRegex().Replace(result, "");
    }

    public static string ExceptionMessage(Exception exception)
    {
        return Message(exception.FullMessage());
    }

    public static Url Url(Url url)
    {
        // Replace hostname for user privacy
        url.Host = "REDACTED";
        return url;
    }

    private static string SanitizeMatch(Match match)
    {
        return Url(match.Value).ToString() ?? match.Value;
    }

    [GeneratedRegex(@"\([-a-zA-Z0-9@:%._+~#=]{1,256}(?::[0-9]+)?\)")]
    private static partial Regex HostRegex();

    [GeneratedRegex(@"https?://(www\.)?[-a-zA-Z0-9@:%._+~#=]{1,256}(:[0-9]+)?\b([-a-zA-Z0-9()@:%_+.~#?&/=]*)")]
    private static partial Regex UrlRegex();
}
