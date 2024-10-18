using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Logging;
using Recyclarr.Settings;

namespace Recyclarr.Cli.Console.Setup;

public class JanitorCleanupTask(LogJanitor janitor, ILogger log, ISettings<LogJanitorSettings> settings)
    : IGlobalSetupTask
{
    public void OnStart(BaseCommandSettings cmd)
    {
    }

    public void OnFinish()
    {
        var maxFiles = settings.Value.MaxFiles;
        log.Debug("Cleaning up logs using max files of {MaxFiles}", maxFiles);
        janitor.DeleteOldestLogFiles(maxFiles);
    }
}
