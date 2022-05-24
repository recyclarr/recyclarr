using Serilog;
using Serilog.Core;

namespace Recyclarr.Logging;

public class LoggerFactory
{
    private readonly LoggingLevelSwitch _logLevel;
    private readonly IDelayedFileSink _fileSink;

    public LoggerFactory(LoggingLevelSwitch logLevel, IDelayedFileSink fileSink)
    {
        _logLevel = logLevel;
        _fileSink = fileSink;
    }

    public ILogger Create()
    {
        const string consoleTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";

        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: consoleTemplate, levelSwitch: _logLevel)
            .WriteTo.Sink(_fileSink)
            .CreateLogger();
    }
}
