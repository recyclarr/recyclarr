using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;

namespace Recyclarr.TestLibrary;

public sealed class TestableLogger : ILogger, IDisposable
{
    private readonly Logger _log;
    private ITestCorrelatorContext _logContext;

    public TestableLogger()
    {
        _log = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .WriteTo.TestCorrelator()
            .WriteTo.Console()
            .CreateLogger();

        _logContext = TestCorrelator.CreateContext();
    }

    public void Write(LogEvent logEvent)
    {
        _log.Write(logEvent);
    }

    public void Dispose()
    {
        _logContext.Dispose();
        _log.Dispose();
    }

    public void ResetCapturedLogs()
    {
        _logContext.Dispose();
        _logContext = TestCorrelator.CreateContext();
    }

    public IEnumerable<string> GetRenderedMessages()
    {
        return TestCorrelator.GetLogEventsFromContextGuid(_logContext.Guid)
            .Select(x => x.RenderMessage());
    }
}
