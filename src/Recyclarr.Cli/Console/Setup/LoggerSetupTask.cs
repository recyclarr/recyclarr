using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Logging;
using Serilog.Core;

namespace Recyclarr.Cli.Console.Setup;

internal class LoggerSetupTask(
    LoggingLevelSwitch loggingLevelSwitch,
    ReloadableLogger reloadableLogger
) : IGlobalSetupTask
{
    public void OnStart(BaseCommandSettings cmd)
    {
        loggingLevelSwitch.MinimumLevel = cmd.EffectiveLogLevel.ToLogEventLevel();

        if (cmd.IsLogMode)
        {
            reloadableLogger.UseConsole();
        }
        else
        {
            reloadableLogger.Silence();
        }
    }

    public void OnFinish() { }
}
