using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.Cli.Logging;

public class ExceptionMessageEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var msg = logEvent.Exception?.Message;
        if (string.IsNullOrEmpty(msg))
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ExceptionMessage", msg));
    }
}
