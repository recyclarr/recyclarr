using System.Text.RegularExpressions;
using Flurl.Http;

namespace Recyclarr.TrashLib.Http;

public static class FlurlExtensions
{
    public static string SanitizedExceptionMessage(this FlurlHttpException exception)
    {
        // Replace full URLs
        const string urlExpression =
            @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}(\:[0-9]+)?\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";
        var result = Regex.Replace(exception.Message, urlExpression, Sanitize);

        // There are sometimes parenthetical parts of the message that contain the host but are not
        // detected as true URLs. Just strip those out completely.
        const string hostExpression = @"\([-a-zA-Z0-9@:%._+~#=]{1,256}(?:\:[0-9]+)\)";
        return Regex.Replace(result, hostExpression, "");
    }

    private static string Sanitize(Match match)
    {
        return FlurlLogging.SanitizeUrl(match.Value).ToString();
    }
}
