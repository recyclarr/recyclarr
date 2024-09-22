using Serilog;
using Serilog.Events;

namespace Recyclarr.Logging;

public class LoggerFactory(IEnumerable<ILogConfigurator> configurators)
{
    public ILogger Create()
    {
        var config = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .Enrich.FromLogContext()
            .Enrich.With<FlurlExceptionSanitizingEnricher>();

        foreach (var configurator in configurators)
        {
            configurator.Configure(config);
        }

        return config.CreateLogger();
    }
}
