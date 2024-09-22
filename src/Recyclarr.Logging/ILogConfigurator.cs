using Serilog;

namespace Recyclarr.Logging;

public interface ILogConfigurator
{
    void Configure(LoggerConfiguration config);
}
