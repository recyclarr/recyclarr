using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Logging;
using Recyclarr.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.Cli.Console.Setup;

public class LoggerSetupTask(
    LoggingLevelSwitch loggingLevelSwitch,
    LoggerFactory loggerFactory,
    IEnumerable<ILogConfigurator> logConfigurators)
    : IGlobalSetupTask
{
    public void OnStart(BaseCommandSettings cmd)
    {
        loggingLevelSwitch.MinimumLevel = cmd.Debug switch
        {
            true => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };

        loggerFactory.AddLogConfiguration(logConfigurators);
    }

    public void OnFinish()
    {
        throw new NotImplementedException();
    }
}
