using Serilog.Core;

namespace Recyclarr.Logging;

public interface IDelayedFileSink : ILogEventSink, IDisposable
{
}
