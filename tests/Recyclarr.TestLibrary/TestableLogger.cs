using System.Globalization;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.TestLibrary;

/// <summary>
/// Serilog logger that writes to Console.Out.
/// TUnit automatically intercepts Console output and associates it with the current test.
/// </summary>
[UsedImplicitly]
public sealed class TestableLogger : ILogger
{
    private readonly Logger _log = new LoggerConfiguration()
        .MinimumLevel.Is(LogEventLevel.Verbose)
        .WriteTo.TextWriter(Console.Out, formatProvider: CultureInfo.InvariantCulture)
        .CreateLogger();

    public void Write(LogEvent logEvent)
    {
        _log.Write(logEvent);
    }
}
