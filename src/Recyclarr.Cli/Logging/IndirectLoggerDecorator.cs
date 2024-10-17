using Serilog.Events;

namespace Recyclarr.Cli.Logging;

internal class IndirectLoggerDecorator(LoggerFactory loggerFactory) : ILogger
{
    public void Write(LogEvent logEvent)
    {
        loggerFactory.Logger.Write(logEvent);
    }
}
