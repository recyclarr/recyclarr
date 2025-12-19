using System.Globalization;
using NUnit.Framework;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.TestLibrary;

[UsedImplicitly]
public sealed class TestableLogger : ILogger
{
    private readonly Logger _log = new LoggerConfiguration()
        .MinimumLevel.Is(LogEventLevel.Verbose)
        .WriteTo.TextWriter(TestContext.Out, formatProvider: CultureInfo.InvariantCulture)
        .CreateLogger();

    public void Write(LogEvent logEvent)
    {
        _log.Write(logEvent);
    }
}
