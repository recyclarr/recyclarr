using System.Globalization;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.TestLibrary;

public sealed class TestableLogger : ILogger
{
    // TUnit intercepts Console.Out and routes each write to the output of the running test.
    private readonly Logger _log = new LoggerConfiguration()
        .MinimumLevel.Is(LogEventLevel.Verbose)
        .WriteTo.TextWriter(Console.Out, formatProvider: CultureInfo.InvariantCulture)
        .CreateLogger();

    public void Write(LogEvent logEvent)
    {
        _log.Write(logEvent);
    }
}
