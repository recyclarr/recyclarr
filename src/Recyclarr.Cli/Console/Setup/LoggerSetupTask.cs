using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Logging;
using Recyclarr.Logging;
using Serilog.Core;

namespace Recyclarr.Cli.Console.Setup;

internal class LoggerSetupTask(
    ILogger log,
    LoggingLevelSwitch loggingLevelSwitch,
    LoggerFactory loggerFactory,
    ConsoleLogSinkConfigurator consoleLogSinkConfigurator,
    IList<ILogConfigurator> logConfigurators
) : IGlobalSetupTask
{
    public void OnStart(BaseCommandSettings cmd)
    {
#pragma warning disable CS0618
        if (cmd.Debug)
        {
            log.Warning(
                "The -d/--debug option is deprecated. Use '--log debug' instead. "
                    + "See: <https://recyclarr.dev/guide/upgrade-guide/v8.0/#debug-removed>"
            );
        }
#pragma warning restore CS0618

        loggingLevelSwitch.MinimumLevel = cmd.LogLevel.Value.ToLogEventLevel();

        if (cmd.LogLevel.IsSet)
        {
            logConfigurators.Add(consoleLogSinkConfigurator);
        }

        loggerFactory.AddLogConfiguration(logConfigurators);
    }

    public void OnFinish() { }
}
