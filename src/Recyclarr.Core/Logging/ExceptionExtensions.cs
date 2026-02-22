using Recyclarr.Common.Extensions;

namespace Recyclarr.Logging;

public static class ExceptionExtensions
{
    public static T? FindInnerException<T>(this Exception e)
        where T : Exception
    {
        for (var current = e.InnerException; current is not null; current = current.InnerException)
        {
            if (current is T match)
            {
                return match;
            }
        }

        return null;
    }

    public static string FullMessage(this Exception ex)
    {
        if (ex is AggregateException aex)
        {
            return aex.InnerExceptions.Aggregate(
                    "[ ",
                    (total, next) => $"{total}[{next.FullMessage()}] "
                ) + "]";
        }

        var msg = ex.Message.Replace(", see inner exception.", "", StringComparison.Ordinal).Trim();
        var innerMsg = ex.InnerException?.FullMessage();
        if (
            innerMsg != null
            && !innerMsg.ContainsIgnoreCase(msg)
            && !msg.ContainsIgnoreCase(innerMsg)
        )
        {
            msg = $"{msg} [ {innerMsg} ]";
        }

        return msg;
    }
}
