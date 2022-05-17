using System.Text.RegularExpressions;
using Flurl.Http;

namespace TrashLib.Extensions;

public static class FlurlExtensions
{
    public static string SanitizedExceptionMessage(this FlurlHttpException exception)
    {
        const string expression =
            @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}(\:[0-9]+)?\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";

        return Regex.Replace(exception.ToString(), expression,
            match => FlurlLogging.SanitizeUrl(match.Value).ToString());
    }
}
