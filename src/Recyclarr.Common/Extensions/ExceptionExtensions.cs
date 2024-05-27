namespace Recyclarr.Common.Extensions;

public static class ExceptionExtensions
{
    public static string FullMessage(this Exception ex)
    {
        if (ex is AggregateException aex)
        {
            return aex.InnerExceptions.Aggregate("[ ", (total, next) => $"{total}[{next.FullMessage()}] ") + "]";
        }

        var msg = ex.Message.Replace(", see inner exception.", "").Trim();
        var innerMsg = ex.InnerException?.FullMessage();
        if (innerMsg != null && !innerMsg.ContainsIgnoreCase(msg) && !msg.ContainsIgnoreCase(innerMsg))
        {
            msg = $"{msg} [ {innerMsg} ]";
        }

        return msg;
    }
}
