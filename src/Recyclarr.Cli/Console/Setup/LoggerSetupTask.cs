using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Logging;
using Recyclarr.Logging;
using Serilog.Core;
using Spectre.Console;

namespace Recyclarr.Cli.Console.Setup;

internal class LoggerSetupTask(
    ILogger log,
    LoggingLevelSwitch loggingLevelSwitch,
    LoggerFactory loggerFactory,
    ConsoleLogSinkConfigurator consoleLogSinkConfigurator,
    IList<ILogConfigurator> logConfigurators,
    IAnsiConsole console
) : IGlobalSetupTask
{
    public void OnStart(BaseCommandSettings cmd)
    {
        // Accept obsolete Debug property usage here for backward compatibility.
        // Remove this call and pragma before next major (v8) version is released.
#pragma warning disable CS0618
        if (cmd.Debug)
#pragma warning restore CS0618
        {
            log.Warning(
                "The -d/--debug option is deprecated. Use '--log debug' instead. "
                    + "See: <https://recyclarr.dev/guide/upgrade-guide/v8.0/#debug-removed>"
            );
        }

        loggingLevelSwitch.MinimumLevel = cmd.LogLevel.Value.ToLogEventLevel();

        if (cmd.LogLevel.IsSet)
        {
            // Log mode: Enable Serilog console output, disable IAnsiConsole output
            logConfigurators.Add(consoleLogSinkConfigurator);
            console.Profile.Out = new AnsiConsoleOutput(TextWriter.Null);
        }

        loggerFactory.AddLogConfiguration(logConfigurators);
    }

    public void OnFinish() { }
}
