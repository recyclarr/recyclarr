using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.Logging;

internal class FlurlExceptionSanitizingEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Exception is null)
        {
            return;
        }

        var sanitizedMessage = Sanitize.ExceptionMessage(logEvent.Exception);

        if (!string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SanitizedExceptionMessage", sanitizedMessage));
        }
    }
}
