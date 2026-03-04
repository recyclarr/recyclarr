using System.Text.RegularExpressions;

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

    public static Uri Url(Uri url)
    {
        var builder = new UriBuilder(url) { Host = "REDACTED" };
        return builder.Uri;
    }

    private static string SanitizeMatch(Match match)
    {
        return Url(new Uri(match.Value)).ToString();
    }

    [GeneratedRegex(@"\([-a-zA-Z0-9@:%._+~#=]{1,256}(?::[0-9]+)?\)")]
    private static partial Regex HostRegex();

    [GeneratedRegex(
        @"https?://(www\.)?[-a-zA-Z0-9@:%._+~#=]{1,256}(:[0-9]+)?\b([-a-zA-Z0-9()@:%_+.~#?&/=]*)"
    )]
    private static partial Regex UrlRegex();
}
