using Recyclarr.Cli.Logging;
using Recyclarr.Settings;

namespace Recyclarr.Cli.Console.Setup;

public class JanitorCleanupTask(LogJanitor janitor, ILogger log, ISettingsProvider settingsProvider)
    : IGlobalSetupTask
{
    public void OnFinish()
    {
        var maxFiles = settingsProvider.Settings.LogJanitor.MaxFiles;
        log.Debug("Cleaning up logs using max files of {MaxFiles}", maxFiles);
        janitor.DeleteOldestLogFiles(maxFiles);
    }
}
