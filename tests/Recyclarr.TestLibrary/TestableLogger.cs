using JetBrains.Annotations;
using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.TestLibrary;

[UsedImplicitly]
public sealed class TestableLogger : ILogger
{
    private readonly Logger _log;
    private readonly List<string> _messages = new();

    public TestableLogger()
    {
        _log = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .WriteTo.Observers(o => o.Subscribe(x => _messages.Add(x.RenderMessage())))
            .WriteTo.Console()
            .CreateLogger();
    }

    public void Write(LogEvent logEvent)
    {
        _log.Write(logEvent);
    }

    public IEnumerable<string> Messages => _messages;
}
