using Serilog;
using Serilog.Core;

namespace Recyclarr.Logging;

public class LoggerFactory
{
    private const string ConsoleTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";

    private readonly LoggingLevelSwitch _logLevel;
    private readonly Func<IDelayedFileSink> _fileSinkFactory;

    public LoggerFactory(LoggingLevelSwitch logLevel, Func<IDelayedFileSink> fileSinkFactory)
    {
        _logLevel = logLevel;
        _fileSinkFactory = fileSinkFactory;
    }

    public ILogger Create()
    {
        var fileSink = _fileSinkFactory();
        fileSink.SetTemplate(ConsoleTemplate);

        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: ConsoleTemplate, levelSwitch: _logLevel)
            .WriteTo.Sink(fileSink)
            .CreateLogger();
    }
}
