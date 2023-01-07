using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.Common.Serilog;

public class ExceptionMessageEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Exception is null)
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
            "ExceptionMessage", logEvent.Exception.Message));
    }
}
