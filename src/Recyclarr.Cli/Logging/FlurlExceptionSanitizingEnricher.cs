using System.Text;
using Recyclarr.Http;
using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.Cli.Logging;

public class FlurlExceptionSanitizingEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Exception is null)
        {
            return;
        }

        var sanitizedMessage = Sanitize.ExceptionMessage(logEvent.Exception);

        // Use a builder to handle whether to use a newline character for the full exception message without checking if
        // the sanitized message is null or whitespace more than once.
        var fullBuilder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            MakeProperty("SanitizedExceptionMessage", sanitizedMessage);
            fullBuilder.Append($"{sanitizedMessage}\n");
        }

        // ReSharper disable once InvertIf
        if (logEvent.Exception.StackTrace is not null)
        {
            fullBuilder.Append(logEvent.Exception.StackTrace);
            MakeProperty("SanitizedExceptionFull", fullBuilder.ToString());
        }

        return;

        void MakeProperty(string propertyName, object value)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(propertyName, value));
        }
    }
}
