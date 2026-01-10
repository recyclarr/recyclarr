using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Logging;
using Recyclarr.Logging;
using Serilog.Core;

namespace Recyclarr.Cli.Console.Setup;

internal class LoggerSetupTask(
    LoggingLevelSwitch loggingLevelSwitch,
    LoggerFactory loggerFactory,
    ConsoleLogSinkConfigurator consoleLogSinkConfigurator,
    IList<ILogConfigurator> logConfigurators
) : IGlobalSetupTask
{
    public void OnStart(BaseCommandSettings cmd)
    {
        loggingLevelSwitch.MinimumLevel = cmd.LogLevel.Value.ToLogEventLevel();

        if (cmd.LogLevel.IsSet)
        {
            logConfigurators.Add(consoleLogSinkConfigurator);
        }

        loggerFactory.AddLogConfiguration(logConfigurators);
    }

    public void OnFinish() { }
}
