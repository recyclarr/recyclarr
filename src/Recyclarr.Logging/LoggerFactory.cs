using Serilog;
using Serilog.Events;

namespace Recyclarr.Logging;

public class LoggerFactory(Lazy<IEnumerable<ILogConfigurator>> lazyConfigurators)
{
    public ILogger Create()
    {
        var configurators = lazyConfigurators.Value;

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
