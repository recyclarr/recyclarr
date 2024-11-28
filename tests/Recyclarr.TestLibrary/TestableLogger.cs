using System.Globalization;
using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.TestLibrary;

[UsedImplicitly]
public sealed class TestableLogger : ILogger
{
    private readonly Logger _log;
    private readonly List<string> _messages = [];

    public TestableLogger()
    {
        _log = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .WriteTo.Observers(o =>
                o.Subscribe(x => _messages.Add(x.RenderMessage(CultureInfo.InvariantCulture)))
            )
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();
    }

    public void Write(LogEvent logEvent)
    {
        _log.Write(logEvent);
    }

    public IEnumerable<string> Messages => _messages;
}
