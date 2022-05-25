using Serilog.Core;

namespace Recyclarr.Logging;

public interface IDelayedFileSink : ILogEventSink, IDisposable
{
    void SetTemplate(string template);
}
