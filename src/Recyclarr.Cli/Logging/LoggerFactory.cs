using Recyclarr.Logging;

namespace Recyclarr.Cli.Logging;

internal class LoggerFactory
{
    public ILogger Logger { get; private set; } = LogSetup.BaseConfiguration().CreateLogger();

    public void AddLogConfiguration(IEnumerable<ILogConfigurator> configurators)
    {
        var config = LogSetup.BaseConfiguration().WriteTo.Logger(Logger);

        foreach (var configurator in configurators)
        {
            configurator.Configure(config);
        }

        Logger = config.CreateLogger();
    }
}
