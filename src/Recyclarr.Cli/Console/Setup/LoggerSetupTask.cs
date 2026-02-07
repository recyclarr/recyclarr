using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Logging;
using Recyclarr.Logging;
using Serilog.Core;

namespace Recyclarr.Cli.Console.Setup;

internal class LoggerSetupTask(
    LoggingLevelSwitch loggingLevelSwitch,
    ReloadableLogger reloadableLogger,
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

        reloadableLogger.Reload(logConfigurators);
    }

    public void OnFinish() { }
}
